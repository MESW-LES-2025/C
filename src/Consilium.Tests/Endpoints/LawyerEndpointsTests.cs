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

public class LawyerEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LawyerEndpointsTests(WebApplicationFactory<Program> factory)
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

    private async Task<Guid> SeedLawyer(AppDbContext db)
    {
        await AuthTestHelper.SeedAuthUser(db);
        var user = new User { ID = Guid.NewGuid(), Name = "Lawyer One", Email = "lawyer1@test", NIF = "333333333", PasswordHash = "x", IsActive = true };
        var lawyer = new Lawyer { ID = user.ID, ProfessionalRegister = "LR-1" };
        var phone = new Phone { ID = Guid.NewGuid(), UserID = user.ID, Number = "2222", CountryCode = 351, IsMain = true };
        user.Phones.Add(phone);

        await db.Users.AddAsync(user);
        await db.Lawyers.AddAsync(lawyer);
        await db.Phones.AddAsync(phone);
        await db.SaveChangesAsync();
        return user.ID;
    }

    [Fact]
    public async Task CreateLawyer_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);

        var req = new CreateLawyerRequest(Email: $"newlawyer{Guid.NewGuid():N}@test", Password: "pass", Name: "New Lawyer", NIF: "444444444", ProfessionalRegister: "REG-1", PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null);

        var resp = await client.PostAsJsonAsync("/api/lawyers/", req);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        var createdJson = System.Text.Json.JsonDocument.Parse(text).RootElement;
        var createdName = createdJson.GetProperty("name").GetString();
        var createdReg = createdJson.GetProperty("professionalRegister").GetString();
        Assert.Equal(req.Name, createdName);
        Assert.Equal(req.ProfessionalRegister, createdReg);
    }
    
    [Fact]
    public async Task GetAllLawyers_ReturnsList()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedLawyer(db);
        }

        var resp = await client.GetAsync("/api/lawyers/");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        
        var json = System.Text.Json.JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        var data = json.GetProperty("data");
        Assert.True(data.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetLawyerById_ReturnsLawyer()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedLawyer(db);
        }

        var resp = await client.GetAsync($"/api/lawyers/{id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var text = await resp.Content.ReadAsStringAsync();
        var json = System.Text.Json.JsonDocument.Parse(text).RootElement;
        Assert.Equal(id, json.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task PatchLawyer_UpdatesProvidedFields()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedLawyer(db);
        }

        var updateReq = new UpdateLawyerRequest(Name: "Updated Lawyer", Email: null, Password: null, ProfessionalRegister: "REG-NEW", NIF: null, PhoneNumber: null, PhoneCountryCode: null, PhoneIsMain: null, IsActive: null);
        var patchResp = await client.PatchAsJsonAsync($"/api/lawyers/{id}", updateReq);
        Assert.Equal(HttpStatusCode.OK, patchResp.StatusCode);

        var patchText = await patchResp.Content.ReadAsStringAsync();
        var updatedJson = System.Text.Json.JsonDocument.Parse(patchText).RootElement;
        Assert.Equal("Updated Lawyer", updatedJson.GetProperty("name").GetString());
        Assert.Equal("REG-NEW", updatedJson.GetProperty("professionalRegister").GetString());
    }

    [Fact]
    public async Task DeleteLawyer_DeletesAndReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedLawyer(db);
        }

        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            id = await SeedLawyer(db);
        }

        var delResp = await client.DeleteAsync($"/api/lawyers/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        var getResp = await client.GetAsync($"/api/lawyers/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }
}
