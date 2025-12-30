using System.Text.Json;
using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Repositories;

public class ProcessRepository : IProcessRepository
{
    private readonly AppDbContext _context;
    private readonly JsonElement _emptyJson = JsonSerializer.SerializeToElement(new { });

    public ProcessRepository(AppDbContext context)
    {
        _context = context;
    }

    #region CRUD Operations

    /// <summary>
    /// Persists a new process and records the initial state in PROCESS_CREATED.
    /// </summary>
    public async Task<Process> Create(Process process, Guid editorId)
    {
        process.Id = Guid.NewGuid();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Processes.Add(process);

            var snapshot = WrapProcessToLog(process, "CREATE");
            await RecordLogAsync("PROCESS_CREATED", process.Id, snapshot, snapshot, editorId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return process;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// General search with pagination and sorting.
    /// </summary>
    public async Task<(List<Process> Processes, int TotalCount)> GetAll(
        string? search, int page, int limit, string? sortBy, string? sortOrder)
    {
        IQueryable<Process> query = _context.Processes
            .Include(p => p.Client).ThenInclude(c => c!.User)
            .Include(p => p.Lawyer).ThenInclude(l => l!.User)
            .Include(p => p.Status)
            .Include(p => p.ProcessTypePhase).ThenInclude(ptp => ptp!.ProcessType)
            .AsNoTracking();

        query = ApplySearchFilter(query, search);
        var totalCount = await query.CountAsync();

        query = ApplySorting(query, sortBy, sortOrder);
        var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

        return (processes, totalCount);
    }

    /// <summary>
    /// Lightweight fetch for internal logic.
    /// </summary>
    public async Task<Process?> GetProcessById(Guid id)
    {
        return await _context.Processes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Full profile fetch with mandatory READ audit log.
    /// </summary>
    public async Task<Process?> GetProcessDetailById(Guid id, Guid editorId)
    {
        var process = await _context.Processes
            .Include(p => p.Client).ThenInclude(c => c!.User)
            .Include(p => p.Lawyer).ThenInclude(l => l!.User)
            .Include(p => p.Status)
            .Include(p => p.ProcessTypePhase).ThenInclude(ptp => ptp!.ProcessType)
            .Include(p => p.ProcessTypePhase).ThenInclude(ptp => ptp!.ProcessPhase)
            .Where(p => p.Id == id)
            .Select(p => new Process
            {
                Id = p.Id,
                Name = p.Name,
                Number = p.Number,
                ClientId = p.ClientId,
                Client = p.Client,
                LawyerId = p.LawyerId,
                Lawyer = p.Lawyer,
                AdversePartName = p.AdversePartName,
                OpposingCounselName = p.OpposingCounselName,
                CreatedAt = p.CreatedAt,
                ClosedAt = p.ClosedAt,
                Priority = p.Priority,
                CourtInfo = p.CourtInfo,
                ProcessTypePhaseId = p.ProcessTypePhaseId,
                ProcessTypePhase = p.ProcessTypePhase,
                ProcessStatusId = p.ProcessStatusId,
                Status = p.Status,
                Description = p.Description,
                NextHearingDate = p.NextHearingDate,
                Documents = p.Documents.Select(d => new Document
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FileMimeType = d.FileMimeType,
                    FileSize = d.FileSize,
                    CreatedAt = d.CreatedAt,
                    ProcessId = d.ProcessId
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (process != null)
        {
            var snapshot = WrapProcessToLog(process, "READ");
            await RecordLogAsync("PROCESS_READ", process.Id, snapshot, snapshot, editorId);
            await _context.SaveChangesAsync();
        }

        return process;
    }

    /// <summary>
    /// Updates process and records the delta in PROCESS_UPDATED.
    /// </summary>
    public async Task Update(Process process, Guid editorId)
    {
        var oldProcess = await _context.Processes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == process.Id);

        if (oldProcess == null) throw new KeyNotFoundException($"Process {process.Id} not found");

        var oldValue = WrapProcessToLog(oldProcess, "UPDATE");
        var newValue = WrapProcessToLog(process, "UPDATE");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Processes.Update(process);
            await RecordLogAsync("PROCESS_UPDATED", process.Id, oldValue, newValue, editorId);

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
    /// Deletes process and records final state in PROCESS_DELETED.
    /// </summary>
    public async Task Delete(Guid id, Guid editorId)
    {
        var process = await _context.Processes.FindAsync(id);
        if (process == null) throw new KeyNotFoundException($"Process {id} not found");

        var snapshot = WrapProcessToLog(process, "DELETE");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Processes.Remove(process);
            await RecordLogAsync("PROCESS_DELETED", id, snapshot, _emptyJson, editorId);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Relationship Based Operations

    /// <summary>
    /// Batch dissociation for lawyer deletion scenarios.
    /// </summary>
    public async Task DissociateProcessesFromLawyer(Guid id, Guid editorId)
    {
        var processes = await _context.Processes.Where(p => p.LawyerId == id).ToListAsync();
        if (!processes.Any()) return;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var process in processes)
            {
                var oldValue = WrapProcessToLog(process, "DISSOCIATE");
                process.LawyerId = null;
                var newValue = WrapProcessToLog(process, "DISSOCIATE");

                await RecordLogAsync("PROCESS_UPDATED", process.Id, oldValue, newValue, editorId);
            }

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
    /// Unified client query: light list or list with documents.
    /// </summary>
    public async Task<List<Process>> GetProcessesByClient(Guid clientId, bool includeDocuments = false)
    {
        var query = _context.Processes.AsNoTracking().Where(p => p.ClientId == clientId);

        if (includeDocuments)
        {
            return await query.Select(p => new Process
            {
                Id = p.Id,
                Name = p.Name,
                Documents = p.Documents.Select(d => new Document
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    CreatedAt = d.CreatedAt
                }).ToList()
            }).ToListAsync();
        }

        return await query.Select(p => new Process { Id = p.Id, Name = p.Name }).ToListAsync();
    }

    public async Task<(List<Process> Processes, int TotalCount)> GetProcessesByClientId(
        Guid clientId, string? search, int page, int limit, string? sortBy, string? sortOrder)
    {
        IQueryable<Process> query = _context.Processes
            .Include(p => p.Client).ThenInclude(c => c!.User)
            .Include(p => p.Status)
            .Where(p => p.ClientId == clientId)
            .AsNoTracking();

        query = ApplySearchFilter(query, search);
        var totalCount = await query.CountAsync();
        query = ApplySorting(query, sortBy, sortOrder);

        var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
        return (processes, totalCount);
    }

    /// <summary>
    /// Unified lawyer query: light list or list with documents.
    /// </summary>
    public async Task<List<Process>> GetProcessesByLawyer(Guid lawyerId, bool includeDocuments = false)
    {
        var query = _context.Processes.AsNoTracking().Where(p => p.LawyerId == lawyerId);

        if (includeDocuments)
        {
            return await query.Select(p => new Process
            {
                Id = p.Id,
                Name = p.Name,
                Documents = p.Documents.Select(d => new Document
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    CreatedAt = d.CreatedAt
                }).ToList()
            }).ToListAsync();
        }

        return await query.Select(p => new Process { Id = p.Id, Name = p.Name }).ToListAsync();
    }

    public async Task<(List<Process> Processes, int TotalCount)> GetProcessesByLawyerId(
        Guid lawyerId, string? search, int page, int limit, string? sortBy, string? sortOrder)
    {
        IQueryable<Process> query = _context.Processes
            .Include(p => p.Lawyer).ThenInclude(l => l!.User)
            .Include(p => p.Status)
            .Where(p => p.LawyerId == lawyerId)
            .AsNoTracking();

        query = ApplySearchFilter(query, search);
        var totalCount = await query.CountAsync();
        query = ApplySorting(query, sortBy, sortOrder);

        var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
        return (processes, totalCount);
    }

    #endregion

    #region Private Helpers

    private async Task RecordLogAsync(string actionTypeId, Guid entityId, JsonElement oldVal, JsonElement newVal, Guid editorId)
    {
        // Finds the ID based on the professional naming convention in action_log_type
        var actionType = await _context.ActionLogTypes.AsNoTracking()
            .FirstAsync(alt => alt.Name == actionTypeId);

        _context.ProcessLogs.Add(new ProcessLog
        {
            ID = Guid.NewGuid(),
            ProcessID = entityId,
            UpdatedByID = editorId,
            ActionLogTypeID = actionType.ID,
            OldValue = oldVal,
            NewValue = newVal
        });
    }

    private JsonElement WrapProcessToLog(Process? process, string actionType)
    {
        if (process == null) return _emptyJson;
        return JsonSerializer.SerializeToElement(new
        {
            action_type = actionType,
            process_id = process.Id,
            process_name = process.Name,
            process_number = process.Number,
            client_id = process.ClientId,
            lawyer_id = process.LawyerId,
            status_id = process.ProcessStatusId,
            priority = process.Priority
        });
    }

    private IQueryable<Process> ApplySearchFilter(IQueryable<Process> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(p =>
            (p.Name != null && p.Name.Contains(search)) ||
            (p.Number != null && p.Number.Contains(search)) ||
            (p.CourtInfo != null && p.CourtInfo.Contains(search)));
    }

    private IQueryable<Process> ApplySorting(IQueryable<Process> query, string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return query;
        bool desc = sortOrder?.ToLower() == "desc";

        return sortBy.ToLower() switch
        {
            "created" => desc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "number" => desc ? query.OrderByDescending(p => p.Number) : query.OrderBy(p => p.Number),
            _ => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };
    }

    #endregion
}