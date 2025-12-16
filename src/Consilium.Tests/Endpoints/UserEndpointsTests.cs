using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.API.Dtos;
using Consilium.Tests.TestHelpers;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Consilium.Tests.Endpoints;

public class UserEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "TestDb_Users_" + Guid.NewGuid();
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

    private async Task SeedUsers(AppDbContext db)
    {
        await AuthTestHelper.SeedAuthUser(db);
        
        var u1 = new User { ID = Guid.NewGuid(), Name = "U1", Email = "u1@t.com", NIF = "1", PasswordHash = "x", IsActive = true };
        var c1 = new Client { ID = u1.ID, Address = "Addr1" };
        var u2 = new User { ID = Guid.NewGuid(), Name = "U2", Email = "u2@t.com", NIF = "2", PasswordHash = "x", IsActive = false };
        var c2 = new Client { ID = u2.ID, Address = "Addr2" };
        
        await db.Users.AddRangeAsync(u1, u2);
        await db.Clients.AddRangeAsync(c1, c2);
        await db.SaveChangesAsync();
    }

    private System.Text.Json.JsonSerializerOptions _jsonOptions = new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetAllUsers_ReturnsUsers()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Clear existing users to match assertion count if needed, but SeedUsers adds 2 + AuthUser = 3
            // In-memory DB persists across requests in same test run if context is reused, but here we scope per request.
            // Wait, WebApplicationFactory shares the InMemory DB instance if configured with the same name.
            // My constructor generates a unique DB name per test class instance.
            await SeedUsers(db);
        }

        var response = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>(_jsonOptions);
        Assert.NotNull(users);
        Assert.True(users.Count >= 2);
    }

    [Fact]
    public async Task GetUserById_ReturnsUser()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedUsers(db);
            userId = await db.Users.Where(u => u.Name == "U1").Select(u => u.ID).FirstAsync();
        }

        var response = await client.GetAsync($"/api/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(_jsonOptions);
        Assert.Equal("U1", user!.Name);
    }

    [Fact]
    public async Task GetUserById_NotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task DeleteUser_DeletesUser()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedUsers(db);
            userId = await db.Users.Where(u => u.Name == "U2").Select(u => u.ID).FirstAsync();
        }

        var response = await client.DeleteAsync($"/api/users/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var u = await db.Users.FindAsync(userId);
            Assert.Null(u);
        }
    }
    
    [Fact]
    public async Task DeleteUser_NotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await client.AddTestAuth(_factory);
        var response = await client.DeleteAsync($"/api/users/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
