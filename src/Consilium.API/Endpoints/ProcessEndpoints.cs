using Consilium.API.Dtos;
using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Consilium.Infrastructure.Data;
using System.Security.Claims;

namespace Consilium.API.Endpoints;

public static class ProcessEndpoints
{
    #region Route Mapping

    /// <summary>
    /// Registers process-related routes and applies security policies.
    /// </summary>
    public static void MapProcessEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/processes").WithOpenApi();

        // Administrative and Legal Staff Access
        group.MapGet("/", GetAllProcesses).WithName("GetAllProcesses").RequireAuthorization("AdminOrLawyer");
        group.MapGet("/{id:guid}", GetProcessById).WithName("GetProcessById").RequireAuthorization("AdminOrLawyer");
        group.MapGet("/lawyer/{lawyerId:guid}", GetProcessesByLawyer).WithName("GetProcessesByLawyer").RequireAuthorization("AdminOrLawyer");
        group.MapGet("/lawyer/{lawyerId:guid}/with-documents", GetProcessesByLawyerWithDocuments).WithName("GetProcessesByLawyerWithDocuments").RequireAuthorization("AdminOrLawyer");


        // Management operations for creating, updating and removing legal processes.
        group.MapPost("/", CreateProcess).WithName("CreateProcess").RequireAuthorization("AdminOrLawyer");
        group.MapPost("/with-documents", CreateProcessWithDocuments).WithName("CreateProcessWithDocuments").RequireAuthorization("AdminOrLawyer");
        group.MapPatch("/{id:guid}", UpdateProcess).WithName("UpdateProcess").RequireAuthorization("AdminOrLawyer");
        group.MapPatch("/{id:guid}/with-documents", UpdateProcessWithDocuments).WithName("UpdateProcessWithDocuments").RequireAuthorization("AdminOrLawyer");
        group.MapDelete("/{id:guid}", DeleteProcess).WithName("DeleteProcess").RequireAuthorization("AdminOrLawyer");

        // General Access (Clients or Assigned Parties)
        group.MapGet("/{id:guid}/with-documents", GetProcessByIdWithDocuments).WithName("GetProcessByIdWithDocuments").RequireAuthorization("Any");
        group.MapGet("/client/{clientId:guid}", GetProcessesByClient).WithName("GetProcessesByClient").RequireAuthorization("Any");
        group.MapGet("/client/{clientId:guid}/with-documents", GetProcessesByClientWithDocuments).WithName("GetProcessesByClientWithDocuments").RequireAuthorization("Any");
    }

    #endregion

    #region Queries (Read Operations)

    /// <summary>
    /// Fetches all processes with pagination and filtering support.
    /// </summary>
    private static async Task<IResult> GetAllProcesses(IProcessRepository repo, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? sortBy = "name", [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var (processes, totalCount) = await repo.GetAll(search, page, limit, sortBy, sortOrder);
            return Results.Ok(new { data = processes.Select(MapToResponse), meta = new { totalCount, page, limit } });
        }
        catch (PostgresException)
        {
            // Fail-safe for initial database setup or inaccessible tables
            return Results.Ok(new { data = Array.Empty<object>(), meta = new { totalCount = 0, page, limit } });
        }
    }

    /// <summary>
    /// Retrieves a specific process by its unique identifier.
    /// </summary>
    private static async Task<IResult> GetProcessById(Guid id, IProcessRepository repo)
    {
        var process = await repo.GetById(id);
        return process == null ? Results.NotFound(new { message = $"Process {id} not found" }) : Results.Ok(MapToResponse(process));
    }

    /// <summary>
    /// Retrieves a specific process including its document list.
    /// </summary>
    private static async Task<IResult> GetProcessByIdWithDocuments(Guid id, IProcessRepository repo)
    {
        var process = await repo.GetById(id);
        return process == null ? Results.NotFound(new { message = $"Process {id} not found" }) : Results.Ok(MapWithDocsToResponse(process));
    }

    /// <summary>
    /// Retrieves all processes belonging to a client.
    /// </summary>
    private static async Task<IResult> GetProcessesByClient(Guid clientId, IProcessRepository repo, [FromQuery] string? search, int page = 1, int limit = 20)
    {
        var (processes, totalCount) = await repo.GetProcessesByClientId(clientId, search, page, limit, "name", "asc");
        return Results.Ok(new { data = processes.Select(MapToResponse), meta = new { totalCount, page, limit } });
    }

    /// <summary>
    /// Retrieves all processes and documents belonging to a client.
    /// </summary>
    private static async Task<IResult> GetProcessesByClientWithDocuments(Guid clientId, IProcessRepository repo)
    {
        var processes = await repo.GetProcessesByClientIdWithDocuments(clientId);
        return Results.Ok(new { data = processes.Select(MapWithDocsToResponse) });
    }

    /// <summary>
    /// Retrieves all processes assigned to a lawyer.
    /// </summary>
    private static async Task<IResult> GetProcessesByLawyer(Guid lawyerId, IProcessRepository repo, [FromQuery] string? search, int page = 1, int limit = 20)
    {
        var (processes, totalCount) = await repo.GetProcessesByLawyerId(lawyerId, search, page, limit, "name", "asc");
        return Results.Ok(new { data = processes.Select(MapToResponse), meta = new { totalCount, page, limit } });
    }

    /// <summary>
    /// Retrieves all processes and documents assigned to a lawyer.
    /// </summary>
    private static async Task<IResult> GetProcessesByLawyerWithDocuments(Guid lawyerId, IProcessRepository repo)
    {
        var processes = await repo.GetProcessesByLawyerIdWithDocuments(lawyerId);
        return Results.Ok(new { data = processes.Select(MapWithDocsToResponse) });
    }

    #endregion

    #region Commands (Write Operations)

    /// <summary>
    /// Creates a new legal process.
    /// </summary>
    private static async Task<IResult> CreateProcess(CreateProcessRequest request, IProcessRepository repo, IClientRepository clientRepo, ILawyerRepository lawyerRepo, AppDbContext db, HttpContext context)
    {
        var (process, error) = await ValidateAndMapProcess(request, clientRepo, lawyerRepo, db, GetEditorId(context));
        if (error != null) return error;

        return await SaveAndReturnCreated(process!, repo);
    }

    /// <summary>
    /// Creates a process with associated files using multi-part form data.
    /// </summary>
    private static async Task<IResult> CreateProcessWithDocuments([FromForm] CreateProcessWithDocumentsRequest request, IProcessRepository repo, IClientRepository clientRepo, ILawyerRepository lawyerRepo, AppDbContext db, HttpContext context)
    {
        // 1. DTO Construction for form-data compatibility
        var createDto = new CreateProcessRequest(
            Name: request.Name ?? "",
            Number: request.Number,
            ClientId: request.ClientId,
            LawyerId: request.LawyerId,
            ProcessTypePhaseId: request.ProcessTypePhaseId,
            ProcessStatusId: request.ProcessStatusId,
            NextHearingDate: request.NextHearingDate,
            AdversePartName: request.AdversePartName,
            OpposingCounselName: request.OpposingCounselName,
            Priority: request.Priority,
            CourtInfo: request.CourtInfo,
            Description: request.Description
        );

        // 2. Validation and Mapping
        var (process, error) = await ValidateAndMapProcess(createDto, clientRepo, lawyerRepo, db, GetEditorId(context));
        if (error != null) return error;

        // 3. Document attachment
        if (request.Files?.Any() == true) await HandleFileUploads(process!, request.Files);

        return await SaveAndReturnCreated(process!, repo);
    }

    /// <summary>
    /// Updates process metadata.
    /// </summary>
    private static async Task<IResult> UpdateProcess(Guid id, UpdateProcessRequest request, IProcessRepository repo, AppDbContext db, IClientRepository clientRepo, ILawyerRepository lawyerRepo, HttpContext context)
    {
        var existing = await repo.GetById(id);
        if (existing == null) return Results.NotFound(new { message = $"Process {id} not found" });

        var error = await ApplyUpdatesToProcess(existing, request, clientRepo, lawyerRepo, GetEditorId(context));
        if (error != null) return error;

        await repo.Update(existing);
        return Results.Ok(MapToResponse(existing));
    }

    /// <summary>
    /// Updates process data and manages document syncing (Add/Remove).
    /// </summary>
    private static async Task<IResult> UpdateProcessWithDocuments(Guid id, [FromForm] UpdateProcessWithDocumentsRequest request, IProcessRepository repo, AppDbContext db, IClientRepository clientRepo, ILawyerRepository lawyerRepo, HttpContext context)
    {
        var existing = await repo.GetById(id);
        if (existing == null) return Results.NotFound(new { message = $"Process {id} not found" });

        // 1. Handle Document Deletions
        if (request.DeletedDocumentIds?.Any() == true)
        {
            var guidIds = request.DeletedDocumentIds.Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty);
            await HandleFileDeletions(existing.Id, guidIds, db);
        }

        // 2. Manual parsing for form-data update
        var updateDto = new UpdateProcessRequest(
            Name: request.Name,
            Number: request.Number,
            ClientId: request.ClientId != null ? Guid.Parse(request.ClientId) : null,
            LawyerId: request.LawyerId != null ? Guid.Parse(request.LawyerId) : null,
            ProcessTypePhaseId: request.ProcessTypePhaseId != null ? int.Parse(request.ProcessTypePhaseId) : null,
            ProcessStatusId: request.ProcessStatusId != null ? int.Parse(request.ProcessStatusId) : null,
            NextHearingDate: request.NextHearingDate != null ? DateTime.Parse(request.NextHearingDate) : null,
            ClosedAt: request.ClosedAt != null ? DateTime.Parse(request.ClosedAt) : null,
            AdversePartName: request.AdversePartName,
            OpposingCounselName: request.OpposingCounselName,
            Priority: request.Priority != null ? short.Parse(request.Priority) : null,
            CourtInfo: request.CourtInfo,
            Description: request.Description
        );

        // 3. Apply updates and handle new uploads
        var error = await ApplyUpdatesToProcess(existing, updateDto, clientRepo, lawyerRepo, GetEditorId(context));
        if (error != null) return error;

        if (request.Files?.Any() == true) await HandleFileUploads(existing, request.Files, db);

        await db.SaveChangesAsync();
        return Results.Ok(MapWithDocsToResponse(existing));
    }

    /// <summary>
    /// Deletes a process record.
    /// </summary>
    private static async Task<IResult> DeleteProcess(Guid id, IProcessRepository repo)
    {
        try
        {
            var existing = await repo.GetById(id);
            if (existing == null) return Results.NotFound();

            await repo.Delete(id);
            return Results.NoContent();
        }
        catch { return Results.BadRequest(new { message = "Error deleting process. Ensure it has no linked records." }); }
    }

    #endregion

    #region Private Helpers & Mappers

    private static Guid GetEditorId(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static async Task<(Process? Process, IResult? Error)> ValidateAndMapProcess(CreateProcessRequest req, IClientRepository clientRepo, ILawyerRepository lawyerRepo, AppDbContext db, Guid editorId)
    {
        // 1. Mandatory Data Validation
        if (string.IsNullOrWhiteSpace(req.Name)) return (null, Results.BadRequest("Process name is required"));

        // 2. Foreign Key Check
        var lawyer = await lawyerRepo.GetLawyerById(req.LawyerId);
        if (lawyer == null) return (null, Results.BadRequest("Lawyer not found"));

        var client = await clientRepo.GetById(req.ClientId);
        if (client == null) return (null, Results.BadRequest("Client not found"));

        // 3. Entity Mapping
        var process = new Process
        {
            Id = Guid.NewGuid(),
            Name = req.Name,
            Number = req.Number ?? "",
            ClientId = req.ClientId,
            LawyerId = req.LawyerId,
            Priority = req.Priority,
            ProcessTypePhaseId = req.ProcessTypePhaseId,
            ProcessStatusId = req.ProcessStatusId,
            NextHearingDate = req.NextHearingDate?.ToUniversalTime() // DB timestamp sync
        };
        return (process, null);
    }

    private static async Task<IResult?> ApplyUpdatesToProcess(Process existing, UpdateProcessRequest req, IClientRepository clientRepo, ILawyerRepository lawyerRepo, Guid editorId)
    {
        // 1. Basic Fields Update
        if (!string.IsNullOrWhiteSpace(req.Name)) existing.Name = req.Name;

        // 2. Entity Relations Update
        if (req.ClientId.HasValue && await clientRepo.GetById(req.ClientId.Value) != null)
            existing.ClientId = req.ClientId.Value;

        if (req.LawyerId.HasValue)
        {
            if (await lawyerRepo.GetLawyerById(req.LawyerId.Value) != null) existing.LawyerId = req.LawyerId.Value;
            else return Results.BadRequest("Lawyer not found");
        }

        // 3. Status and Date Sync (UTC)
        if (req.NextHearingDate.HasValue) existing.NextHearingDate = req.NextHearingDate.Value.ToUniversalTime();
        if (req.ClosedAt.HasValue) existing.ClosedAt = req.ClosedAt.Value.ToUniversalTime();

        if (req.ProcessStatusId.HasValue) existing.ProcessStatusId = req.ProcessStatusId.Value;
        if (req.ProcessTypePhaseId.HasValue) existing.ProcessTypePhaseId = req.ProcessTypePhaseId.Value;
        if (req.Priority.HasValue) existing.Priority = req.Priority.Value;

        existing.Description = req.Description ?? existing.Description;
        existing.CourtInfo = req.CourtInfo ?? existing.CourtInfo;

        return null;
    }

    private static async Task HandleFileUploads(Process process, IFormFileCollection files, AppDbContext? db = null)
    {
        // Iterates through uploaded files, converting to byte arrays
        foreach (var file in files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var doc = new Document
            {
                Id = Guid.NewGuid(),
                ProcessId = process.Id,
                File = ms.ToArray(),
                FileName = file.FileName,
                FileMimeType = file.ContentType,
                FileSize = ms.Length,
                CreatedAt = DateTime.UtcNow
            };

            if (db != null) db.Documents.Add(doc); else process.Documents.Add(doc);
        }
    }

    private static async Task HandleFileDeletions(Guid processId, IEnumerable<Guid> docIds, AppDbContext db)
    {
        // Removes specific documents associated with the process
        foreach (var id in docIds)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.ProcessId == processId);
            if (doc != null) db.Documents.Remove(doc);
        }
    }

    private static async Task<IResult> SaveAndReturnCreated(Process process, IProcessRepository repo)
    {
        var created = await repo.Create(process);
        return Results.Created($"/api/processes/{created.Id}", MapToResponse(created));
    }

    private static ProcessResponse MapToResponse(Process p) => new(
        ProcessId: p.Id,
        Name: p.Name,
        Number: p.Number,
        ClientId: p.ClientId,
        ClientName: p.Client?.User?.Name ?? "Unknown",
        LawyerId: p.LawyerId ?? Guid.Empty,
        LawyerName: p.Lawyer?.User?.Name ?? "Unassigned",
        AdversePartName: p.AdversePartName,
        OpposingCounselName: p.OpposingCounselName,
        CreatedAt: p.CreatedAt,
        ClosedAt: p.ClosedAt,
        Priority: p.Priority,
        CourtInfo: p.CourtInfo,
        ProcessTypePhaseId: p.ProcessTypePhaseId,
        ProcessStatusId: p.ProcessStatusId,
        Description: p.Description,
        NextHearingDate: p.NextHearingDate
    );

    private static ProcessWithDocumentsResponse MapWithDocsToResponse(Process p) => new(
        ProcessId: p.Id,
        Name: p.Name,
        Number: p.Number,
        ClientId: p.ClientId,
        ClientName: p.Client?.User?.Name ?? "Unknown",
        LawyerId: p.LawyerId ?? Guid.Empty,
        LawyerName: p.Lawyer?.User?.Name ?? "Unassigned",
        AdversePartName: p.AdversePartName,
        OpposingCounselName: p.OpposingCounselName,
        CreatedAt: p.CreatedAt,
        ClosedAt: p.ClosedAt,
        Priority: p.Priority,
        CourtInfo: p.CourtInfo,
        ProcessTypePhaseId: p.ProcessTypePhaseId,
        ProcessStatusId: p.ProcessStatusId,
        Description: p.Description,
        NextHearingDate: p.NextHearingDate,
        Documents: p.Documents?.Select(d => new DocumentResponse(
            DocumentId: d.Id,
            FileName: d.FileName,
            FileMimeType: d.FileMimeType,
            FileSize: d.FileSize,
            CreatedAt: d.CreatedAt,
            DownloadUrl: $"/api/documents/{d.Id}/download"
        )).ToList() ?? new List<DocumentResponse>()
    );

    #endregion
}