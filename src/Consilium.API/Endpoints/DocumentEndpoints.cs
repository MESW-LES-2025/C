using Consilium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Consilium.API.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents")
            .WithName("Documents")
            .WithOpenApi();
        group.RequireAuthorization("Any");

        group.MapGet("/{id:guid}/download", DownloadDocument)
            .WithName("DownloadDocument")
            .WithDescription("Download the raw file content");

        group.MapDelete("/{id:guid}", DeleteDocument)
            .WithName("DeleteDocument")
            .WithDescription("Delete a document by ID")
            .RequireAuthorization("AdminOrLawyer");
    }

    private static async Task<IResult> DownloadDocument(Guid id, AppDbContext db)
    {
        // Fetch the document from the DB using AsNoTracking for performance
        var doc = await db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null)
        {
            return Results.NotFound(new { message = "Document not found" });
        }

        // Return the file stream directly with proper content type
        return Results.File(doc.File, doc.FileMimeType, doc.FileName);
    }

    private static async Task<IResult> DeleteDocument(Guid id, AppDbContext db)
    {
        var doc = await db.Documents.FindAsync(id);

        if (doc == null)
        {
            return Results.NotFound(new { message = "Document not found" });
        }

        db.Documents.Remove(doc);
        await db.SaveChangesAsync();

        return Results.NoContent();
    }
}
