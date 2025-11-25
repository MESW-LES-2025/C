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

        public async Task<(List<Process> Processes, int TotalCount)> GetByClientId(Guid clientId, string? search, int page, int limit, string? sortBy, string? sortOrder)
        {
            var query = _context.Processes
                .Include(p => p.Client)
                .Include(p => p.Lawyer)
                .Include(p => p.Status)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessType)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessPhase)
                .Where(p => p.ClientId == clientId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => (p.Name ?? string.Empty).Contains(search) || (p.Number ?? string.Empty).Contains(search) || (p.CourtInfo ?? string.Empty).Contains(search));
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = sortBy?.ToLower();
                sortOrder = sortOrder?.ToLower() ?? "asc";

                if (sortBy == "number")
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.Number) : query.OrderBy(p => p.Number);
                else if (sortBy == "created")
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
                else
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);
            }

            var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
            return (processes, totalCount);
        }

        public async Task<(List<Process> Processes, int TotalCount)> GetByLawyerId(Guid lawyerId, string? search, int page, int limit, string? sortBy, string? sortOrder)
        {
            var query = _context.Processes
                .Include(p => p.Client)
                .Include(p => p.Lawyer)
                .Include(p => p.Status)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessType)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessPhase)
                .Where(p => p.LawyerId == lawyerId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => (p.Name ?? string.Empty).Contains(search) || (p.Number ?? string.Empty).Contains(search) || (p.CourtInfo ?? string.Empty).Contains(search));
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = sortBy?.ToLower();
                sortOrder = sortOrder?.ToLower() ?? "asc";

                if (sortBy == "number")
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.Number) : query.OrderBy(p => p.Number);
                else if (sortBy == "created")
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
                else
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);
            }

            var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();
            return (processes, totalCount);
        }

        public async Task<List<Process>> GetByClientIdWithDocuments(Guid clientId)
        {
            var processes = await _context.Processes
                .Include(p => p.Client)
                .Include(p => p.Lawyer)
                .Include(p => p.Status)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessType)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessPhase)
                .Where(p => p.ClientId == clientId)
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
                }).ToListAsync();

            return processes;
        }

        public async Task<List<Process>> GetByLawyerIdWithDocuments(Guid lawyerId)
        {
            var processes = await _context.Processes
                .Include(p => p.Client)
                .Include(p => p.Lawyer)
                .Include(p => p.Status)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessType)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessPhase)
                .Where(p => p.LawyerId == lawyerId)
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
                }).ToListAsync();

            return processes;
        }

        public async Task<Process?> GetById(Guid id)
        {
            // Query with Select to avoid loading the heavy 'File' byte array
            // Only load document metadata (Id, FileName, FileSize, etc.)
            var process = await _context.Processes
                .Include(p => p.Client)
                .Include(p => p.Lawyer)
                .Include(p => p.Status)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessType)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessPhase)
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
                    // Project Documents without the File byte array
                    Documents = p.Documents.Select(d => new Document
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        FileMimeType = d.FileMimeType,
                        FileSize = d.FileSize,
                        CreatedAt = d.CreatedAt,
                        ProcessId = d.ProcessId
                        // Note: We do NOT select d.File here to avoid loading bytes
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return process;
        }

        public async Task<(List<Process> Processes, int TotalCount)> GetAll(
            string? search,
            int page,
            int limit,
            string? sortBy,
            string? sortOrder)
        {
            var query = _context.Processes
                .Include(p => p.Client)
                .Include(p => p.Lawyer)
                .Include(p => p.Status)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessType)
                .Include(p => p.ProcessTypePhase)
                    .ThenInclude(ptp => ptp!.ProcessPhase)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                // Null-coalesce strings to be defensive with EF mapping
                query = query.Where(p => (p.Name ?? string.Empty).Contains(search) || (p.Number ?? string.Empty).Contains(search) || (p.CourtInfo ?? string.Empty).Contains(search));
            }

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                sortBy = sortBy?.ToLower();
                sortOrder = sortOrder?.ToLower() ?? "asc";

                if (sortBy == "number")
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.Number) : query.OrderBy(p => p.Number);
                else if (sortBy == "created")
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt);
                else
                    query = sortOrder == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);
            }

            var processes = await query.Skip((page - 1) * limit).Take(limit).ToListAsync();

            return (processes, totalCount);
        }

        public async Task<Process> Create(Process process)
        {
            // CreatedAt will be set by default in model; ensure ID is set to new Guid (DB also can generate via gen_random_uuid)
            if (process.Id == Guid.Empty)
                process.Id = Guid.NewGuid();

            _context.Processes.Add(process);
            await _context.SaveChangesAsync();

            // Reload with includes
            var loaded = await GetById(process.Id);
            return loaded ?? process;
        }

        public async Task Update(Process process)
        {
            _context.Processes.Update(process);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            var existing = await _context.Processes.FindAsync(id);
            if (existing == null)
                throw new KeyNotFoundException($"Process with ID {id} not found");

            _context.Processes.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}
