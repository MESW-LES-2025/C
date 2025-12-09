using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly AppDbContext _context;

        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetById(Guid id)
        {
            // Use Include to also load the related User data
            return await _context.Clients
                .Include(c => c.User)
                    .ThenInclude(u => u.Phones)
                .FirstOrDefaultAsync(c => c.ID == id);
        }

        public async Task<(List<Client> Clients, int TotalCount)> GetAll(
            string? search,
            string? status,
            int page,
            int limit,
            string? sortBy,
            string? sortOrder)
        {
            var query = _context.Clients
                .Include(c => c.User)
                    .ThenInclude(u => u.Phones)
                .AsQueryable();

            // Text search across Name, Email, and NIF
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.User.Name.Contains(search) ||
                    c.User.Email.Contains(search) ||
                    c.User.NIF.Contains(search));
            }

            // Filter by status (IsActive boolean)
            if (!string.IsNullOrWhiteSpace(status))
            {
                var isActive = status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
                query = query.Where(c => c.User.IsActive == isActive);
            }

            // Count before pagination
            var totalCount = await query.CountAsync();

            // ↕Sorting
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = sortBy.ToLower();
                sortOrder = sortOrder?.ToLower() ?? "asc";

                if (sortBy == "nif")
                    query = sortOrder == "desc" ? query.OrderByDescending(c => c.User.NIF) : query.OrderBy(c => c.User.NIF);
                else
                    query = sortOrder == "desc" ? query.OrderByDescending(c => c.User.Name) : query.OrderBy(c => c.User.Name);
            }

            // Pagination
            var clients = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (clients, totalCount);
        }

        
        public async Task<Client> Create(User user, Client client)
        {
            // This needs to be a transaction
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Set the Client's ID to be the same as the User's ID
                user.ID = Guid.NewGuid();
                client.ID = user.ID;
                
                // Ensure IsActive is set (should default to true)
                user.IsActive = true;

                // Add the User first
                _context.Users.Add(user);
                
                // Add the Client
                _context.Clients.Add(client);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Reload the client with related user and phones so caller gets populated data
                var loaded = await _context.Clients
                    .Include(c => c.User)
                        .ThenInclude(u => u.Phones)
                    .FirstOrDefaultAsync(c => c.ID == client.ID);

                return loaded ?? client;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error saving User/Client to DB: {ex.GetType()}: {ex.Message}");
                throw;
            }
        }

        public async Task Update(Client client)
        {
            // Note: This only updates the Client table (e.g., Address)
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();
        }

        public async Task<Client?> UpdateClientAndUser(Guid clientId, Client clientUpdates, User userUpdates, bool? isActive)
        {
            // Get the existing client with its user
            var existingClient = await _context.Clients
                .Include(c => c.User)
                    .ThenInclude(u => u.Phones)
                .FirstOrDefaultAsync(c => c.ID == clientId);

            if (existingClient == null)
                return null;

            // Update User fields if provided
            if (!string.IsNullOrWhiteSpace(userUpdates.Name))
                existingClient.User.Name = userUpdates.Name;

            if (!string.IsNullOrWhiteSpace(userUpdates.Email))
                existingClient.User.Email = userUpdates.Email;

            // Update NIF if provided
            if (!string.IsNullOrWhiteSpace(userUpdates.NIF))
                existingClient.User.NIF = userUpdates.NIF;

            // Update active flag if provided
            if (isActive.HasValue)
                existingClient.User.IsActive = isActive.Value;

            // Update NIF if provided
            if (!string.IsNullOrWhiteSpace(userUpdates.NIF))
                existingClient.User.NIF = userUpdates.NIF;

            // Handle phone updates: if the caller provided Phone objects in userUpdates.Phones,
            // we'll treat the first one as the 'main' phone and upsert it.
            if (userUpdates.Phones != null && userUpdates.Phones.Any())
            {
                var phoneUpd = userUpdates.Phones.First();

                // Try to find an existing main phone
                var existingMain = existingClient.User.Phones.FirstOrDefault(p => p.IsMain == true);
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
                        UserID = existingClient.User.ID,
                        Number = phoneUpd.Number ?? string.Empty,
                        CountryCode = phoneUpd.CountryCode != 0 ? phoneUpd.CountryCode : (short)351,
                        IsMain = phoneUpd.IsMain
                    };
                    existingClient.User.Phones.Add(newPhone);
                    _context.Phones.Add(newPhone);
                }
            }

            if (!string.IsNullOrWhiteSpace(userUpdates.PasswordHash))
                existingClient.User.PasswordHash = userUpdates.PasswordHash;

            // Update Client fields if provided
            if (!string.IsNullOrWhiteSpace(clientUpdates.Address))
                existingClient.Address = clientUpdates.Address;

            _context.Clients.Update(existingClient);
            _context.Users.Update(existingClient.User);
            await _context.SaveChangesAsync();

            return existingClient;
        }

        public async Task Delete(Guid id)
        {
            // Get the client first
            var client = await _context.Clients
                .Include(c => c.User)
                .ThenInclude(u => u.Phones)
                .FirstOrDefaultAsync(c => c.ID == id);
            
            if (client == null)
                throw new KeyNotFoundException($"Client with ID {id} not found");

            // Check if client has active/open cases
            var hasActiveCases = await _context.Processes
                .Include(p => p.Status)
                .AnyAsync(p => p.ClientId == id && !p.Status.IsFinal);

            if (hasActiveCases)
                throw new InvalidOperationException("Client has active/open cases and cannot be deleted");
            
            // Delete in proper order: Phones -> Client -> User
            if (client.User?.Phones != null)
            {
                foreach (var phone in client.User.Phones)
                    _context.Phones.Remove(phone);
            }
            
            _context.Clients.Remove(client);
            
            if (client.User != null)
                _context.Users.Remove(client.User);
            
            await _context.SaveChangesAsync();
        }
    }
}