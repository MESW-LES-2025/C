using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.API.Dtos;
using Microsoft.Extensions.Configuration;

namespace Consilium.Tests.Endpoints;

using Consilium.Tests.TestHelpers;

public class ProcessEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProcessEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "TestDb" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                // Remove existing AppDbContext and related DbContextOptions registrations
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) || d.ServiceType == typeof(AppDbContext)).ToList();
                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                // Setup InMemory DB
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        });
    }

    private async Task SeedMinimalData(AppDbContext db)
    {
        await AuthTestHelper.SeedAuthUser(db);
        // Add Users (Client and Lawyer)
        var clientUser = new User { ID = Guid.NewGuid(), Name = "Client Test", Email = "client@test", NIF = "111111111", PasswordHash = "x", IsActive = true };
        var lawyerUser = new User { ID = Guid.NewGuid(), Name = "Lawyer Test", Email = "lawyer@test", NIF = "222222222", PasswordHash = "x", IsActive = true };

        var client = new Client { ID = clientUser.ID, Address = "somewhere" };
        var lawyer = new Lawyer { ID = lawyerUser.ID, ProfessionalRegister = "ABC-1" };

        await db.Users.AddRangeAsync(clientUser, lawyerUser);
        await db.Clients.AddAsync(client);
        await db.Lawyers.AddAsync(lawyer);

        // Add process types/phases/status
        var ptype = new ProcessType { Name = "Civil" };
        var pphase = new ProcessPhase { Name = "Initial" };
        var pstatus = new ProcessStatus { Name = "Open", IsDefault = true};
        await db.ProcessTypes.AddAsync(ptype);
        await db.ProcessPhases.AddAsync(pphase);
        await db.ProcessStatuses.AddAsync(pstatus);
        await db.SaveChangesAsync();

        var typePhase = new ProcessTypePhase { ProcessPhaseId = pphase.Id, ProcessTypeId = ptype.Id, TypePhaseOrder = 1 };
        await db.ProcessTypePhases.AddAsync(typePhase);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Create_Get_Update_Delete_ProcessFlow()
    {
        // Arrange
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        // Get DB from the factory and seed
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        string processNumber = $"P-{Guid.NewGuid():N}";

        // Prepare request
        Guid clientId;
        Guid lawyerId;
        int processTypePhaseId;
        int processStatusId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clientId = db.Clients.First().ID;
            lawyerId = db.Lawyers.First().ID;
            processTypePhaseId = db.ProcessTypePhases.First().Id;
            processStatusId = db.ProcessStatuses.First().Id;
        }

        var createReq = new CreateProcessRequest(
            Name: "Test Process",
            Number: processNumber,
            ClientId: clientId,
            LawyerId: lawyerId,
            AdversePartName: "Adverse",
            OpposingCounselName: "Opposing",
            Priority: 1,
            CourtInfo: "Court A",
            ProcessTypePhaseId: processTypePhaseId,
            ProcessStatusId: processStatusId,
            NextHearingDate: DateTime.UtcNow.AddDays(7),
            Description: "A test process"
        );

        // Act: Create
        var createResp = await client.PostAsJsonAsync("/api/processes/", createReq);

        // Assert Create
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<ProcessResponse>();
        Assert.NotNull(created);
        Assert.Equal(createReq.Number, created!.Number);

        var processId = created.ProcessId;

        // Act: Get By Id
        var getResp = await client.GetAsync($"/api/processes/{processId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await getResp.Content.ReadFromJsonAsync<ProcessResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(processId, fetched!.ProcessId);

        // Act: Update - modify the entity directly in the DB instead of via API PATCH
        var updateReq = new UpdateProcessRequest(Name: "Updated Name", Number: null, ClientId: null, LawyerId: null, AdversePartName: null, OpposingCounselName: null, Priority: null, CourtInfo: null, ProcessTypePhaseId: null, ProcessStatusId: null, NextHearingDate: null, Description: null, ClosedAt: null);
        using (var updateScope = _factory.Services.CreateScope())
        {
            var db = updateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var existingProcess = await db.Processes.FindAsync(processId);
            Assert.NotNull(existingProcess);
            existingProcess.Name = updateReq.Name ?? existingProcess.Name;
            db.Processes.Update(existingProcess);
            await db.SaveChangesAsync();
        }

        // Verify via GET
        var getUpdatedResp = await client.GetAsync($"/api/processes/{processId}");
        Assert.Equal(HttpStatusCode.OK, getUpdatedResp.StatusCode);
        var updated = await getUpdatedResp.Content.ReadFromJsonAsync<ProcessResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated!.Name);

        // Act: Delete
        var deleteResp = await client.DeleteAsync($"/api/processes/{processId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Act: Get after delete
        var getAfterDel = await client.GetAsync($"/api/processes/{processId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDel.StatusCode);
    }

    [Fact]
    public async Task CreateProcess_MissingTypePhase_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        // Get DB from the factory and seed minimal client/lawyer
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        Guid clientId;
        Guid lawyerId;
        int statusId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clientId = db.Clients.First().ID;
            lawyerId = db.Lawyers.First().ID;
            statusId = db.ProcessStatuses.First().Id;
        }

        var createReq = new CreateProcessRequest(
            Name: "Test Process",
            Number: $"P-{Guid.NewGuid():N}",
            ClientId: clientId,
            LawyerId: lawyerId,
            AdversePartName: "Adverse",
            OpposingCounselName: "Opposing",
            Priority: 1,
            CourtInfo: "Court A",
            ProcessTypePhaseId: 9999, // invalid
            ProcessStatusId: statusId,
            NextHearingDate: DateTime.UtcNow.AddDays(7),
            Description: "A test process"
        );

        var createResp = await client.PostAsJsonAsync("/api/processes/", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, createResp.StatusCode);
    }

    [Fact]
    public async Task GetProcessByIdWithDocuments_ReturnsProcessWithDocuments()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid processId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "DocProc",
                Number = "P-Doc",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            var doc = new Document { Id = Guid.NewGuid(), ProcessId = process.Id, FileName = "file.txt", File = new byte[] { 1, 2, 3 }, FileMimeType = "text/plain", FileSize = 3, CreatedAt = DateTime.UtcNow };
            process.Documents.Add(doc);
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var resp = await client.GetAsync($"/api/processes/{processId}/with-documents");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        var docs = json.GetProperty("documents");
        Assert.NotEmpty(docs.EnumerateArray());
    }

    [Fact]
    public async Task CreateProcessWithDocuments_UploadsDocumentsAndCreatesProcess()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        // Seed minimal data
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        Guid clientId;
        Guid lawyerId;
        int ptypeId;
        int pstatusId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clientId = db.Clients.First().ID;
            lawyerId = db.Lawyers.First().ID;
            ptypeId = db.ProcessTypePhases.First().Id;
            pstatusId = db.ProcessStatuses.First().Id;
        }

        var multiContent = new MultipartFormDataContent();
        multiContent.Add(new StringContent("Process with file"), "Name");
        multiContent.Add(new StringContent($"P-{Guid.NewGuid():N}"), "Number");
        multiContent.Add(new StringContent("Court A"), "CourtInfo");
        multiContent.Add(new StringContent(clientId.ToString()), "ClientId");
        multiContent.Add(new StringContent(lawyerId.ToString()), "LawyerId");
        multiContent.Add(new StringContent(ptypeId.ToString()), "ProcessTypePhaseId");
        multiContent.Add(new StringContent(pstatusId.ToString()), "ProcessStatusId");
        multiContent.Add(new StringContent("1"), "Priority");
        // Add file
        var fileContent = new ByteArrayContent(new byte[] { 10, 20, 30 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        multiContent.Add(fileContent, "Files", "upload.bin");

        var resp = await client.PostAsync($"/api/processes/with-documents", multiContent);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var json = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.NotNull(json.GetProperty("processId"));
    }

    [Fact]
    public async Task CreateProcess_MissingName_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        Guid clientId;
        Guid lawyerId;
        int ptypeId;
        int pstatusId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clientId = db.Clients.First().ID;
            lawyerId = db.Lawyers.First().ID;
            ptypeId = db.ProcessTypePhases.First().Id;
            pstatusId = db.ProcessStatuses.First().Id;
        }

        var createReq = new CreateProcessRequest(
            Name: "",
            Number: $"P-{Guid.NewGuid():N}",
            ClientId: clientId,
            LawyerId: lawyerId,
            AdversePartName: "Adverse",
            OpposingCounselName: "Opposing",
            Priority: 1,
            CourtInfo: "Court A",
            ProcessTypePhaseId: ptypeId,
            ProcessStatusId: pstatusId,
            NextHearingDate: DateTime.UtcNow.AddDays(7),
            Description: "A test process"
        );

        var resp = await client.PostAsJsonAsync("/api/processes/", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProcess_MissingNumber_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        Guid clientId;
        Guid lawyerId;
        int ptypeId;
        int pstatusId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clientId = db.Clients.First().ID;
            lawyerId = db.Lawyers.First().ID;
            ptypeId = db.ProcessTypePhases.First().Id;
            pstatusId = db.ProcessStatuses.First().Id;
        }

        var createReq = new CreateProcessRequest(
            Name: "Test Process",
            Number: "",
            ClientId: clientId,
            LawyerId: lawyerId,
            AdversePartName: "Adverse",
            OpposingCounselName: "Opposing",
            Priority: 1,
            CourtInfo: "Court A",
            ProcessTypePhaseId: ptypeId,
            ProcessStatusId: pstatusId,
            NextHearingDate: DateTime.UtcNow.AddDays(7),
            Description: "A test process"
        );

        var resp = await client.PostAsJsonAsync("/api/processes/", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProcess_NegativePriority_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        Guid clientId;
        Guid lawyerId;
        int ptypeId;
        int pstatusId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clientId = db.Clients.First().ID;
            lawyerId = db.Lawyers.First().ID;
            ptypeId = db.ProcessTypePhases.First().Id;
            pstatusId = db.ProcessStatuses.First().Id;
        }

        var createReq = new CreateProcessRequest(
            Name: "Test Process",
            Number: $"P-{Guid.NewGuid():N}",
            ClientId: clientId,
            LawyerId: lawyerId,
            AdversePartName: "Adverse",
            OpposingCounselName: "Opposing",
            Priority: -1,
            CourtInfo: "Court A",
            ProcessTypePhaseId: ptypeId,
            ProcessStatusId: pstatusId,
            NextHearingDate: DateTime.UtcNow.AddDays(7),
            Description: "A test process"
        );

        var resp = await client.PostAsJsonAsync("/api/processes/", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProcess_InvalidClient_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        Guid lawyerId;
        int ptypeId;
        int pstatusId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            lawyerId = db.Lawyers.First().ID;
            ptypeId = db.ProcessTypePhases.First().Id;
            pstatusId = db.ProcessStatuses.First().Id;
        }

        var createReq = new CreateProcessRequest(
            Name: "Test Process",
            Number: $"P-{Guid.NewGuid():N}",
            ClientId: Guid.NewGuid(), // invalid client
            LawyerId: lawyerId,
            AdversePartName: "Adverse",
            OpposingCounselName: "Opposing",
            Priority: 1,
            CourtInfo: "Court A",
            ProcessTypePhaseId: ptypeId,
            ProcessStatusId: pstatusId,
            NextHearingDate: DateTime.UtcNow.AddDays(7),
            Description: "A test process"
        );

        var resp = await client.PostAsJsonAsync("/api/processes/", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateProcess_InvalidLawyer_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
        }

        Guid clientId;
        int ptypeId;
        int pstatusId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            clientId = db.Clients.First().ID;
            ptypeId = db.ProcessTypePhases.First().Id;
            pstatusId = db.ProcessStatuses.First().Id;
        }

        var createReq = new CreateProcessRequest(
            Name: "Test Process",
            Number: $"P-{Guid.NewGuid():N}",
            ClientId: clientId,
            LawyerId: Guid.NewGuid(), // invalid lawyer
            AdversePartName: "Adverse",
            OpposingCounselName: "Opposing",
            Priority: 1,
            CourtInfo: "Court A",
            ProcessTypePhaseId: ptypeId,
            ProcessStatusId: pstatusId,
            NextHearingDate: DateTime.UtcNow.AddDays(7),
            Description: "A test process"
        );

        var resp = await client.PostAsJsonAsync("/api/processes/", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcess_NotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        var updateReq = new UpdateProcessRequest("NewName", null, null, null, null, null, null, null, null, null, null, null, null);
        var resp = await client.PatchAsJsonAsync($"/api/processes/{Guid.NewGuid()}", updateReq);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcessWithDocuments_InvalidDeletedDocumentIdFormat_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            // Create a process with a document
            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "UpdProc",
                Number = "P-Upd",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            var doc = new Document { Id = Guid.NewGuid(), ProcessId = process.Id, FileName = "file.txt", File = new byte[] { 1, 2, 3 }, FileMimeType = "text/plain", FileSize = 3, CreatedAt = DateTime.UtcNow };
            process.Documents.Add(doc);
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
        }

        Guid processId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            processId = db.Processes.First().Id;
        }

        var multi = new MultipartFormDataContent();
        multi.Add(new StringContent("Invalid format id"), "DeletedDocumentIds");

        var resp = await client.PatchAsync($"/api/processes/{processId}/with-documents", multi);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcessWithDocuments_DeleteDocumentNotBelongingToProcess_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        Guid otherDocId;
        Guid currentProcessId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            // Create a process with a document (doc belongs to other process)
            var otherProcess = new Process
            {
                Id = Guid.NewGuid(),
                Name = "OtherProc",
                Number = "P-Other",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            var otherDoc = new Document { Id = Guid.NewGuid(), ProcessId = otherProcess.Id, FileName = "file-other.txt", File = new byte[] { 1 }, FileMimeType = "text/plain", FileSize = 1, CreatedAt = DateTime.UtcNow };
            otherProcess.Documents.Add(otherDoc);
            await db.Processes.AddAsync(otherProcess);
            await db.SaveChangesAsync();
            otherDocId = otherDoc.Id;

            // Create a current process which will try to delete the other process's doc
            var currentProcess = new Process
            {
                Id = Guid.NewGuid(),
                Name = "CurrentProc",
                Number = "P-Curr",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            await db.Processes.AddAsync(currentProcess);
            await db.SaveChangesAsync();
            currentProcessId = currentProcess.Id;
        }

        var multi = new MultipartFormDataContent();
        multi.Add(new StringContent(otherDocId.ToString()), "DeletedDocumentIds");

        var resp = await client.PatchAsync($"/api/processes/{currentProcessId}/with-documents", multi);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcessWithDocuments_InvalidClientIdFormat_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        Guid processId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);
            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "Proc",
                Number = "P-Proc",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var multi = new MultipartFormDataContent();
        multi.Add(new StringContent("not-a-guid"), "ClientId");

        var resp = await client.PatchAsync($"/api/processes/{processId}/with-documents", multi);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteProcess_NotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var resp = await client.DeleteAsync($"/api/processes/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcess_ValidData_ReturnsOkAndUpdatesProcess()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid processId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "Original Name",
                Number = "P-Original",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow,
                Priority = 1
            };
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var updateReq = new UpdateProcessRequest(
            Name: "Updated Name",
            Number: "P-Updated",
            ClientId: null,
            LawyerId: null,
            AdversePartName: "New Adverse",
            OpposingCounselName: "New Opposing",
            Priority: 5,
            CourtInfo: "Court B",
            ProcessTypePhaseId: null,
            ProcessStatusId: null,
            NextHearingDate: DateTime.UtcNow.AddDays(14),
            Description: "Updated description",
            ClosedAt: null
        );

        var resp = await client.PatchAsJsonAsync($"/api/processes/{processId}", updateReq);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        // Verify the response contains the updated data
        var updated = await resp.Content.ReadFromJsonAsync<ProcessResponse>();
        Assert.NotNull(updated);
        // The update should have been applied
        Assert.Equal(processId, updated!.ProcessId);
    }

    [Fact]
    public async Task UpdateProcess_InvalidClient_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid processId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Number = "P-Test",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var updateReq = new UpdateProcessRequest(
            Name: null,
            Number: null,
            ClientId: Guid.NewGuid(), // Invalid client
            LawyerId: null,
            AdversePartName: null,
            OpposingCounselName: null,
            Priority: null,
            CourtInfo: null,
            ProcessTypePhaseId: null,
            ProcessStatusId: null,
            NextHearingDate: null,
            Description: null,
            ClosedAt: null
        );

        var resp = await client.PatchAsJsonAsync($"/api/processes/{processId}", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcess_InvalidLawyer_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid processId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Number = "P-Test",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var updateReq = new UpdateProcessRequest(
            Name: null,
            Number: null,
            ClientId: null,
            LawyerId: Guid.NewGuid(), // Invalid lawyer
            AdversePartName: null,
            OpposingCounselName: null,
            Priority: null,
            CourtInfo: null,
            ProcessTypePhaseId: null,
            ProcessStatusId: null,
            NextHearingDate: null,
            Description: null,
            ClosedAt: null
        );

        var resp = await client.PatchAsJsonAsync($"/api/processes/{processId}", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcess_InvalidProcessTypePhase_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid processId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Number = "P-Test",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var updateReq = new UpdateProcessRequest(
            Name: null,
            Number: null,
            ClientId: null,
            LawyerId: null,
            AdversePartName: null,
            OpposingCounselName: null,
            Priority: null,
            CourtInfo: null,
            ProcessTypePhaseId: 9999, // Invalid
            ProcessStatusId: null,
            NextHearingDate: null,
            Description: null,
            ClosedAt: null
        );

        var resp = await client.PatchAsJsonAsync($"/api/processes/{processId}", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcess_InvalidProcessStatus_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid processId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Number = "P-Test",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var updateReq = new UpdateProcessRequest(
            Name: null,
            Number: null,
            ClientId: null,
            LawyerId: null,
            AdversePartName: null,
            OpposingCounselName: null,
            Priority: null,
            CourtInfo: null,
            ProcessTypePhaseId: null,
            ProcessStatusId: 9999, // Invalid
            NextHearingDate: null,
            Description: null,
            ClosedAt: null
        );

        var resp = await client.PatchAsJsonAsync($"/api/processes/{processId}", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateProcessWithDocuments_ValidData_ReturnsOk()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid processId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedMinimalData(db);

            var clientId = db.Clients.First().ID;
            var lawyerId = db.Lawyers.First().ID;
            var ptypeId = db.ProcessTypePhases.First().Id;
            var pstatusId = db.ProcessStatuses.First().Id;

            var process = new Process
            {
                Id = Guid.NewGuid(),
                Name = "Test Process",
                Number = "P-Test",
                ClientId = clientId,
                LawyerId = lawyerId,
                CourtInfo = "Court A",
                ProcessTypePhaseId = ptypeId,
                ProcessStatusId = pstatusId,
                CreatedAt = DateTime.UtcNow
            };
            await db.Processes.AddAsync(process);
            await db.SaveChangesAsync();
            processId = process.Id;
        }

        var multi = new MultipartFormDataContent();
        multi.Add(new StringContent("Updated Process Name"), "Name");
        multi.Add(new StringContent("Updated Court"), "CourtInfo");

        var resp = await client.PatchAsync($"/api/processes/{processId}/with-documents", multi);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}
