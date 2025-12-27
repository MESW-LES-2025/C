using System.Text.Json;
using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Repositories;

public class LawyerRepository : ILawyerRepository
{
    private readonly AppDbContext _context;
    private readonly IProcessRepository _processRepository;
    private readonly JsonElement _emptyJson = JsonSerializer.SerializeToElement(new { });

    public LawyerRepository(AppDbContext context, IProcessRepository processRepository)
    {
        _context = context;
        _processRepository = processRepository;
    }

    #region CRUD Operations

    /// <summary>
    /// Registers a new lawyer and their associated user account.
    /// </summary>
    public async Task<Lawyer> Create(User user, Lawyer lawyer, Guid editorId)
    {
        var editor = await GetEditorAsync(editorId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Initialize IDs and default status
            user.ID = Guid.NewGuid();
            lawyer.ID = user.ID;
            user.IsActive = true;

            // Ensure mandatory contact information
            ValidateLawyerPhone(user.Phones);

            _context.Users.Add(user);
            _context.Lawyers.Add(lawyer);

            // Record initial state in audit log
            await RecordLogAsync("LAWYER_CREATED", lawyer, lawyer, editor);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return lawyer;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Performs a simple database lookup without triggering audit logs.
    /// </summary>
    public async Task<Lawyer?> GetLawyerById(Guid id)
    {
        return await _context.Lawyers
            .Include(l => l.User)
                .ThenInclude(u => u.Phones)
            .FirstOrDefaultAsync(l => l.ID == id);
    }

    /// <summary>
    /// Retrieves the full profile for display and records a read action.
    /// </summary>
    public async Task<Lawyer?> GetLawyerProfileById(Guid id, Guid editorId)
    {
        var lawyer = await GetLawyerById(id);
        if (lawyer == null) throw new KeyNotFoundException($"Lawyer {id} not found.");

        var editor = await GetEditorAsync(editorId);

        // Track who viewed this profile
        await RecordLogAsync("LAWYER_READ", lawyer, lawyer, editor);
        await _context.SaveChangesAsync();

        return lawyer;
    }

    /// <summary>
    /// Returns a paginated list of lawyers with optional search and status filters.
    /// </summary>
    public async Task<(IEnumerable<Lawyer> lawyers, int totalCount)> GetAll(
        string? search, string? status, int page, int limit, string? sortBy, string? sortOrder)
    {
        var query = _context.Lawyers.Include(l => l.User).ThenInclude(u => u.Phones).AsQueryable();

        // Apply filters if provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.User.Name.Contains(search) || l.User.Email.Contains(search) ||
                                     l.User.NIF.Contains(search) || l.ProfessionalRegister.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var isActive = status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase);
            query = query.Where(l => l.User.IsActive == isActive);
        }

        var totalCount = await query.CountAsync();
        query = ApplySorting(query, sortBy, sortOrder);

        // Execute pagination
        var lawyers = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
        return (lawyers, totalCount);
    }

    /// <summary>
    /// Updates both Lawyer and User records and logs the changes.
    /// </summary>
    public async Task<Lawyer?> UpdateLawyerAndUser(Guid id, Lawyer lawyerUpdates, User userUpdates, Guid editorId, bool? isActive)
    {
        var editor = await GetEditorAsync(editorId);

        // Retrieve tracked entity
        var oldLawyer = await GetLawyerById(id);
        if (oldLawyer == null) return null;

        // Maintain reference for the update operation
        var newLawyer = oldLawyer;

        // Apply modifications to the instance
        ApplyUserUpdates(newLawyer.User, userUpdates, isActive);
        if (userUpdates.Phones != null && userUpdates.Phones.Any())
        {
            ValidateLawyerPhone(userUpdates.Phones);
            ApplyPhoneUpdates(newLawyer.User, userUpdates.Phones);
        }
        ApplyLawyerUpdates(newLawyer, lawyerUpdates);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Lawyers.Update(newLawyer);

            await RecordLogAsync("LAWYER_UPDATED", oldLawyer, newLawyer, editor);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return newLawyer;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Permanently removes a lawyer and their user data if no active cases exist.
    /// </summary>
    /// <summary>
    /// Permanently removes a lawyer and their user data from the system.
    /// </summary>
    public async Task Delete(Guid id, Guid editorId)
    {
        var editor = await GetEditorAsync(editorId);

        // Retrieve the lawyer to be deleted
        var oldLawyer = await GetLawyerById(id);
        if (oldLawyer == null) throw new KeyNotFoundException($"Lawyer {id} not found");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Clean up related data before removal
            await _processRepository.DissociateProcessesFromLawyer(id, editorId);

            // Remove all associated phone records
            DeleteUserPhones(oldLawyer.User);

            // Record the deletion in audit logs
            await RecordLogAsync("LAWYER_DELETED", oldLawyer, oldLawyer, editor);

            // Remove PII linkage from historical logs
            await AnonymizeUserLogByAffectedUserId(id);

            // Remove main entities
            _context.Lawyers.Remove(oldLawyer);
            if (oldLawyer.User != null) _context.Users.Remove(oldLawyer.User);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Removes all phone numbers associated with a specific user.
    /// </summary>
    private void DeleteUserPhones(User? user)
    {
        if (user?.Phones != null && user.Phones.Any())
        {
            _context.Phones.RemoveRange(user.Phones);
        }
    }

    #endregion

    #region Private Helper Methods (Alphabetical)

    /// <summary>
    /// Decouples logs from a deleted user for historical preservation without PII.
    /// </summary>
    private async Task AnonymizeUserLogByAffectedUserId(Guid userId)
    {
        await _context.UserLogs
            .Where(ul => ul.AffectedUserID == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(ul => ul.AffectedUserID, (Guid?)null));
    }

    /// <summary>
    /// Updates lawyer-specific properties.
    /// </summary>
    private void ApplyLawyerUpdates(Lawyer existingLawyer, Lawyer updates)
    {
        if (!string.IsNullOrWhiteSpace(updates.ProfessionalRegister))
            existingLawyer.ProfessionalRegister = updates.ProfessionalRegister;
    }

    /// <summary>
    /// Manages user phone contact updates, maintaining a single main phone.
    /// </summary>
    private void ApplyPhoneUpdates(User existingUser, ICollection<Phone> phoneUpdates)
    {
        var phoneUpd = phoneUpdates.First();
        var existingMain = existingUser.Phones.FirstOrDefault(p => p.IsMain);

        if (existingMain != null)
            existingMain.Number = phoneUpd.Number ?? existingMain.Number;
        else
            existingUser.Phones.Add(new Phone { ID = Guid.NewGuid(), Number = phoneUpd.Number, CountryCode = 351, IsMain = true });
    }

    /// <summary>
    /// Applies dynamic sorting to the lawyer query.
    /// </summary>
    private IQueryable<Lawyer> ApplySorting(IQueryable<Lawyer> query, string? sortBy, string? sortOrder)
    {
        sortOrder = sortOrder?.ToLower() ?? "asc";
        return sortBy?.ToLower() switch
        {
            "nif" => sortOrder == "desc" ? query.OrderByDescending(l => l.User.NIF) : query.OrderBy(l => l.User.NIF),
            "register" => sortOrder == "desc" ? query.OrderByDescending(l => l.ProfessionalRegister) : query.OrderBy(l => l.ProfessionalRegister),
            _ => sortOrder == "desc" ? query.OrderByDescending(l => l.User.Name) : query.OrderBy(l => l.User.Name),
        };
    }

    /// <summary>
    /// Updates base user properties.
    /// </summary>
    private void ApplyUserUpdates(User existingUser, User updates, bool? isActive)
    {
        if (!string.IsNullOrWhiteSpace(updates.Name)) existingUser.Name = updates.Name;
        if (!string.IsNullOrWhiteSpace(updates.Email)) existingUser.Email = updates.Email;
        if (!string.IsNullOrWhiteSpace(updates.NIF)) existingUser.NIF = updates.NIF;
        if (isActive.HasValue) existingUser.IsActive = isActive.Value;
    }

    /// <summary>
    /// Retrieves the editor user or throws if not found.
    /// </summary>
    private async Task<User> GetEditorAsync(Guid editorId)
    {
        return await _context.Users.FindAsync(editorId) ?? throw new KeyNotFoundException($"Editor {editorId} not found");
    }

    /// <summary>
    /// Records the audit log for lawyer operations with the exact requested signature.
    /// </summary>
    private async Task RecordLogAsync(string actionType, Lawyer oldLawyer, Lawyer newLawyer, User editor)
    {
        var actionLogType = await _context.ActionLogTypes.FirstAsync(alt => alt.Name == actionType);

        // Serialize states to JSON elements
        var oldValueWrapped = WrapLawyerToLog(oldLawyer, actionType);
        var newValueWrapped = WrapLawyerToLog(newLawyer, actionType);

        var log = new UserLog
        {
            ID = Guid.NewGuid(),
            AffectedUserID = newLawyer.ID,
            UpdatedByID = editor.ID,
            ActionLogTypeID = actionLogType.ID,
            OldValue = oldValueWrapped,
            NewValue = newValueWrapped
        };

        _context.UserLogs.Add(log);
    }

    /// <summary>
    /// Validates that at least one valid phone number is provided.
    /// </summary>
    private void ValidateLawyerPhone(ICollection<Phone>? phones)
    {
        if (phones == null || !phones.Any() || string.IsNullOrWhiteSpace(phones.First().Number))
            throw new InvalidOperationException("A valid phone number is required.");
    }

    /// <summary>
    /// Wraps lawyer entity data into a standardized JSON structure for logging.
    /// </summary>
    private JsonElement WrapLawyerToLog(Lawyer? lawyer, string actionType)
    {
        if (lawyer == null) return _emptyJson;

        return JsonSerializer.SerializeToElement(new
        {
            action_type = actionType,
            user_id = lawyer.User?.ID,
            user_name = lawyer.User?.Name,
            user_nif = lawyer.User?.NIF,
            lawyer_id = lawyer.ID,
            lawyer_register = lawyer.ProfessionalRegister,
            status = lawyer.User?.IsActive
        });
    }

    #endregion
}