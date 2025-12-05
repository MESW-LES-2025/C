using Consilium.API.Dtos;
using Consilium.Application.Interfaces;
using Consilium.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Consilium.Infrastructure.Data;
namespace Consilium.API.Endpoints;

public static class ProcessEndpoints
{
    public static void MapProcessEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/processes")
            .WithName("Processes")
            .WithOpenApi();

        group.MapGet("/", GetAllProcesses)
            .WithName("GetAllProcesses")
            .WithDescription("Retrieve all processes")
            .RequireAuthorization("AdminOrLawyer");

        group.MapGet("/{id:guid}", GetProcessById)
            .WithName("GetProcessById")
            .WithDescription("Retrieve a process by ID")
            .RequireAuthorization("AdminOrLawyer");

        group.MapGet("/{id:guid}/with-documents", GetProcessByIdWithDocuments)
            .WithName("GetProcessByIdWithDocuments")
            .WithDescription("Retrieve a process by ID with its associated documents")
            .RequireAuthorization("Any");

        // Client-specific endpoints
        group.MapGet("/client/{clientId:guid}", GetProcessesByClient)
            .WithName("GetProcessesByClient")
            .WithDescription("Retrieve all processes for a specific client")
            .RequireAuthorization("Any");

        group.MapGet("/client/{clientId:guid}/with-documents", GetProcessesByClientWithDocuments)
            .WithName("GetProcessesByClientWithDocuments")
            .WithDescription("Retrieve all processes and documents for a specific client")
            .RequireAuthorization("Any");

        group.MapPost("/", CreateProcess)
            .WithName("CreateProcess")
            .WithDescription("Create a new legal process")
            .RequireAuthorization("AdminOrLawyer");
        // Create a process with associated documents via multipart/form-data
        group.MapPost("/with-documents", CreateProcessWithDocuments)
            .WithName("CreateProcessWithDocuments")
            .WithDescription("Create a new legal process and upload documents (multipart/form-data)")
            .DisableAntiforgery() // Disable CSRF for testing file uploads
            .RequireAuthorization("AdminOrLawyer");

        group.MapPatch("/{id:guid}", UpdateProcess)
            .WithName("UpdateProcess")
            .WithDescription("Update process")
            .RequireAuthorization("AdminOrLawyer");
        group.MapPatch("/{id:guid}/with-documents", UpdateProcessWithDocuments)
            .WithName("UpdateProcessWithDocuments")
            .WithDescription("Update process and upload/delete documents (multipart/form-data)")
            .DisableAntiforgery() // Disable CSRF for testing file uploads
            .RequireAuthorization("AdminOrLawyer");

        group.MapDelete("/{id:guid}", DeleteProcess)
            .WithName("DeleteProcess")
            .WithDescription("Delete a process")
            .RequireAuthorization("AdminOrLawyer");

        // Lawyer-specific endpoints
        group.MapGet("/lawyer/{lawyerId:guid}", GetProcessesByLawyer)
            .WithName("GetProcessesByLawyer")
            .WithDescription("Retrieve all processes for a specific lawyer")
            .RequireAuthorization("AdminOrLawyer");

        group.MapGet("/lawyer/{lawyerId:guid}/with-documents", GetProcessesByLawyerWithDocuments)
            .WithName("GetProcessesByLawyerWithDocuments")
            .WithDescription("Retrieve all processes and documents for a specific lawyer")
            .RequireAuthorization("AdminOrLawyer");
    }

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

            var response = processes.Select(p => new ProcessResponse(
                ProcessId: p.Id,
                Name: p.Name,
                Number: p.Number,
                ClientId: p.ClientId,
                LawyerId: p.LawyerId,
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
            ));

            return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            // Missing relation/column (DB not seeded) -> return empty list instead of 500
            return Results.Ok(new { data = Array.Empty<object>(), meta = new { totalCount = 0, page, limit } });
        }
    }

    private static async Task<IResult> GetProcessById(Guid id, IProcessRepository repo)
    {
        Process? process;
        try
        {
            process = await repo.GetById(id);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            // Missing relation/column (DB not seeded) -> return 404 so client sees an empty result
            return Results.NotFound(new { message = $"Process with ID {id} not found" });
        }

        if (process == null)
            return Results.NotFound(new { message = $"Process with ID {id} not found" });

        var response = new ProcessResponse(
            ProcessId: process.Id,
            Name: process.Name,
            Number: process.Number,
            ClientId: process.ClientId,
            LawyerId: process.LawyerId,
            AdversePartName: process.AdversePartName,
            OpposingCounselName: process.OpposingCounselName,
            CreatedAt: process.CreatedAt,
            ClosedAt: process.ClosedAt,
            Priority: process.Priority,
            CourtInfo: process.CourtInfo,
            ProcessTypePhaseId: process.ProcessTypePhaseId,
            ProcessStatusId: process.ProcessStatusId,
            Description: process.Description,
            NextHearingDate: process.NextHearingDate
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> GetProcessByIdWithDocuments(Guid id, IProcessRepository repo, ILogger<Program> logger, AppDbContext db)
    {
        Process? process;
        try
        {
            process = await repo.GetById(id);
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            // Missing relation/column (DB not seeded) -> return 404
            return Results.NotFound(new { message = $"Process with ID {id} not found" });
        }

        if (process == null)
            return Results.NotFound(new { message = $"Process with ID {id} not found" });

        var documents = process.Documents?.Select(d => new DocumentResponse(
            DocumentId: d.Id,
            FileName: d.FileName,
            FileMimeType: d.FileMimeType,
            FileSize: d.FileSize,
            CreatedAt: d.CreatedAt,
            DownloadUrl: $"/api/documents/{d.Id}/download"
        )).ToList() ?? new List<DocumentResponse>();

        var response = new ProcessWithDocumentsResponse(
            ProcessId: process.Id,
            Name: process.Name,
            Number: process.Number,
            ClientId: process.ClientId,
            LawyerId: process.LawyerId,
            AdversePartName: process.AdversePartName,
            OpposingCounselName: process.OpposingCounselName,
            CreatedAt: process.CreatedAt,
            ClosedAt: process.ClosedAt,
            Priority: process.Priority,
            CourtInfo: process.CourtInfo,
            ProcessTypePhaseId: process.ProcessTypePhaseId,
            ProcessStatusId: process.ProcessStatusId,
            Description: process.Description,
            NextHearingDate: process.NextHearingDate,
            Documents: documents
        );

        return Results.Ok(response);
    }

    private static async Task<IResult> GetProcessesByClient(Guid clientId,
        IProcessRepository repo,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var (processes, totalCount) = await repo.GetByClientId(clientId, search, page, limit, sortBy, sortOrder);
            var response = processes.Select(p => new ProcessResponse(
                ProcessId: p.Id,
                Name: p.Name,
                Number: p.Number,
                ClientId: p.ClientId,
                LawyerId: p.LawyerId,
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
            ));

            return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            return Results.Ok(new { data = Array.Empty<object>(), meta = new { totalCount = 0, page, limit } });
        }
    }

    private static async Task<IResult> GetProcessesByClientWithDocuments(Guid clientId, IProcessRepository repo)
    {
        var processes = await repo.GetByClientIdWithDocuments(clientId);
        var response = processes.Select(p => new ProcessWithDocumentsResponse(
            ProcessId: p.Id,
            Name: p.Name,
            Number: p.Number,
            ClientId: p.ClientId,
            LawyerId: p.LawyerId,
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
        ));

        return Results.Ok(new { data = response });
    }

    private static async Task<IResult> GetProcessesByLawyer(Guid lawyerId,
        IProcessRepository repo,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var (processes, totalCount) = await repo.GetByLawyerId(lawyerId, search, page, limit, sortBy, sortOrder);
            var response = processes.Select(p => new ProcessResponse(
                ProcessId: p.Id,
                Name: p.Name,
                Number: p.Number,
                ClientId: p.ClientId,
                LawyerId: p.LawyerId,
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
            ));

            return Results.Ok(new { data = response, meta = new { totalCount, page, limit } });
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            return Results.Ok(new { data = Array.Empty<object>(), meta = new { totalCount = 0, page, limit } });
        }
    }

    private static async Task<IResult> GetProcessesByLawyerWithDocuments(Guid lawyerId, IProcessRepository repo)
    {
        var processes = await repo.GetByLawyerIdWithDocuments(lawyerId);
        var response = processes.Select(p => new ProcessWithDocumentsResponse(
            ProcessId: p.Id,
            Name: p.Name,
            Number: p.Number,
            ClientId: p.ClientId,
            LawyerId: p.LawyerId,
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
        ));

        return Results.Ok(new { data = response });
    }

    private static async Task<IResult> CreateProcess(
        CreateProcessRequest request,
        IProcessRepository repo,
        IClientRepository clientRepo,
        ILawyerRepository lawyerRepo,
        AppDbContext db)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Name is required" });

        if (string.IsNullOrWhiteSpace(request.Number))
            return Results.BadRequest(new { message = "Number is required" });

        if (request.Priority < 0)
            return Results.BadRequest(new { message = "Priority must be non-negative" });

        // Validate foreign keys
        var client = await clientRepo.GetById(request.ClientId);
        if (client == null)
            return Results.BadRequest(new { message = $"Client with ID {request.ClientId} not found" });

        var lawyer = await lawyerRepo.GetById(request.LawyerId);
        if (lawyer == null)
            return Results.BadRequest(new { message = $"Lawyer with ID {request.LawyerId} not found" });

        var typePhaseExists = await db.ProcessTypePhases.AnyAsync(x => x.Id == request.ProcessTypePhaseId);
        if (!typePhaseExists)
            return Results.BadRequest(new { message = $"ProcessTypePhase with ID {request.ProcessTypePhaseId} not found" });

        var statusExists = await db.ProcessStatuses.AnyAsync(x => x.Id == request.ProcessStatusId);
        if (!statusExists)
            return Results.BadRequest(new { message = $"ProcessStatus with ID {request.ProcessStatusId} not found" });

        var process = new Process
        {
            Name = request.Name,
            Number = request.Number,
            ClientId = request.ClientId,
            LawyerId = request.LawyerId,
            AdversePartName = request.AdversePartName,
            OpposingCounselName = request.OpposingCounselName,
            Priority = request.Priority,
            CourtInfo = request.CourtInfo,
            ProcessTypePhaseId = request.ProcessTypePhaseId,
            ProcessStatusId = request.ProcessStatusId,
            NextHearingDate = request.NextHearingDate.HasValue 
                ? DateTime.SpecifyKind(request.NextHearingDate.Value, DateTimeKind.Utc) 
                : null,
            Description = request.Description
        };

        try
        {
            var created = await repo.Create(process);

            var response = new ProcessResponse(
            ProcessId: created.Id,
            Name: created.Name,
            Number: created.Number,
            ClientId: created.ClientId,
            LawyerId: created.LawyerId,
            AdversePartName: created.AdversePartName,
            OpposingCounselName: created.OpposingCounselName,
            CreatedAt: created.CreatedAt,
            ClosedAt: created.ClosedAt,
            Priority: created.Priority,
            CourtInfo: created.CourtInfo,
            ProcessTypePhaseId: created.ProcessTypePhaseId,
            ProcessStatusId: created.ProcessStatusId,
            Description: created.Description,
            NextHearingDate: created.NextHearingDate
        );

            return Results.Created($"/api/processes/{created.Id}", response);
        }
        catch (DbUpdateException dbEx)
        {
            if (dbEx.InnerException is PostgresException pgEx)
            {
                if (pgEx.SqlState == "23503") // foreign key violation
                    return Results.BadRequest(new { message = "Invalid foreign key (client/lawyer/status/typePhase) provided" });

                if (pgEx.SqlState == "23505") // unique violation
                    return Results.Conflict(new { message = "Process number must be unique for this client and lawyer" });
            }

            throw;
        }
    }

    private static async Task<IResult> CreateProcessWithDocuments(
        [FromForm] CreateProcessWithDocumentsRequest request,
        IProcessRepository repo,
        IClientRepository clientRepo,
        ILawyerRepository lawyerRepo,
        AppDbContext db,
        ILogger<Program> logger)
    {
        // Reuse the same validation as CreateProcess
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { message = "Name is required" });

        if (string.IsNullOrWhiteSpace(request.Number))
            return Results.BadRequest(new { message = "Number is required" });

        if (request.Priority < 0)
            return Results.BadRequest(new { message = "Priority must be non-negative" });

        var client = await clientRepo.GetById(request.ClientId);
        if (client == null)
            return Results.BadRequest(new { message = $"Client with ID {request.ClientId} not found" });

        var lawyer = await lawyerRepo.GetById(request.LawyerId);
        if (lawyer == null)
            return Results.BadRequest(new { message = $"Lawyer with ID {request.LawyerId} not found" });

        var typePhaseExists = await db.ProcessTypePhases.AnyAsync(x => x.Id == request.ProcessTypePhaseId);
        if (!typePhaseExists)
            return Results.BadRequest(new { message = $"ProcessTypePhase with ID {request.ProcessTypePhaseId} not found" });

        var statusExists = await db.ProcessStatuses.AnyAsync(x => x.Id == request.ProcessStatusId);
        if (!statusExists)
            return Results.BadRequest(new { message = $"ProcessStatus with ID {request.ProcessStatusId} not found" });

        var process = new Process
        {
            Name = request.Name,
            Number = request.Number,
            ClientId = request.ClientId,
            LawyerId = request.LawyerId,
            AdversePartName = request.AdversePartName,
            OpposingCounselName = request.OpposingCounselName,
            Priority = request.Priority,
            CourtInfo = request.CourtInfo,
            ProcessTypePhaseId = request.ProcessTypePhaseId,
            ProcessStatusId = request.ProcessStatusId,
            NextHearingDate = request.NextHearingDate.HasValue 
                ? DateTime.SpecifyKind(request.NextHearingDate.Value, DateTimeKind.Utc) 
                : null,
            Description = request.Description
        };

        // Handle files
        logger.LogInformation("[FILES DEBUG] Received request - Files count: {Count}", request.Files?.Count ?? 0);
        if (request.Files != null && request.Files.Count > 0)
        {
            logger.LogInformation("[FILES DEBUG] Processing {Count} file(s)", request.Files.Count);
            foreach (var file in request.Files)
            {
                logger.LogInformation("[FILES DEBUG] File: {FileName}, Size: {Length}, ContentType: {ContentType}", 
                    file.FileName, file.Length, file.ContentType);
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var doc = new Document
                {
                    Id = Guid.NewGuid(),
                    File = ms.ToArray(),
                    FileName = Path.GetFileName(file.FileName),
                    FileMimeType = file.ContentType ?? "application/octet-stream",
                    FileSize = ms.Length,
                    CreatedAt = DateTime.UtcNow, // Explicitly set UTC time for PostgreSQL
                    ProcessId = process.Id
                };
                logger.LogInformation("[FILES DEBUG] Created document with ID: {Id}, Size: {Size}", doc.Id, doc.FileSize);
                process.Documents.Add(doc);
            }
        }
        else
        {
            logger.LogInformation("[FILES DEBUG] No files in request");
        }

        try
        {
            var created = await repo.Create(process);

            var response = new ProcessResponse(
                ProcessId: created.Id,
                Name: created.Name,
                Number: created.Number,
                ClientId: created.ClientId,
                LawyerId: created.LawyerId,
                AdversePartName: created.AdversePartName,
                OpposingCounselName: created.OpposingCounselName,
                CreatedAt: created.CreatedAt,
                ClosedAt: created.ClosedAt,
                Priority: created.Priority,
                CourtInfo: created.CourtInfo,
                ProcessTypePhaseId: created.ProcessTypePhaseId,
                ProcessStatusId: created.ProcessStatusId,
                Description: created.Description,
                NextHearingDate: created.NextHearingDate
            );

            return Results.Created($"/api/processes/{created.Id}", response);
        }
        catch (DbUpdateException dbEx)
        {
            if (dbEx.InnerException is PostgresException pgEx)
            {
                if (pgEx.SqlState == "23503") // foreign key violation
                    return Results.BadRequest(new { message = "Invalid foreign key (client/lawyer/status/typePhase) provided" });

                if (pgEx.SqlState == "23505") // unique violation
                    return Results.Conflict(new { message = "Process number must be unique for this client and lawyer" });
            }

            throw;
        }
    }

    private static async Task<IResult> UpdateProcess(Guid id, UpdateProcessRequest request, IProcessRepository repo, AppDbContext db, IClientRepository clientRepo, ILawyerRepository lawyerRepo)
    {
        var existing = await repo.GetById(id);
        if (existing == null)
            return Results.NotFound(new { message = $"Process with ID {id} not found" });

        // Fix DateTime Kind issues for PostgreSQL - ensure CreatedAt is UTC
        if (existing.CreatedAt.Kind != DateTimeKind.Utc)
            existing.CreatedAt = DateTime.SpecifyKind(existing.CreatedAt, DateTimeKind.Utc);
        if (existing.ClosedAt.HasValue && existing.ClosedAt.Value.Kind != DateTimeKind.Utc)
            existing.ClosedAt = DateTime.SpecifyKind(existing.ClosedAt.Value, DateTimeKind.Utc);
        if (existing.NextHearingDate.HasValue && existing.NextHearingDate.Value.Kind != DateTimeKind.Utc)
            existing.NextHearingDate = DateTime.SpecifyKind(existing.NextHearingDate.Value, DateTimeKind.Utc);

        if (!string.IsNullOrWhiteSpace(request.Name))
            existing.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Number))
            existing.Number = request.Number;
        if (request.ClientId.HasValue)
        {
            existing.ClientId = request.ClientId.Value;
            var clientExists = await clientRepo.GetById(request.ClientId.Value);
            if (clientExists == null)
                return Results.BadRequest(new { message = $"Client with ID {request.ClientId.Value} not found" });
        }
        if (request.LawyerId.HasValue)
        {
            existing.LawyerId = request.LawyerId.Value;
            var lawyerExists = await lawyerRepo.GetById(request.LawyerId.Value);
            if (lawyerExists == null)
                return Results.BadRequest(new { message = $"Lawyer with ID {request.LawyerId.Value} not found" });
        }
        if (!string.IsNullOrWhiteSpace(request.AdversePartName))
            existing.AdversePartName = request.AdversePartName;
        if (!string.IsNullOrWhiteSpace(request.OpposingCounselName))
            existing.OpposingCounselName = request.OpposingCounselName;
        if (request.Priority.HasValue)
            existing.Priority = request.Priority.Value;
        if (!string.IsNullOrWhiteSpace(request.CourtInfo))
            existing.CourtInfo = request.CourtInfo;
        if (request.ProcessTypePhaseId.HasValue)
        {
            existing.ProcessTypePhaseId = request.ProcessTypePhaseId.Value;
            var typePhaseExists = await db.ProcessTypePhases.AnyAsync(x => x.Id == request.ProcessTypePhaseId.Value);
            if (!typePhaseExists)
                return Results.BadRequest(new { message = $"ProcessTypePhase with ID {request.ProcessTypePhaseId.Value} not found" });
        }
        if (request.ProcessStatusId.HasValue)
        {
            existing.ProcessStatusId = request.ProcessStatusId.Value;
            var statusExists = await db.ProcessStatuses.AnyAsync(x => x.Id == request.ProcessStatusId.Value);
            if (!statusExists)
                return Results.BadRequest(new { message = $"ProcessStatus with ID {request.ProcessStatusId.Value} not found" });
        }
        if (request.NextHearingDate.HasValue)
            existing.NextHearingDate = DateTime.SpecifyKind(request.NextHearingDate.Value, DateTimeKind.Utc);
        if (!string.IsNullOrWhiteSpace(request.Description))
            existing.Description = request.Description;
        if (request.ClosedAt.HasValue)
            existing.ClosedAt = DateTime.SpecifyKind(request.ClosedAt.Value, DateTimeKind.Utc);

        await db.SaveChangesAsync();
        
        var updated = await repo.GetById(id);

        var response = new ProcessResponse(
            ProcessId: updated!.Id,
            Name: updated.Name,
            Number: updated.Number,
            ClientId: updated.ClientId,
            LawyerId: updated.LawyerId,
            AdversePartName: updated.AdversePartName,
            OpposingCounselName: updated.OpposingCounselName,
            CreatedAt: updated.CreatedAt,
            ClosedAt: updated.ClosedAt,
            Priority: updated.Priority,
            CourtInfo: updated.CourtInfo,
            ProcessTypePhaseId: updated.ProcessTypePhaseId,
            ProcessStatusId: updated.ProcessStatusId,
            Description: updated.Description,
            NextHearingDate: updated.NextHearingDate
        );

        return Results.Ok(response);
    }

private static async Task<IResult> UpdateProcessWithDocuments(
    Guid id,
    [FromForm] UpdateProcessWithDocumentsRequest request,
    IProcessRepository repo,
    AppDbContext db,
    IClientRepository clientRepo,
    ILawyerRepository lawyerRepo)
{
    // 1. Load the entity
    var existing = await repo.GetById(id);
    
    if (existing == null)
        return Results.NotFound(new { message = $"Process with ID {id} not found" });

    // Fix DateTime Kind issues
    if (existing.CreatedAt.Kind != DateTimeKind.Utc)
        existing.CreatedAt = DateTime.SpecifyKind(existing.CreatedAt, DateTimeKind.Utc);
    if (existing.ClosedAt.HasValue && existing.ClosedAt.Value.Kind != DateTimeKind.Utc)
        existing.ClosedAt = DateTime.SpecifyKind(existing.ClosedAt.Value, DateTimeKind.Utc);
    if (existing.NextHearingDate.HasValue && existing.NextHearingDate.Value.Kind != DateTimeKind.Utc)
        existing.NextHearingDate = DateTime.SpecifyKind(existing.NextHearingDate.Value, DateTimeKind.Utc);

    // 2. Handle Document Deletion (FIXED for List<string>)
    if (request.DeletedDocumentIds != null && request.DeletedDocumentIds.Count > 0)
    {
        var deleteIds = new List<Guid>();
        
        // Iterate over the List directly
        foreach (var docIdStr in request.DeletedDocumentIds)
        {
            // Handle cases where a single list item might contain commas (e.g. "id1,id2")
            // just in case the form binding did something weird, though usually not needed for List<string>
            var parts = docIdStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                if (Guid.TryParse(part.Trim(), out var parsed))
                    deleteIds.Add(parsed);
                else
                    return Results.BadRequest(new { message = $"Invalid document ID format: {part}" });
            }
        }

        // Verify and Remove
        foreach (var docId in deleteIds)
        {
            var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == docId && d.ProcessId == existing.Id);
            
            if (doc != null)
            {
                db.Documents.Remove(doc);
            }
            else
            {
                return Results.BadRequest(new { message = $"Document {docId} not found or does not belong to this process" });
            }
        }
    }

    // 3. Handle Property Updates
    if (!string.IsNullOrWhiteSpace(request.Name))
        existing.Name = request.Name;
    if (!string.IsNullOrWhiteSpace(request.Number))
        existing.Number = request.Number;
    
    if (!string.IsNullOrWhiteSpace(request.ClientId))
    {
        if (!Guid.TryParse(request.ClientId, out var clientId))
            return Results.BadRequest(new { message = $"Invalid ClientId format" });
        
        // FIX: Use GetById instead of Exists
        var client = await clientRepo.GetById(clientId);
        if (client == null)
             return Results.BadRequest(new { message = $"Client with ID {clientId} not found" });

        existing.ClientId = clientId;
    }
    
    if (!string.IsNullOrWhiteSpace(request.LawyerId))
    {
        if (!Guid.TryParse(request.LawyerId, out var lawyerId))
            return Results.BadRequest(new { message = $"Invalid LawyerId format" });
            
        // FIX: Use GetById instead of Exists
        var lawyer = await lawyerRepo.GetById(lawyerId);
        if (lawyer == null)
             return Results.BadRequest(new { message = $"Lawyer with ID {lawyerId} not found" });

        existing.LawyerId = lawyerId;
    }
    
    if (!string.IsNullOrWhiteSpace(request.AdversePartName))
        existing.AdversePartName = request.AdversePartName;
    if (!string.IsNullOrWhiteSpace(request.OpposingCounselName))
        existing.OpposingCounselName = request.OpposingCounselName;
    
    if (!string.IsNullOrWhiteSpace(request.Priority))
    {
        if (!short.TryParse(request.Priority, out var priority))
            return Results.BadRequest(new { message = $"Invalid Priority format" });
        existing.Priority = priority;
    }
    
    if (!string.IsNullOrWhiteSpace(request.CourtInfo))
        existing.CourtInfo = request.CourtInfo;
    
    if (!string.IsNullOrWhiteSpace(request.ProcessTypePhaseId))
    {
        if (!int.TryParse(request.ProcessTypePhaseId, out var typePhaseId))
            return Results.BadRequest(new { message = $"Invalid ProcessTypePhaseId format" });
            
        if (!await db.ProcessTypePhases.AnyAsync(x => x.Id == typePhaseId))
            return Results.BadRequest(new { message = $"ProcessTypePhase with ID {typePhaseId} not found" });

        existing.ProcessTypePhaseId = typePhaseId;
    }
    
    if (!string.IsNullOrWhiteSpace(request.ProcessStatusId))
    {
        if (!int.TryParse(request.ProcessStatusId, out var statusId))
            return Results.BadRequest(new { message = $"Invalid ProcessStatusId format" });
            
        if (!await db.ProcessStatuses.AnyAsync(x => x.Id == statusId))
            return Results.BadRequest(new { message = $"ProcessStatus with ID {statusId} not found" });

        existing.ProcessStatusId = statusId;
    }
    
    if (!string.IsNullOrWhiteSpace(request.NextHearingDate))
    {
        if (!DateTime.TryParse(request.NextHearingDate, out var nextHearing))
            return Results.BadRequest(new { message = $"Invalid NextHearingDate format" });
        existing.NextHearingDate = DateTime.SpecifyKind(nextHearing, DateTimeKind.Utc);
    }
    
    if (!string.IsNullOrWhiteSpace(request.Description))
        existing.Description = request.Description;
    
    if (!string.IsNullOrWhiteSpace(request.ClosedAt))
    {
        if (!DateTime.TryParse(request.ClosedAt, out var closedAt))
            return Results.BadRequest(new { message = $"Invalid ClosedAt format" });
        existing.ClosedAt = DateTime.SpecifyKind(closedAt, DateTimeKind.Utc);
    }

    // 4. Handle New Files
    if (request.Files != null && request.Files.Count > 0)
    {
        foreach (var file in request.Files)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            
            var doc = new Document
            {
                Id = Guid.NewGuid(),
                ProcessId = existing.Id,
                File = ms.ToArray(),
                FileName = Path.GetFileName(file.FileName),
                FileMimeType = file.ContentType ?? "application/octet-stream",
                FileSize = ms.Length,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
            };
            
            db.Documents.Add(doc);
        }
    }

    // 5. SAVE CHANGES
    await db.SaveChangesAsync();

    // 6. Return updated response
    var updated = await repo.GetById(id);

    // Handle possible null Documents collection safely
    var documentsResponse = updated!.Documents?.Select(d => new DocumentResponse(
        DocumentId: d.Id,
        FileName: d.FileName,
        FileMimeType: d.FileMimeType,
        FileSize: d.FileSize,
        CreatedAt: d.CreatedAt,
        DownloadUrl: $"/api/documents/{d.Id}/download"
    )).ToList() ?? new List<DocumentResponse>();

    var response = new ProcessWithDocumentsResponse(
        ProcessId: updated.Id,
        Name: updated.Name,
        Number: updated.Number,
        ClientId: updated.ClientId,
        LawyerId: updated.LawyerId,
        AdversePartName: updated.AdversePartName,
        OpposingCounselName: updated.OpposingCounselName,
        CreatedAt: updated.CreatedAt,
        ClosedAt: updated.ClosedAt,
        Priority: updated.Priority,
        CourtInfo: updated.CourtInfo,
        ProcessTypePhaseId: updated.ProcessTypePhaseId,
        ProcessStatusId: updated.ProcessStatusId,
        Description: updated.Description,
        NextHearingDate: updated.NextHearingDate,
        Documents: documentsResponse
    );

    return Results.Ok(response);
}

    private static async Task<IResult> DeleteProcess(Guid id, IProcessRepository repo)
    {
        try
        {
            await repo.Delete(id);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new { message = $"Process with ID {id} not found" });
        }
    }
}
