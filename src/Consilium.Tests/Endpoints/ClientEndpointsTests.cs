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

public class ClientEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ClientEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "TestDb" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) || d.ServiceType == typeof(AppDbContext)).ToList();
                foreach (var d in descriptors) services.Remove(d);

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
            });
        });
    }

    private async Task<Guid> SeedClient(AppDbContext db)
    {
        await AuthTestHelper.SeedAuthUser(db);
        var user = new User { ID = Guid.NewGuid(), Name = "Client One", Email = "client1@test", NIF = "111111111", PasswordHash = "x", IsActive = true };
        var client = new Client { ID = user.ID, Address = "123 St" };
        var phone = new Phone { ID = Guid.NewGuid(), UserID = user.ID, Number = "1234", CountryCode = 351, IsMain = true };
        user.Phones.Add(phone);

        await db.Users.AddAsync(user);
        await db.Clients.AddAsync(client);
        await db.Phones.AddAsync(phone);
        await db.SaveChangesAsync();
        return user.ID;
    }

    [Fact]
    public async Task CreateClient_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        var req = new CreateClientRequest(Email: $"newclient{Guid.NewGuid():N}@test", Password: "pass", Name: "New Client", NIF: "222222222", Address: "Addr 1", PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);

        var resp = await client.PostAsJsonAsync("/api/clients/", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        var createdJson = System.Text.Json.JsonDocument.Parse(text).RootElement;
        var createdId = createdJson.GetProperty("id").GetGuid();
        var createdName = createdJson.GetProperty("name").GetString();
        var createdNif = createdJson.GetProperty("nif").GetString();
        Assert.Equal(req.Name, createdName);
        Assert.Equal(req.NIF, createdNif);
    }
    
    [Fact]
    public async Task GetAllClients_ReturnsList()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedClient(db);
        }

        var resp = await client.GetAsync("/api/clients/");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        
        var json = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        var data = json.GetProperty("data");
        Assert.True(data.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetClientById_ReturnsClient()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedClient(db);
        }

        var resp = await client.GetAsync($"/api/clients/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(text).RootElement;
        Assert.Equal(id, json.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task PatchClient_UpdatesProvidedFields()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedClient(db);
        }

        var updateReq = new UpdateClientRequest(Name: "Updated Name", Email: null, Password: null, Address: "New Addr", NIF: null, IsActive: null, PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);
        var patchResp = await client.PatchAsJsonAsync($"/api/clients/{id}", updateReq);
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        var patchText = await patchResp.Content.ReadAsStringAsync();
        var updatedJson = System.Text.Json.JsonDocument.Parse(patchText).RootElement;
        Assert.Equal("Updated Name", updatedJson.GetProperty("name").GetString());
        Assert.Equal("New Addr", updatedJson.GetProperty("address").GetString());
    }

    [Fact]
    public async Task DeleteClient_DeletesAndReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid id;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedClient(db);
        }

        var delResp = await client.DeleteAsync($"/api/clients/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await client.GetAsync($"/api/clients/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }
}
