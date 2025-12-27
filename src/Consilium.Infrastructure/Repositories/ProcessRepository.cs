using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.Infrastructure.Repositories
{
    public class ProcessRepository : IProcessRepository
    {
        private readonly AppDbContext _context;

        public ProcessRepository(AppDbContext context)
        {
            _context = context;
        }

        #region CRUD Operations
        // Methods in this region follow the CRUD order: Create, Read (All then Single), Update, Delete.

        // Creates a new process record and ensures it has a valid Guid
        public async Task<Process> Create(Process process)
        {
            if (process.Id == Guid.Empty)
                process.Id = Guid.NewGuid();

            _context.Processes.Add(process);
            await _context.SaveChangesAsync();

            var loaded = await GetById(process.Id);
            return loaded ?? process;
        }

        // Retrieves all processes with pagination, search, and sorting
        public async Task<(List<Process> Processes, int TotalCount)> GetAll(string? search, int page, int limit, string? sortBy, string? sortOrder)
        {
            var query = _context.Processes
                .Include(p => p.Client).ThenInclude(c => c!.User)
                .Include(p => p.Lawyer).ThenInclude(l => l!.User)
                .Include(p => p.Status)
                .Include(p => p.ProcessTypePhase).ThenInclude(ptp => ptp!.ProcessType)
                .AsQueryable();

            query = ApplySearchFilter(query, search);
            var totalCount = await query.CountAsync();
            query = ApplySorting(query, sortBy, sortOrder);

            var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
            return (processes, totalCount);
        }

        // Retrieves a specific process by ID excluding heavy document binary data
        public async Task<Process?> GetById(Guid id)
        {
            return await _context.Processes
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
        }

        // Updates an existing process record
        public async Task Update(Process process)
        {
            _context.Processes.Update(process);
            await _context.SaveChangesAsync();
        }

        // Deletes a process by its ID
        public async Task Delete(Guid id)
        {
            var existing = await _context.Processes.FindAsync(id);
            if (existing != null)
            {
                _context.Processes.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }
        #endregion

        #region Relationship Based Operations
        // Methods in this region are sorted alphabetically.

        // Removes a lawyer association from all their assigned processes
        public async Task DissociateProcessesFromLawyer(Guid id, Guid editorId)
        {
            var processes = await _context.Processes.Where(p => p.LawyerId == id).ToListAsync();
            foreach (var process in processes)
            {
                process.LawyerId = null;
            }
            await _context.SaveChangesAsync();
        }

        // Retrieves processes filtered by ClientId
        public async Task<(List<Process> Processes, int TotalCount)> GetProcessesByClientId(Guid clientId, string? search, int page, int limit, string? sortBy, string? sortOrder)
        {
            var query = _context.Processes
                .Include(p => p.Client).ThenInclude(c => c!.User)
                .Include(p => p.Status)
                .Where(p => p.ClientId == clientId)
                .AsQueryable();

            query = ApplySearchFilter(query, search);
            var totalCount = await query.CountAsync();
            query = ApplySorting(query, sortBy, sortOrder);

            var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
            return (processes, totalCount);
        }

        // Retrieves processes for a client including document metadata
        public async Task<List<Process>> GetProcessesByClientIdWithDocuments(Guid clientId)
        {
            return await _context.Processes
                .Where(p => p.ClientId == clientId)
                .Select(p => new Process
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

        // Retrieves processes filtered by LawyerId
        public async Task<(List<Process> Processes, int TotalCount)> GetProcessesByLawyerId(Guid lawyerId, string? search, int page, int limit, string? sortBy, string? sortOrder)
        {
            var query = _context.Processes
                .Include(p => p.Lawyer).ThenInclude(l => l!.User)
                .Include(p => p.Status)
                .Where(p => p.LawyerId == lawyerId)
                .AsQueryable();

            query = ApplySearchFilter(query, search);
            var totalCount = await query.CountAsync();
            query = ApplySorting(query, sortBy, sortOrder);

            var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
            return (processes, totalCount);
        }

        // Retrieves processes for a lawyer including document metadata
        public async Task<List<Process>> GetProcessesByLawyerIdWithDocuments(Guid lawyerId)
        {
            return await _context.Processes
                .Where(p => p.LawyerId == lawyerId)
                .Select(p => new Process
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
        #endregion

        #region Helpers
        // Methods in this region are sorted alphabetically.

        // Reusable search logic for process name, number, and court info
        private IQueryable<Process> ApplySearchFilter(IQueryable<Process> query, string? search)
        {
            if (string.IsNullOrWhiteSpace(search)) return query;

            return query.Where(p =>
                (p.Name != null && p.Name.Contains(search)) ||
                (p.Number != null && p.Number.Contains(search)) ||
                (p.CourtInfo != null && p.CourtInfo.Contains(search)));
        }

        // Reusable sorting logic
        private IQueryable<Process> ApplySorting(IQueryable<Process> query, string? sortBy, string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortBy)) return query;

            sortBy = sortBy.ToLower();
            bool desc = sortOrder?.ToLower() == "desc";

            return sortBy switch
            {
                "created" => desc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                "number" => desc ? query.OrderByDescending(p => p.Number) : query.OrderBy(p => p.Number),
                _ => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };
        }
        #endregion
    }
}