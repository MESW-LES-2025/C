using Consilium.API.Dtos;
using Consilium.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Consilium.API.Endpoints;

public static class LookupEndpoints
{
    public static void MapLookupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/lookups")
            .WithName("Lookups")
            .WithOpenApi();
        group.RequireAuthorization("Any");

        group.MapGet("/process-types", GetProcessTypes)
            .WithName("GetProcessTypes")
            .WithDescription("Get all process types");

        group.MapGet("/process-phases", GetProcessPhases)
            .WithName("GetProcessPhases")
            .WithDescription("Get all process phases");

        group.MapGet("/process-statuses", GetProcessStatuses)
            .WithName("GetProcessStatuses")
            .WithDescription("Get all process statuses");

        group.MapGet("/process-type-phases", GetProcessTypePhases)
            .WithName("GetProcessTypePhases")
            .WithDescription("Get all mappings between process types and phases");

        group.MapGet("/process-types/{typeId:int}/phases", GetPhasesForProcessType)
            .WithName("GetPhasesForProcessType")
            .WithDescription("Get phases for a specific process type (ordered)");
    }

    private static async Task<IResult> GetProcessTypes(AppDbContext db)
    {
        try
        {
        var types = await db.ProcessTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new ProcessTypeResponse(t.Id, t.Name, t.IsActive))
            .ToListAsync();
            return Results.Ok(types);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            // Missing relation or column: DB not seeded — return empty list
            return Results.Ok(Array.Empty<object>());
        }
    }

    private static async Task<IResult> GetProcessPhases(AppDbContext db)
    {
        var phases = await db.ProcessPhases
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProcessPhaseResponse(p.Id, p.Name, p.Description, p.IsActive))
            .ToListAsync();

        return Results.Ok(phases);
    }

    private static async Task<IResult> GetProcessStatuses(AppDbContext db)
    {
        var statuses = await db.ProcessStatuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new ProcessStatusResponse(s.Id, s.Name, s.IsFinal, s.IsDefault, s.IsActive))
            .ToListAsync();

        return Results.Ok(statuses);
    }

    private static async Task<IResult> GetProcessTypePhases(AppDbContext db)
    {
        try
        {
        var list = await db.ProcessTypePhases
            .Include(ptp => ptp.ProcessType)
            .Include(ptp => ptp.ProcessPhase)
            .Where(ptp => ptp.IsActive)
            .OrderBy(ptp => ptp.ProcessTypeId)
            .ThenBy(ptp => ptp.TypePhaseOrder)
            .Select(ptp => new ProcessTypePhaseResponse(
                ptp.Id,
                ptp.ProcessTypeId,
                ptp.ProcessType.Name,
                ptp.ProcessPhaseId,
                ptp.ProcessPhase.Name,
                ptp.TypePhaseOrder,
                ptp.IsOptional,
                ptp.IsActive
            ))
            .ToListAsync();
            return Results.Ok(list);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01" || ex.SqlState == "42703")
        {
            // Missing relation/column -> return empty list instead of 500
            return Results.Ok(Array.Empty<object>());
        }
    }

    private static async Task<IResult> GetPhasesForProcessType(int typeId, AppDbContext db)
    {
        var list = await db.ProcessTypePhases
            .Include(ptp => ptp.ProcessPhase)
            .Where(ptp => ptp.ProcessTypeId == typeId && ptp.IsActive)
            .OrderBy(ptp => ptp.TypePhaseOrder)
            .Select(ptp => new ProcessPhaseResponse(ptp.ProcessPhase.Id, ptp.ProcessPhase.Name, ptp.ProcessPhase.Description, ptp.ProcessPhase.IsActive))
            .ToListAsync();

        return Results.Ok(list);
    }
}
