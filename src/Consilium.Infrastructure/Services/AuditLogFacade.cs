using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Consilium.Infrastructure.Data;
using Consilium.Domain.Models;

namespace Consilium.Infrastructure.Services;

/// <summary>
/// Facade that abstracts audit logging complexity.
/// Provides clean, simple methods for logging CRUD operations on entities.
/// Automatically populates USER_LOG and PROCESS_LOG tables.
/// </summary>
public class AuditLogFacade
{
    private readonly AppDbContext _context;

    public AuditLogFacade(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Log when a Client is created
    /// </summary>
    public async Task AddCreateClientLogAsync(Guid clientId, Guid createdByUserId)
    {
        var client = await _context.Clients
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.ID == clientId);

        if (client == null)
            return;

        var newValue = JsonSerializer.SerializeToElement(new
        {
            client.ID,
            client.User?.Name,
            client.User?.Email,
            client.User?.NIF,
            client.Address,
            client.User?.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: clientId,
            updatedByUserId: createdByUserId,
            actionTypeName: "CLIENT_CREATED",
            oldValue: JsonSerializer.SerializeToElement(new { }),  // Empty object for creation
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when a Client is updated
    /// </summary>
    public async Task AddUpdateClientLogAsync(
        Guid clientId,
        Guid updatedByUserId,
        Dictionary<string, object?> oldValues,
        Dictionary<string, object?> newValues)
    {
        var oldValue = oldValues != null ? JsonSerializer.SerializeToElement(oldValues) : (JsonElement?)null;
        var newValue = JsonSerializer.SerializeToElement(newValues);

        await LogUserActionAsync(
            affectedUserId: clientId,
            updatedByUserId: updatedByUserId,
            actionTypeName: "CLIENT_UPDATED",
            oldValue: oldValue,
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when a Client is deleted
    /// </summary>
    public async Task AddDeleteClientLogAsync(Guid clientId, Guid deletedByUserId)
    {
        var client = await _context.Clients
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.ID == clientId);

        if (client == null)
            return;

        var oldValue = JsonSerializer.SerializeToElement(new
        {
            client.ID,
            client.User?.Name,
            client.User?.Email,
            client.User?.NIF,
            client.Address,
            client.User?.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: clientId,
            updatedByUserId: deletedByUserId,
            actionTypeName: "CLIENT_DELETED",
            oldValue: oldValue,
            newValue: JsonSerializer.SerializeToElement(new { })  // Empty object for deletion
        );
    }

    /// <summary>
    /// Log when a Lawyer is created
    /// </summary>
    public async Task AddCreateLawyerLogAsync(Guid lawyerId, Guid createdByUserId)
    {
        var lawyer = await _context.Lawyers
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.ID == lawyerId);

        if (lawyer == null)
            return;

        var newValue = JsonSerializer.SerializeToElement(new
        {
            lawyer.ID,
            lawyer.User?.Name,
            lawyer.User?.Email,
            lawyer.User?.NIF,
            lawyer.ProfessionalRegister,
            lawyer.User?.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: lawyerId,
            updatedByUserId: createdByUserId,
            actionTypeName: "LAWYER_CREATED",
            oldValue: JsonSerializer.SerializeToElement(new { }),  // Empty object for creation
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when a Lawyer is updated
    /// </summary>
    public async Task AddUpdateLawyerLogAsync(
        Guid lawyerId,
        Guid updatedByUserId,
        Dictionary<string, object?> oldValues,
        Dictionary<string, object?> newValues)
    {
        var oldValue = oldValues != null ? JsonSerializer.SerializeToElement(oldValues) : (JsonElement?)null;
        var newValue = JsonSerializer.SerializeToElement(newValues);

        await LogUserActionAsync(
            affectedUserId: lawyerId,
            updatedByUserId: updatedByUserId,
            actionTypeName: "LAWYER_UPDATED",
            oldValue: oldValue,
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when a Lawyer is deleted
    /// </summary>
    public async Task AddDeleteLawyerLogAsync(Guid lawyerId, Guid deletedByUserId)
    {
        var lawyer = await _context.Lawyers
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.ID == lawyerId);

        if (lawyer == null)
            return;

        var oldValue = JsonSerializer.SerializeToElement(new
        {
            lawyer.ID,
            lawyer.User?.Name,
            lawyer.User?.Email,
            lawyer.User?.NIF,
            lawyer.ProfessionalRegister,
            lawyer.User?.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: lawyerId,
            updatedByUserId: deletedByUserId,
            actionTypeName: "LAWYER_DELETED",
            oldValue: oldValue,
            newValue: JsonSerializer.SerializeToElement(new { })  // Empty object for deletion
        );
    }

    /// <summary>
    /// Log when a User is created
    /// </summary>
    public async Task AddCreateUserLogAsync(Guid userId, Guid createdByUserId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return;

        var newValue = JsonSerializer.SerializeToElement(new
        {
            user.ID,
            user.Name,
            user.Email,
            user.NIF,
            user.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: userId,
            updatedByUserId: createdByUserId,
            actionTypeName: "USER_CREATED",
            oldValue: JsonSerializer.SerializeToElement(new { }),  // Empty object for creation
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when a User is updated
    /// </summary>
    public async Task AddUpdateUserLogAsync(
        Guid userId,
        Guid updatedByUserId,
        Dictionary<string, object?> oldValues,
        Dictionary<string, object?> newValues)
    {
        var oldValue = oldValues != null ? JsonSerializer.SerializeToElement(oldValues) : (JsonElement?)null;
        var newValue = JsonSerializer.SerializeToElement(newValues);

        await LogUserActionAsync(
            affectedUserId: userId,
            updatedByUserId: updatedByUserId,
            actionTypeName: "USER_UPDATED",
            oldValue: oldValue,
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when a User is deleted
    /// </summary>
    public async Task AddDeleteUserLogAsync(Guid userId, Guid deletedByUserId)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return;

        var oldValue = JsonSerializer.SerializeToElement(new
        {
            user.ID,
            user.Name,
            user.Email,
            user.NIF,
            user.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: userId,
            updatedByUserId: deletedByUserId,
            actionTypeName: "USER_DELETED",
            oldValue: oldValue,
            newValue: JsonSerializer.SerializeToElement(new { })  // Empty object for deletion
        );
    }

    /// <summary>
    /// Log when an Admin is created
    /// </summary>
    public async Task AddCreateAdminLogAsync(Guid adminId, Guid createdByUserId)
    {
        var admin = await _context.Admins
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.ID == adminId);

        if (admin == null)
            return;

        var newValue = JsonSerializer.SerializeToElement(new
        {
            admin.ID,
            admin.User?.Name,
            admin.User?.Email,
            admin.User?.NIF,
            admin.StartedAt,
            admin.User?.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: adminId,
            updatedByUserId: createdByUserId,
            actionTypeName: "ADMIN_CREATED",
            oldValue: JsonSerializer.SerializeToElement(new { }),  // Empty object for creation
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when an Admin is updated
    /// </summary>
    public async Task AddUpdateAdminLogAsync(
        Guid adminId,
        Guid updatedByUserId,
        Dictionary<string, object?> oldValues,
        Dictionary<string, object?> newValues)
    {
        var oldValue = oldValues != null ? JsonSerializer.SerializeToElement(oldValues) : (JsonElement?)null;
        var newValue = JsonSerializer.SerializeToElement(newValues);

        await LogUserActionAsync(
            affectedUserId: adminId,
            updatedByUserId: updatedByUserId,
            actionTypeName: "ADMIN_UPDATED",
            oldValue: oldValue,
            newValue: newValue
        );
    }

    /// <summary>
    /// Log when an Admin is deleted
    /// </summary>
    public async Task AddDeleteAdminLogAsync(Guid adminId, Guid deletedByUserId)
    {
        var admin = await _context.Admins
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.ID == adminId);

        if (admin == null)
            return;

        var oldValue = JsonSerializer.SerializeToElement(new
        {
            admin.ID,
            admin.User?.Name,
            admin.User?.Email,
            admin.User?.NIF,
            admin.StartedAt,
            admin.User?.IsActive
        });

        await LogUserActionAsync(
            affectedUserId: adminId,
            updatedByUserId: deletedByUserId,
            actionTypeName: "ADMIN_DELETED",
            oldValue: oldValue,
            newValue: JsonSerializer.SerializeToElement(new { })  // Empty object for deletion
        );
    }

    // /// <summary>
    // /// Log when a Process is created
    // /// </summary>
    // public async Task AddCreateProcessLogAsync(Guid processId, Guid createdByUserId)
    // {
    //     var process = await _context.Processes.FindAsync(processId);

    //     if (process == null)
    //         return;

    //     var newValue = JsonSerializer.SerializeToElement(new
    //     {
    //         process.ID,
    //         process.ProcessNumber,
    //         process.ClientID,
    //         process.LawyerID,
    //         process.ProcessStateID,
    //         process.Description,
    //         process.CreatedAt
    //     });

    //     await LogProcessActionAsync(
    //         processId: processId,
    //         updatedByUserId: createdByUserId,
    //         actionTypeName: "PROCESS_CREATED",
    //         oldValue: null,
    //         newValue: newValue
    //     );
    // }

    // /// <summary>
    // /// Log when a Process is updated
    // /// </summary>
    // public async Task AddUpdateProcessLogAsync(
    //     Guid processId,
    //     Guid updatedByUserId,
    //     Dictionary<string, object?> oldValues,
    //     Dictionary<string, object?> newValues)
    // {
    //     var oldValue = oldValues != null ? JsonSerializer.SerializeToElement(oldValues) : (JsonElement?)null;
    //     var newValue = JsonSerializer.SerializeToElement(newValues);

    //     await LogProcessActionAsync(
    //         processId: processId,
    //         updatedByUserId: updatedByUserId,
    //         actionTypeName: "PROCESS_UPDATED",
    //         oldValue: oldValue,
    //         newValue: newValue
    //     );
    // }

    // /// <summary>
    // /// Log when a Process is deleted
    // /// </summary>
    // public async Task AddDeleteProcessLogAsync(Guid processId, Guid deletedByUserId)
    // {
    //     var process = await _context.Processes.FindAsync(processId);

    //     if (process == null)
    //         return;

    //     var oldValue = JsonSerializer.SerializeToElement(new
    //     {
    //         process.ID,
    //         process.ProcessNumber,
    //         process.ClientID,
    //         process.LawyerID,
    //         process.ProcessStateID,
    //         process.Description,
    //         process.CreatedAt
    //     });

    //     await LogProcessActionAsync(
    //         processId: processId,
    //         updatedByUserId: deletedByUserId,
    //         actionTypeName: "PROCESS_DELETED",
    //         oldValue: oldValue,
    //         newValue: null
    //     );
    // }

    private async Task LogUserActionAsync(
        Guid affectedUserId,
        Guid updatedByUserId,
        string actionTypeName,
        JsonElement? oldValue,
        JsonElement? newValue)
    {
        try
        {
            // Get or create action log type
            var actionLogType = await _context.ActionLogTypes
                .FirstOrDefaultAsync(alt => alt.Name == actionTypeName);

            if (actionLogType == null)
            {
                actionLogType = new ActionLogType
                {
                    ID = Guid.NewGuid(),
                    Name = actionTypeName
                };
                _context.ActionLogTypes.Add(actionLogType);
                await _context.SaveChangesAsync();
            }

            // Create the log entry
            var userLog = new UserLog
            {
                ID = Guid.NewGuid(),
                AffectedUserID = affectedUserId,
                UpdatedByID = updatedByUserId,
                ActionLogTypeID = actionLogType.ID,
                OldValue = oldValue,
                NewValue = newValue,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserLogs.Add(userLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[AUDIT LOG ERROR] {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    // /// <summary>
    // /// Core method for logging Process actions
    // /// </summary>
    // private async Task LogProcessActionAsync(
    //     Guid processId,
    //     Guid updatedByUserId,
    //     string actionTypeName,
    //     JsonElement? oldValue,
    //     JsonElement? newValue)
    // {
    //     // Get or create action log type
    //     var actionLogType = await _context.ActionLogTypes
    //         .FirstOrDefaultAsync(alt => alt.Name == actionTypeName);

    //     if (actionLogType == null)
    //     {
    //         actionLogType = new ActionLogType
    //         {
    //             ID = Guid.NewGuid(),
    //             Name = actionTypeName
    //         };
    //         _context.ActionLogTypes.Add(actionLogType);
    //         await _context.SaveChangesAsync();
    //     }

    //     // Create the log entry
    //     var processLog = new ProcessLog
    //     {
    //         ID = Guid.NewGuid(),
    //         ProcessID = processId,
    //         UpdatedByID = updatedByUserId,
    //         ActionLogTypeID = actionLogType.ID,
    //         OldValue = oldValue,
    //         NewValue = newValue,
    //         UpdatedAt = DateTime.UtcNow
    //     };

    //     _context.ProcessLogs.Add(processLog);
    //     await _context.SaveChangesAsync();
    // }
}
