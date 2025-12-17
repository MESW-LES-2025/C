using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting; // Added missing namespace
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.Tests.TestHelpers;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Consilium.Tests.Endpoints;

public class DocumentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DocumentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "TestDb_Docs_" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) || d.ServiceType == typeof(AppDbContext)).ToList();
                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        });
    }

    private async Task<(Guid DocId, Guid ProcessId)> SeedData(AppDbContext db)
    {
        await AuthTestHelper.SeedAuthUser(db);
        
        var clientUser = new User { ID = Guid.NewGuid(), Name = "C", Email = "c@t.com", NIF = "1", PasswordHash = "x", IsActive = true };
        var lawyerUser = new User { ID = Guid.NewGuid(), Name = "L", Email = "l@t.com", NIF = "2", PasswordHash = "x", IsActive = true };
        
        await db.Users.AddRangeAsync(clientUser, lawyerUser);
        await db.Clients.AddAsync(new Client { ID = clientUser.ID, Address = "A" });
        await db.Lawyers.AddAsync(new Lawyer { ID = lawyerUser.ID, ProfessionalRegister = "R" });
        
        var ptype = new ProcessType { Name = "T" };
        var pphase = new ProcessPhase { Name = "P" };
        var pstatus = new ProcessStatus { Name = "S" };
        await db.ProcessTypes.AddAsync(ptype);
        await db.ProcessPhases.AddAsync(pphase);
        await db.ProcessStatuses.AddAsync(pstatus);
        await db.SaveChangesAsync();
        
        var tp = new ProcessTypePhase { ProcessPhaseId = pphase.Id, ProcessTypeId = ptype.Id, TypePhaseOrder = 1 };
        await db.ProcessTypePhases.AddAsync(tp);
        await db.SaveChangesAsync();
        
        var process = new Process 
        { 
            Id = Guid.NewGuid(), 
            Name = "P", 
            Number = "N", 
            ClientId = clientUser.ID, 
            LawyerId = lawyerUser.ID, 
            ProcessTypePhaseId = tp.Id,
            ProcessStatusId = pstatus.Id,
            CreatedAt = DateTime.UtcNow,
            CourtInfo = "Court" // Fixed CourtInfo
        };
        await db.Processes.AddAsync(process); 
        await db.SaveChangesAsync();
        
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            ProcessId = process.Id,
            FileName = "test.txt",
            FileMimeType = "text/plain",
            File = new byte[] { 0x1, 0x2, 0x3 },
            FileSize = 3,
            CreatedAt = DateTime.UtcNow
        };
        await db.Documents.AddAsync(doc);
        await db.SaveChangesAsync();
        
        return (doc.Id, process.Id);
    }

    [Fact]
    public async Task DownloadDocument_ReturnsFile()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        
        Guid docId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (docId, _) = await SeedData(db);
        }

        var response = await client.GetAsync($"/api/documents/{docId}/download");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType!.ToString());
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(3, bytes.Length);
    }

    [Fact]
    public async Task DownloadDocument_NotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var response = await client.GetAsync($"/api/documents/{Guid.NewGuid()}/download");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_DeletesSuccessfully()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        
        Guid docId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (docId, _) = await SeedData(db);
        }

        var response = await client.DeleteAsync($"/api/documents/{docId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify deletion
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var doc = await db.Documents.FindAsync(docId);
            Assert.Null(doc);
        }
    }

    [Fact]
    public async Task DeleteDocument_NotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var response = await client.DeleteAsync($"/api/documents/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
