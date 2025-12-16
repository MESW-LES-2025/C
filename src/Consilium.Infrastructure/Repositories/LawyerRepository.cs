using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Repositories
{
    public class LawyerRepository : ILawyerRepository
    {
        private readonly AppDbContext _context;

        public LawyerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Lawyer?> GetById(Guid id)
        {
            // Use Include to also load the related User data and Phones
            return await _context.Lawyers
                .Include(l => l.User)
                    .ThenInclude(u => u.Phones)
                .FirstOrDefaultAsync(l => l.ID == id);
        }

        public async Task<(List<Lawyer> Lawyers, int TotalCount)> GetAll(
            string? search,
            string? status,
            int page,
            int limit,
            string? sortBy,
            string? sortOrder)
        {
            var query = _context.Lawyers
                .Include(l => l.User)
                    .ThenInclude(u => u.Phones)
                .AsQueryable();

            // Text search across Name, Email, NIF, and Professional Register
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l =>
                    l.User.Name.Contains(search) ||
                    l.User.Email.Contains(search) ||
                    l.User.NIF.Contains(search) ||
                    l.ProfessionalRegister.Contains(search));
            }

            // Filter by status (IsActive boolean)
            if (!string.IsNullOrWhiteSpace(status))
            {
                var isActive = status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
                query = query.Where(l => l.User.IsActive == isActive);
            }

            // Count before pagination
            var totalCount = await query.CountAsync();

            // Sorting
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = sortBy.ToLower();
                sortOrder = sortOrder?.ToLower() ?? "asc";

                if (sortBy == "nif")
                    query = sortOrder == "desc" ? query.OrderByDescending(l => l.User.NIF) : query.OrderBy(l => l.User.NIF);
                else if (sortBy == "register")
                    query = sortOrder == "desc" ? query.OrderByDescending(l => l.ProfessionalRegister) : query.OrderBy(l => l.ProfessionalRegister);
                else
                    query = sortOrder == "desc" ? query.OrderByDescending(l => l.User.Name) : query.OrderBy(l => l.User.Name);
            }

            // Pagination
            var lawyers = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (lawyers, totalCount);
        }

        public async Task<Lawyer> Create(User user, Lawyer lawyer)
        {
            // This needs to be a transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Set the Lawyer's ID to be the same as the User's ID
                user.ID = Guid.NewGuid();
                lawyer.ID = user.ID;
                
                // Ensure IsActive is set (should default to true)
                user.IsActive = true;

                // Add the User first
                _context.Users.Add(user);
                
                // Add the Lawyer
                _context.Lawyers.Add(lawyer);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Reload the lawyer with related user and phones so caller gets populated data
                var loaded = await _context.Lawyers
                    .Include(l => l.User)
                        .ThenInclude(u => u.Phones)
                    .FirstOrDefaultAsync(l => l.ID == lawyer.ID);

                return loaded ?? lawyer;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error saving User/Lawyer to DB: {ex.GetType()}: {ex.Message}");
                throw;
            }
        }

        public async Task Update(Lawyer lawyer)
        {
            // Note: This only updates the Lawyer table (e.g., ProfessionalRegister)
            _context.Lawyers.Update(lawyer);
            await _context.SaveChangesAsync();
        }

    public async Task<Lawyer?> UpdateLawyerAndUser(Guid lawyerId, Lawyer lawyerUpdates, User userUpdates, bool? isActive = null)
        {
            // Get the existing lawyer with its user and phones
            var existingLawyer = await _context.Lawyers
                .Include(l => l.User)
                    .ThenInclude(u => u.Phones)
                .FirstOrDefaultAsync(l => l.ID == lawyerId);

            if (existingLawyer == null)
                return null;

            // Update User fields if provided
            if (!string.IsNullOrWhiteSpace(userUpdates.Name))
                existingLawyer.User.Name = userUpdates.Name;

            if (!string.IsNullOrWhiteSpace(userUpdates.Email))
                existingLawyer.User.Email = userUpdates.Email;

            // Update NIF if provided
            if (!string.IsNullOrWhiteSpace(userUpdates.NIF))
                existingLawyer.User.NIF = userUpdates.NIF;

            if (!string.IsNullOrWhiteSpace(userUpdates.PasswordHash))
                existingLawyer.User.PasswordHash = userUpdates.PasswordHash;

            // Update IsActive flag if provided
            if (isActive.HasValue)
                existingLawyer.User.IsActive = isActive.Value;

            // Handle phone updates: if the caller provided Phone objects in userUpdates.Phones,
            // we'll treat the first one as the 'main' phone and upsert it.
            if (userUpdates.Phones != null && userUpdates.Phones.Any())
            {
                var phoneUpd = userUpdates.Phones.First();

                // Try to find an existing main phone
                var existingMain = existingLawyer.User.Phones.FirstOrDefault(p => p.IsMain == true);
                if (existingMain != null)
                {
                    // Update existing main phone
                    if (!string.IsNullOrWhiteSpace(phoneUpd.Number))
                        existingMain.Number = phoneUpd.Number;
                    if (phoneUpd.CountryCode != 0)
                        existingMain.CountryCode = phoneUpd.CountryCode;
                    existingMain.IsMain = phoneUpd.IsMain;
                }
                else
                {
                    // Create a new phone record and attach to the user
                    var newPhone = new Phone
                    {
                        ID = Guid.NewGuid(),
                        UserID = existingLawyer.User.ID,
                        Number = phoneUpd.Number ?? string.Empty,
                        CountryCode = phoneUpd.CountryCode != 0 ? phoneUpd.CountryCode : (short)351,
                        IsMain = phoneUpd.IsMain
                    };
                    existingLawyer.User.Phones.Add(newPhone);
                    _context.Phones.Add(newPhone);
                }
            }

            // Update Lawyer fields if provided
            if (!string.IsNullOrWhiteSpace(lawyerUpdates.ProfessionalRegister))
                existingLawyer.ProfessionalRegister = lawyerUpdates.ProfessionalRegister;

            _context.Lawyers.Update(existingLawyer);
            _context.Users.Update(existingLawyer.User);
            await _context.SaveChangesAsync();

            return existingLawyer;
        }

        public async Task Delete(Guid id)
        {
            // Get the lawyer first
            var lawyer = await _context.Lawyers
                .Include(l => l.User)
                .ThenInclude(u => u.Phones)
                .FirstOrDefaultAsync(l => l.ID == id);
            
            if (lawyer == null)
                throw new KeyNotFoundException($"Lawyer with ID {id} not found");

            // Check if lawyer has active/open cases
            // Check if lawyer has active/open cases
            var hasActiveCases = await _context.Processes
                .Include(p => p.Status)
                .AnyAsync(p => p.LawyerId == id && !p.Status.IsFinal);

            if (hasActiveCases)
                throw new InvalidOperationException("Lawyer has active/open cases and cannot be deleted");
            
            // Delete in proper order: Phones -> Lawyer -> User
            if (lawyer.User?.Phones != null)
            {
                foreach (var phone in lawyer.User.Phones)
                    _context.Phones.Remove(phone);
            }
            
            _context.Lawyers.Remove(lawyer);
            
            if (lawyer.User != null)
                _context.Users.Remove(lawyer.User);
            
            await _context.SaveChangesAsync();
        }
    }
}
