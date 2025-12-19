using System.Text.Json;
using Consilium.Infrastructure.Data;
using Consilium.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Services;

public class AuditLogFacade
{
    private readonly AppDbContext _context;

    public AuditLogFacade(AppDbContext context)
    {
        _context = context;
    }

    private async Task<ActionLogType> GetActionTypeAsync(string name)
    {
        // Query only, no insertion attempt
        var actionType = await _context.ActionLogTypes
            .FirstOrDefaultAsync(a => a.Name == name);

        if (actionType == null)
        {
            // Since this is a domain table, this error helps identify
            // if the required SQL INSERTs were not executed in the database.
            throw new InvalidOperationException($"Action type '{name}' not found in database.");
        }

        return actionType;
    }

    private async Task LogUserActionAsync(Guid? affectedUserId, Guid? updatedByUserId, string actionTypeName, JsonElement? oldValue, JsonElement? newValue)
    {
        // Fetch the action type from the domain table
        ActionLogType actionLogType = await GetActionTypeAsync(actionTypeName);

        var userLog = new UserLog
        {
            ID = Guid.NewGuid(),
            AffectedUserID = affectedUserId,
            UpdatedByID = updatedByUserId,
            ActionLogTypeID = actionLogType.ID, // Uses the integer ID from the database
            OldValue = oldValue,
            NewValue = newValue
        };

        _context.UserLogs.Add(userLog);
        await _context.SaveChangesAsync();
    }

    // Overloads for entity create/update/delete - these will be called by endpoints

    public async Task AddCreateClientLogAsync(Guid clientId, Guid createdByUserId)
    {
        var client = await _context.Clients
            .Include(c => c.User)
            .ThenInclude(u => u.Phones)
            .FirstOrDefaultAsync(c => c.ID == clientId);
        if (client == null) return;

        var newValue = JsonSerializer.SerializeToElement(new
        {
            ClientID = client.ID,
            UserID = client.User?.ID,
            UserName = client.User?.Name,
            UserEmail = client.User?.Email,
            UserNIF = client.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = client.User?.IsActive,
            ClientAddress = client.Address,
            Phones = client.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        var emptyJson = JsonSerializer.SerializeToElement(new { });

        await LogUserActionAsync(clientId, createdByUserId, "CLIENT_CREATED", emptyJson, newValue);
    }

    public async Task AddDeleteClientLogAsync(Guid clientId, Guid deletedByUserId)
    {
        var client = await _context.Clients
            .Include(c => c.User)
            .ThenInclude(u => u.Phones)
            .FirstOrDefaultAsync(c => c.ID == clientId);
        if (client == null) return;

        var oldValue = JsonSerializer.SerializeToElement(new
        {
            ClientID = client.ID,
            UserID = client.User?.ID,
            UserName = client.User?.Name,
            UserEmail = client.User?.Email,
            UserNIF = client.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = client.User?.IsActive,
            ClientAddress = client.Address,
            Phones = client.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        var emptyJson = JsonSerializer.SerializeToElement(new { });
        await LogUserActionAsync(clientId, deletedByUserId, "CLIENT_DELETED", oldValue, emptyJson);
    }

    // Generic overload to accept snapshots for updates
    public async Task AddUpdateClientLogAsync(Guid clientId, Guid updatedByUserId, JsonElement? oldSnapshot, JsonElement? newSnapshot)
    {
        await LogUserActionAsync(clientId, updatedByUserId, "CLIENT_UPDATED", oldSnapshot, newSnapshot);
    }

    // Lawyer
    public async Task AddCreateLawyerLogAsync(Guid lawyerId, Guid createdByUserId)
    {
        var lawyer = await _context.Lawyers
            .Include(l => l.User)
            .ThenInclude(u => u.Phones)
            .FirstOrDefaultAsync(l => l.ID == lawyerId);
        if (lawyer == null) return;

        var newValue = JsonSerializer.SerializeToElement(new
        {
            LawyerID = lawyer.ID,
            UserID = lawyer.User?.ID,
            UserName = lawyer.User?.Name,
            UserEmail = lawyer.User?.Email,
            UserNIF = lawyer.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = lawyer.User?.IsActive,
            LawyerProfessionalRegister = lawyer.ProfessionalRegister,
            Phones = lawyer.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        var emptyJson = JsonSerializer.SerializeToElement(new { });
        await LogUserActionAsync(lawyerId, createdByUserId, "LAWYER_CREATED", emptyJson, newValue);
    }

    public async Task AddDeleteLawyerLogAsync(Guid lawyerId, Guid deletedByUserId)
    {
        var lawyer = await _context.Lawyers
            .Include(l => l.User)
            .ThenInclude(u => u.Phones)
            .FirstOrDefaultAsync(l => l.ID == lawyerId);
        if (lawyer == null) return;

        var oldValue = JsonSerializer.SerializeToElement(new
        {
            LawyerID = lawyer.ID,
            UserID = lawyer.User?.ID,
            UserName = lawyer.User?.Name,
            UserEmail = lawyer.User?.Email,
            UserNIF = lawyer.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = lawyer.User?.IsActive,
            LawyerProfessionalRegister = lawyer.ProfessionalRegister,
            Phones = lawyer.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        var emptyJson = JsonSerializer.SerializeToElement(new { });
        await LogUserActionAsync(lawyerId, deletedByUserId, "LAWYER_DELETED", oldValue, emptyJson);
    }

    public async Task AddUpdateLawyerLogAsync(Guid lawyerId, Guid updatedByUserId, JsonElement? oldSnapshot, JsonElement? newSnapshot)
    {
        await LogUserActionAsync(lawyerId, updatedByUserId, "LAWYER_UPDATED", oldSnapshot, newSnapshot);
    }

    // Admin
    public async Task AddCreateAdminLogAsync(Guid adminId, Guid createdByUserId)
    {
        var admin = await _context.Admins
            .Include(a => a.User)
            .ThenInclude(u => u.Phones)
            .FirstOrDefaultAsync(a => a.ID == adminId);
        if (admin == null) return;

        var newValue = JsonSerializer.SerializeToElement(new
        {
            AdminID = admin.ID,
            UserID = admin.User?.ID,
            UserName = admin.User?.Name,
            UserEmail = admin.User?.Email,
            UserNIF = admin.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = admin.User?.IsActive,
            AdminStartedAt = admin.StartedAt,
            Phones = admin.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        var emptyJson = JsonSerializer.SerializeToElement(new { });
        await LogUserActionAsync(adminId, createdByUserId, "ADMIN_CREATED", emptyJson, newValue);
    }

    public async Task AddDeleteAdminLogAsync(Guid adminId, Guid deletedByUserId)
    {
        var admin = await _context.Admins
            .Include(a => a.User)
            .ThenInclude(u => u.Phones)
            .FirstOrDefaultAsync(a => a.ID == adminId);
        if (admin == null) return;

        var oldValue = JsonSerializer.SerializeToElement(new
        {
            AdminID = admin.ID,
            UserID = admin.User?.ID,
            UserName = admin.User?.Name,
            UserEmail = admin.User?.Email,
            UserNIF = admin.User?.NIF,
            UserPassword = "***REDACTED***",
            UserIsActive = admin.User?.IsActive,
            AdminStartedAt = admin.StartedAt,
            Phones = admin.User?.Phones?.Select(p => new { p.ID, p.Number, p.CountryCode, p.IsMain })
        });

        var emptyJson = JsonSerializer.SerializeToElement(new { });
        await LogUserActionAsync(adminId, deletedByUserId, "ADMIN_DELETED", oldValue, emptyJson);
    }

    public async Task AddUpdateAdminLogAsync(Guid adminId, Guid updatedByUserId, JsonElement? oldSnapshot, JsonElement? newSnapshot)
    {
        await LogUserActionAsync(adminId, updatedByUserId, "ADMIN_UPDATED", oldSnapshot, newSnapshot);
    }

    /// <summary>
    /// Right to be forgotten: blank *both* JSON columns to empty JSON object ({}), and nullify UpdatedByID where needed
    /// </summary>
    public async Task AnonymizeUserLogsAsync(Guid userId)
    {
        var logsAffected = await _context.UserLogs
            .Where(l => l.AffectedUserID == userId)
            .ToListAsync();

        var logsUpdatedBy = await _context.UserLogs
            .Where(l => l.UpdatedByID == userId)
            .ToListAsync();

        var emptyJson = JsonSerializer.SerializeToElement(new { });

        foreach (var log in logsAffected)
        {
            log.OldValue = emptyJson;
            log.NewValue = emptyJson;
            log.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var log in logsUpdatedBy)
        {
            log.UpdatedByID = null;
            log.OldValue = emptyJson;
            log.NewValue = emptyJson;
            log.UpdatedAt = DateTime.UtcNow;
        }

        if (logsAffected.Count > 0 || logsUpdatedBy.Count > 0)
            await _context.SaveChangesAsync();
    }
}
