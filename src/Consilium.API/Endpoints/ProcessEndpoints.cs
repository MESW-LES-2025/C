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
    /// Registers all process-related endpoints and applies authorization policies.
    /// Ensures compliance with existing frontend route expectations.
    /// </summary>
    public static void MapProcessEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/processes").WithOpenApi();

        // Administrative and Legal Staff Access
        group.MapGet("/", GetAllProcesses).WithName("GetAllProcesses").RequireAuthorization("AdminOrLawyer");
        group.MapGet("/{id:guid}", GetProcessById).WithName("GetProcessById").RequireAuthorization("AdminOrLawyer");
        group.MapGet("/lawyer/{lawyerId:guid}", GetProcessesByLawyer).WithName("GetProcessesByLawyer").RequireAuthorization("AdminOrLawyer");
        group.MapGet("/lawyer/{lawyerId:guid}/with-documents", GetProcessesByLawyerWithDocuments).WithName("GetProcessesByLawyerWithDocuments").RequireAuthorization("AdminOrLawyer");

        // Management operations
        group.MapPost("/", CreateProcess).WithName("CreateProcess").RequireAuthorization("AdminOrLawyer");
        group.MapPost("/with-documents", CreateProcessWithDocuments).WithName("CreateProcessWithDocuments").RequireAuthorization("AdminOrLawyer");
        group.MapPatch("/{id:guid}", UpdateProcess).WithName("UpdateProcess").RequireAuthorization("AdminOrLawyer");
        group.MapPatch("/{id:guid}/with-documents", UpdateProcessWithDocuments).WithName("UpdateProcessWithDocuments").RequireAuthorization("AdminOrLawyer");
        group.MapDelete("/{id:guid}", DeleteProcess).WithName("DeleteProcess").RequireAuthorization("AdminOrLawyer");

        // General Access
        group.MapGet("/{id:guid}/with-documents", GetProcessByIdWithDocuments).WithName("GetProcessByIdWithDocuments").RequireAuthorization("Any");
        group.MapGet("/client/{clientId:guid}", GetProcessesByClient).WithName("GetProcessesByClient").RequireAuthorization("Any");
        group.MapGet("/client/{clientId:guid}/with-documents", GetProcessesByClientWithDocuments).WithName("GetProcessesByClientWithDocuments").RequireAuthorization("Any");
    }

    #endregion

    #region Queries (Read Operations)

    /// <summary>
    /// Retrieves a paginated list of all processes.
    /// </summary>
    private static async Task<IResult> GetAllProcesses(
        IProcessRepository repo,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var (processes, totalCount) = await repo.GetAll(search, page, limit, sortBy, sortOrder);
            return Results.Ok(new { data = processes.Select(MapToResponse), meta = new { totalCount, page, limit } });
        }
        catch (PostgresException)
        {
            return Results.Ok(new { data = Array.Empty<object>(), meta = new { totalCount = 0, page, limit } });
        }
    }

    /// <summary>
    /// Retrieves process details by ID. Triggers a read audit log.
    /// </summary>
    private static async Task<IResult> GetProcessById(Guid id, IProcessRepository repo, ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        var process = await repo.GetProcessDetailById(id, editorId);

        return process == null
            ? Results.NotFound(new { message = $"Process {id} not found" })
            : Results.Ok(MapToResponse(process));
    }

    /// <summary>
    /// Retrieves process details including document metadata. Triggers a read audit log.
    /// </summary>
    private static async Task<IResult> GetProcessByIdWithDocuments(Guid id, IProcessRepository repo, ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        var process = await repo.GetProcessDetailById(id, editorId);

        return process == null
            ? Results.NotFound(new { message = $"Process {id} not found" })
            : Results.Ok(MapWithDocsToResponse(process));
    }

    /// <summary>
    /// Fetches paginated processes for a specific client.
    /// </summary>
    private static async Task<IResult> GetProcessesByClient(
        Guid clientId,
        IProcessRepository repo,
        [FromQuery] string? search,
        int page = 1,
        int limit = 20)
    {
        var (processes, totalCount) = await repo.GetProcessesByClientId(clientId, search, page, limit, "name", "asc");
        return Results.Ok(new { data = processes.Select(MapToResponse), meta = new { totalCount, page, limit } });
    }

    /// <summary>
    /// Retrieves all client processes including associated documents.
    /// </summary>
    private static async Task<IResult> GetProcessesByClientWithDocuments(Guid clientId, IProcessRepository repo)
    {
        var processes = await repo.GetProcessesByClient(clientId, includeDocuments: true);
        return Results.Ok(new { data = processes.Select(MapWithDocsToResponse) });
    }

    /// <summary>
    /// Fetches paginated processes for a specific lawyer.
    /// </summary>
    private static async Task<IResult> GetProcessesByLawyer(
        Guid lawyerId,
        IProcessRepository repo,
        [FromQuery] string? search,
        int page = 1,
        int limit = 20)
    {
        var (processes, totalCount) = await repo.GetProcessesByLawyerId(lawyerId, search, page, limit, "name", "asc");
        return Results.Ok(new { data = processes.Select(MapToResponse), meta = new { totalCount, page, limit } });
    }

    /// <summary>
    /// Retrieves all lawyer processes including associated documents.
    /// </summary>
    private static async Task<IResult> GetProcessesByLawyerWithDocuments(Guid lawyerId, IProcessRepository repo)
    {
        var processes = await repo.GetProcessesByLawyer(lawyerId, includeDocuments: true);
        return Results.Ok(new { data = processes.Select(MapWithDocsToResponse) });
    }

    #endregion

    #region Commands (Write Operations)

    /// <summary>
    /// Creates a new process with validated relational integrity.
    /// </summary>
    private static async Task<IResult> CreateProcess(
        CreateProcessRequest request,
        IProcessRepository repo,
        IClientRepository clientRepo,
        ILawyerRepository lawyerRepo,
        ClaimsPrincipal userClaims)
    {
        var errorMessage = await ValidateClientAndLawyer(request.ClientId, request.LawyerId, clientRepo, lawyerRepo);
        if (errorMessage != null) return Results.BadRequest(new { message = errorMessage });

        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        var process = MapToEntity(request);
        var created = await repo.Create(process, editorId);

        return Results.Created($"/api/processes/{created.Id}", MapToResponse(created));
    }

    /// <summary>
    /// Creates a process and handles multi-part document uploads.
    /// </summary>
    private static async Task<IResult> CreateProcessWithDocuments(
        [FromForm] CreateProcessRequest request,
        IFormFileCollection files,
        IProcessRepository repo,
        IClientRepository clientRepo,
        ILawyerRepository lawyerRepo,
        ClaimsPrincipal userClaims)
    {
        var errorMessage = await ValidateClientAndLawyer(request.ClientId, request.LawyerId, clientRepo, lawyerRepo);
        if (errorMessage != null) return Results.BadRequest(new { message = errorMessage });

        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        var process = MapToEntity(request);
        if (files.Any()) await HandleFileUploads(process, files);

        var created = await repo.Create(process, editorId);
        return Results.Created($"/api/processes/{created.Id}", MapToResponse(created));
    }

    /// <summary>
    /// Updates process metadata and records change delta.
    /// </summary>
    private static async Task<IResult> UpdateProcess(
        Guid id,
        UpdateProcessRequest request,
        IProcessRepository repo,
        ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        var process = await repo.GetProcessById(id);
        if (process == null) return Results.NotFound(new { message = $"Process {id} not found" });

        ApplyChanges(process, request);
        await repo.Update(process, editorId);

        return Results.Ok(MapToResponse(process));
    }

    /// <summary>
    /// Updates process data and synchronizes document collection.
    /// </summary>
    private static async Task<IResult> UpdateProcessWithDocuments(
        Guid id,
        [FromForm] UpdateProcessRequest request,
        IFormFileCollection files,
        HttpContext context,
        IProcessRepository repo,
        AppDbContext db,
        ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        var process = await repo.GetProcessById(id);
        if (process == null) return Results.NotFound(new { message = $"Process {id} not found" });

        var deletedDocumentIds = context.Request.Form["deletedDocumentIds"].ToArray();
        if (deletedDocumentIds?.Length > 0)
        {
            var guidIds = deletedDocumentIds
                .Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty);

            await HandleFileDeletions(process.Id, guidIds, db);
        }

        ApplyChanges(process, request);
        if (files.Any()) await HandleFileUploads(process, files, db);

        await db.SaveChangesAsync();
        await repo.Update(process, editorId);

        return Results.Ok(MapWithDocsToResponse(process));
    }

    /// <summary>
    /// Permanently removes a process and logs the final state before deletion.
    /// </summary>
    private static async Task<IResult> DeleteProcess(Guid id, IProcessRepository repo, ClaimsPrincipal userClaims)
    {
        var editorId = GetUserIdFromClaims(userClaims);
        if (editorId == Guid.Empty) return Results.Unauthorized();

        var process = await repo.GetProcessById(id);
        if (process == null) return Results.NotFound(new { message = $"Process {id} not found" });

        await repo.Delete(id, editorId);
        return Results.NoContent();
    }

    #endregion

    #region Response Mappers

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

    private static ProcessWithDocumentsResponse MapWithDocsToResponse(Process p)
    {
        var baseInfo = MapToResponse(p);
        var docs = p.Documents?.Select(d => new DocumentResponse(
            DocumentId: d.Id,
            FileName: d.FileName,
            FileMimeType: d.FileMimeType,
            FileSize: d.FileSize,
            CreatedAt: d.CreatedAt,
            DownloadUrl: $"/api/documents/{d.Id}/download"
        )).ToList() ?? new List<DocumentResponse>();

        return new ProcessWithDocumentsResponse(
            baseInfo.ProcessId, baseInfo.Name, baseInfo.Number, baseInfo.ClientId, baseInfo.ClientName,
            baseInfo.LawyerId, baseInfo.LawyerName, baseInfo.AdversePartName, baseInfo.OpposingCounselName,
            baseInfo.CreatedAt, baseInfo.ClosedAt, baseInfo.Priority, baseInfo.CourtInfo,
            baseInfo.ProcessTypePhaseId, baseInfo.ProcessStatusId, baseInfo.Description,
            baseInfo.NextHearingDate, docs
        );
    }

    #endregion

    #region Inbound Mappers

    private static Process MapToEntity(CreateProcessRequest req) => new()
    {
        Name = req.Name,
        Number = req.Number,
        ClientId = req.ClientId,
        LawyerId = req.LawyerId,
        Priority = req.Priority,
        CourtInfo = req.CourtInfo,
        ProcessTypePhaseId = req.ProcessTypePhaseId,
        ProcessStatusId = req.ProcessStatusId,
        Description = req.Description ?? string.Empty,
        AdversePartName = req.AdversePartName,
        OpposingCounselName = req.OpposingCounselName,
        NextHearingDate = req.NextHearingDate?.ToUniversalTime()
    };

    private static void ApplyChanges(Process existing, UpdateProcessRequest req)
    {
        existing.Name = !string.IsNullOrWhiteSpace(req.Name) ? req.Name : existing.Name;
        existing.ClientId = req.ClientId ?? existing.ClientId;
        existing.LawyerId = req.LawyerId ?? existing.LawyerId;
        existing.ProcessStatusId = req.ProcessStatusId ?? existing.ProcessStatusId;
        existing.ProcessTypePhaseId = req.ProcessTypePhaseId ?? existing.ProcessTypePhaseId;
        existing.Priority = req.Priority ?? existing.Priority;
        existing.NextHearingDate = req.NextHearingDate?.ToUniversalTime() ?? existing.NextHearingDate;
        existing.ClosedAt = req.ClosedAt?.ToUniversalTime() ?? existing.ClosedAt;
        existing.Description = req.Description ?? existing.Description;
        existing.CourtInfo = req.CourtInfo ?? existing.CourtInfo;
        existing.AdversePartName = req.AdversePartName ?? existing.AdversePartName;
        existing.OpposingCounselName = req.OpposingCounselName ?? existing.OpposingCounselName;
    }

    #endregion

    #region Private Helpers

    private static async Task<string?> ValidateClientAndLawyer(Guid clientId, Guid lawyerId, IClientRepository clientRepo, ILawyerRepository lawyerRepo)
    {
        if (await clientRepo.GetById(clientId) == null) return "Client not found";
        if (await lawyerRepo.GetLawyerById(lawyerId) == null) return "Lawyer not found";
        return null;
    }

    private static Guid GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var claim = user.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static async Task HandleFileUploads(Process process, IFormFileCollection files, AppDbContext? db = null)
    {
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
        foreach (var id in docIds)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.ProcessId == processId);
            if (doc != null) db.Documents.Remove(doc);
        }
    }

    #endregion
}