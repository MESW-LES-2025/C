using System.Text.Json;
using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.Infrastructure.Services;
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
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                user.ID = Guid.NewGuid();
                lawyer.ID = user.ID;
                user.IsActive = true;

                _context.Users.Add(user);
                _context.Lawyers.Add(lawyer);

                // Record log with the same values for old/new
                JsonElement initialState = WrapLawyerToLog(lawyer);
                await RecordLogAsync(lawyer, user, "LAWYER_CREATED", initialState);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetById(lawyer.ID) ?? lawyer;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task Update(Lawyer lawyer)
        {
            // Note: This only updates the Lawyer table (e.g., ProfessionalRegister)
            _context.Lawyers.Update(lawyer);
            await _context.SaveChangesAsync();
        }


        private async Task<User> GetEditorAsync(Guid? editorId)
        {
            if (!editorId.HasValue)
                throw new ArgumentException("Editor ID must be provided for logging purposes");

            // We use FirstOrDefaultAsync to be explicit.
            // If your User model is very heavy, you could even use .Select() here,
            // but for logging, the full entity is usually fine.
            return await _context.Users.FirstOrDefaultAsync(u => u.ID == editorId.Value)
                   ?? throw new KeyNotFoundException($"Editor user with ID {editorId.Value} not found");
        }


        private async Task<(Lawyer? lawyer, JsonElement? oldValue)> GetExistingLawyerWithLogAsync(Guid lawyerId)
        {
            var lawyer = await _context.Lawyers
                .Include(l => l.User)
                    .ThenInclude(u => u.Phones)
                .FirstOrDefaultAsync(l => l.ID == lawyerId);

            if (lawyer == null)
            {
                return (null, null);
            }

            return (lawyer, WrapLawyerToLog(lawyer));
        }



        private void ApplyUserUpdates(User existingUser, User updates, bool? isActive)
        {
            // Update basic user identity fields if provided
            if (!string.IsNullOrWhiteSpace(updates.Name))
                existingUser.Name = updates.Name;

            if (!string.IsNullOrWhiteSpace(updates.Email))
                existingUser.Email = updates.Email;

            if (!string.IsNullOrWhiteSpace(updates.NIF))
                existingUser.NIF = updates.NIF;

            if (!string.IsNullOrWhiteSpace(updates.PasswordHash))
                existingUser.PasswordHash = updates.PasswordHash;

            // Update account status if a new value was explicitly passed
            if (isActive.HasValue)
                existingUser.IsActive = isActive.Value;
        }





        private void ApplyPhoneUpdates(User existingUser, ICollection<Phone> phoneUpdates)
        {
            // Skip if no phone updates were provided
            if (phoneUpdates == null || !phoneUpdates.Any()) return;

            var phoneUpd = phoneUpdates.First();
            var existingMain = existingUser.Phones.FirstOrDefault(p => p.IsMain);

            if (existingMain != null)
            {
                // Update existing main phone details
                if (!string.IsNullOrWhiteSpace(phoneUpd.Number))
                    existingMain.Number = phoneUpd.Number;

                if (phoneUpd.CountryCode != 0)
                    existingMain.CountryCode = phoneUpd.CountryCode;

                existingMain.IsMain = phoneUpd.IsMain;
            }
            else
            {
                // Create and attach a new phone record if none exists
                var newPhone = new Phone
                {
                    ID = Guid.NewGuid(),
                    UserID = existingUser.ID,
                    Number = phoneUpd.Number ?? string.Empty,
                    CountryCode = phoneUpd.CountryCode != 0 ? phoneUpd.CountryCode : (short)351,
                    IsMain = phoneUpd.IsMain
                };
                existingUser.Phones.Add(newPhone);
                _context.Phones.Add(newPhone);
            }
        }


        private void ApplyLawyerUpdates(Lawyer existingLawyer, Lawyer updates)
        {
            // Update lawyer-specific professional information
            if (!string.IsNullOrWhiteSpace(updates.ProfessionalRegister))
                existingLawyer.ProfessionalRegister = updates.ProfessionalRegister;
        }


        private async Task RecordLogAsync(Lawyer lawyer, User editor, string actionType, JsonElement oldValue)
        {
            // Capture current state as NewValue
            JsonElement newValue = WrapLawyerToLog(lawyer);

            // Build the log entry (centralizing the ActionLogType lookup)
            UserLog log = await BuildLawyerLog(lawyer, editor, actionType, oldValue, newValue);

            // Add it to the context tracking
            _context.UserLogs.Add(log);
        }




        public async Task<Lawyer?> UpdateLawyerAndUser(Guid lawyerId, Lawyer lawyerUpdates, User userUpdates, bool? isActive = null, Guid? editorId = null)
        {
            // Get and validate the editor
            User editor = await GetEditorAsync(editorId);


            // Get the existing lawyer and prepare the log snapshot
            var (existingLawyer, oldValue) = await GetExistingLawyerWithLogAsync(lawyerId);
            if (existingLawyer == null) return null;

            // Applying Entity Changes
            ApplyUserUpdates(existingLawyer.User, userUpdates, isActive);
            ApplyPhoneUpdates(existingLawyer.User, userUpdates.Phones);

            // Applying Lawyer-specific Changes
            ApplyLawyerUpdates(existingLawyer, lawyerUpdates);


            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Lawyers.Update(existingLawyer);
                _context.Users.Update(existingLawyer.User);

                // Record log with the captured oldValue
                await RecordLogAsync(existingLawyer, editor, "LAWYER_UPDATED", oldValue!.Value);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return existingLawyer;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
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

        private JsonElement WrapLawyerToLog(Lawyer lawyer)
        {
            // A flat structure that combines data from both tables
            var WrapedLawer = new
            {
                user_type = "lawyer",
                // table user_log
                user_id = lawyer.User?.ID,
                user_name = lawyer.User?.Name,
                user_nif = lawyer.User?.NIF,
                user_email = lawyer.User?.Email,
                user_is_active = lawyer.User?.IsActive,
                // table lawyer
                lawyer_id = lawyer.ID,
                lawyer_professional_register = lawyer.ProfessionalRegister,
                // table phone - taking the first one marked as IsMain or just the first in the list
                user_phone = lawyer.User?.Phones?.FirstOrDefault(p => p.IsMain)?.Number
                     ?? lawyer.User?.Phones?.FirstOrDefault()?.Number
            };

            // Serialize to JsonElement for PostgreSQL JSONB compatibility
            return JsonSerializer.SerializeToElement(WrapedLawer);
        }

        private async Task<UserLog> BuildLawyerLog(Lawyer lawyer, User editor, string actionType, JsonElement oldValue, JsonElement newValue)
        {
            int actionTypeId = await GetActionLogTypeIdByName(actionType);
            UserLog log = new UserLog
            {
                ID = Guid.NewGuid(),
                AffectedUserID = lawyer.User.ID,
                UpdatedByID = editor.ID,
                ActionLogTypeID = actionTypeId,
                OldValue = oldValue,
                NewValue = newValue
            };


            return log;
        }

        private async Task<int> GetActionLogTypeIdByName(string name)
        {
            var actionLogType = await _context.ActionLogTypes
                .FirstOrDefaultAsync(alt => alt.Name == name);

            if (actionLogType == null)
            {
                throw new KeyNotFoundException($"ActionLogType with name '{name}' not found");
            }

            return actionLogType.ID;
        }
    }
}
