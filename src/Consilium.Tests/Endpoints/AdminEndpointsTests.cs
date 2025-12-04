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

public class AdminEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminEndpointsTests(WebApplicationFactory<Program> factory)
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

    private async Task<Guid> SeedAdmin(AppDbContext db)
    {
        await AuthTestHelper.SeedAuthUser(db);
        var user = new User { ID = Guid.NewGuid(), Name = "Admin One", Email = "admin1@test", NIF = "555555555", PasswordHash = "x", IsActive = true };
        var admin = new Admin { ID = user.ID, StartedAt = DateTime.UtcNow };
        var phone = new Phone { ID = Guid.NewGuid(), UserID = user.ID, Number = "9999", CountryCode = 351, IsMain = true };
        user.Phones.Add(phone);

        await db.Users.AddAsync(user);
        await db.Admins.AddAsync(admin);
        await db.Phones.AddAsync(phone);
        await db.SaveChangesAsync();
        return user.ID;
    }

    [Fact]
    public async Task CreateAdmin_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        var req = new CreateAdminRequest(Email: $"newadmin{Guid.NewGuid():N}@test", Password: "pass", Name: "New Admin", NIF: "666666666", PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);

        var resp = await client.PostAsJsonAsync("/api/admins/", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        var createdJson = System.Text.Json.JsonDocument.Parse(text).RootElement;
        var createdName = createdJson.GetProperty("name").GetString();
        var createdNif = createdJson.GetProperty("nif").GetString();
        Assert.Equal(req.Name, createdName);
        Assert.Equal(req.NIF, createdNif);
    }

    [Fact]
    public async Task GetAdminById_ReturnsAdmin()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedAdmin(db);
        }

        var resp = await client.GetAsync($"/api/admins/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(text).RootElement;
        Assert.Equal(id, json.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task PatchAdmin_UpdatesProvidedFields()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedAdmin(db);
        }

        var updateReq = new UpdateAdminRequest(Name: "Updated Admin", Email: null, Password: null, PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);
        var patchResp = await client.PatchAsJsonAsync($"/api/admins/{id}", updateReq);
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        var patchText = await patchResp.Content.ReadAsStringAsync();
        var updatedJson = System.Text.Json.JsonDocument.Parse(patchText).RootElement;
        Assert.Equal("Updated Admin", updatedJson.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetAllAdmins_WithSearchStatusSortPagination_ReturnsFilteredAndMeta()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Seed multiple admins
            for (int i = 1; i <= 4; i++)
            {
                var user = new User { ID = Guid.NewGuid(), Name = $"Admin{i}", Email = $"a{i}@test", NIF = (111111110 + i).ToString(), PasswordHash = "x", IsActive = i % 2 == 0 };
                var admin = new Admin { ID = user.ID, StartedAt = DateTime.UtcNow };
                await db.Users.AddAsync(user);
                await db.Admins.AddAsync(admin);
            }
            await db.SaveChangesAsync();
        }

        var resp = await client.GetAsync($"/api/admins/?search=Admin&status=ACTIVE&page=1&limit=1&sortBy=nif&sortOrder=desc");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        var meta = json.GetProperty("meta");
        Assert.True(meta.GetProperty("totalCount").GetInt32() >= 1);
        Assert.Equal(1, meta.GetProperty("limit").GetInt32());
    }

    [Fact]
    public async Task CreateAdmin_MissingEmail_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var req = new CreateAdminRequest(Email: "", Password: "pass", Name: "Name", NIF: "111222333", PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);
        var resp = await client.PostAsJsonAsync("/api/admins/", req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_MissingPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var req = new CreateAdminRequest(Email: "a@test.com", Password: "", Name: "Name", NIF: "111222333", PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);
        var resp = await client.PostAsJsonAsync("/api/admins/", req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CreateAdmin_InvalidNif_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var req = new CreateAdminRequest(Email: "a@test.com", Password: "pass", Name: "Name", NIF: "INVALID", PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);
        var resp = await client.PostAsJsonAsync("/api/admins/", req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UpdateAdmin_NoFieldsProvided_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedAdmin(db);
        }

        var updateReq = new UpdateAdminRequest(Name: null, Email: null, Password: null, PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);
        var resp = await client.PatchAsJsonAsync($"/api/admins/{id}", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task DeleteAdmin_DeletesAndReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedAdmin(db);
        }

        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedAdmin(db);
        }

        var delResp = await client.DeleteAsync($"/api/admins/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await client.GetAsync($"/api/admins/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }
}
