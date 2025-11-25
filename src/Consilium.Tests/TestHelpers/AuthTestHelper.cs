using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Consilium.API.Services;
using Consilium.Infrastructure.Data;
using Consilium.Domain.Models;

namespace Consilium.Tests.TestHelpers;

public static class AuthTestHelper
{
    public static async Task<string> GetTokenForUser(this WebApplicationFactory<Program> factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jwtService = scope.ServiceProvider.GetRequiredService<JwtTokenService>();
        var user = await db.Users.FindAsync(userId);
        if (user is null) throw new InvalidOperationException($"User with ID {userId} not found in test DB");
        return await jwtService.GenerateToken(user);
    }

    public static void AddAuthHeader(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public const string TestToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyX2lkIjoiMWY5Y2VkNjktY2EyNi00YzFjLTgyZTktOTNjOTNhODg2MTVjIiwidXNlcm5hbWUiOiJwcm9mZXNzb3IiLCJyb2xlIjoiQWRtaW4iLCJlbWFpbCI6InByb2Zlc3NvckBnbWFpbC5jb20iLCJuaWYiOiIwMDAwMDAwMDAiLCJqdGkiOiI2OWFlYmNjZS00NGJkLTQ0MDUtOTMzZC05Y2JmNDhhNTc1NTciLCJleHAiOjE3OTg3NjE1OTksImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjgwODAiLCJhdWQiOiJodHRwOi8vbG9jYWxob3N0OjQyMDAifQ.pj4EzZVAt4FnXlbWOmDceXyEKLnDHHFrfkmUVUvAQRg";

    public static async Task SeedAuthUser(AppDbContext db)
    {
        var userId = Guid.Parse("1f9ced69-ca26-4c1c-82e9-93c93a88615c");
        if (await db.Users.AnyAsync(u => u.ID == userId)) return;

        var user = new User
        {
            ID = userId,
            Name = "professor",
            Email = "professor@gmail.com",
            NIF = "000000000",
            PasswordHash = "x",
            IsActive = true
        };
        await db.Users.AddAsync(user);

        var admin = new Admin { ID = userId, StartedAt = DateTime.UtcNow };
        await db.Admins.AddAsync(admin);

        await db.SaveChangesAsync();
    }

    public static async Task EnsureAuthUser(this WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedAuthUser(db);
    }

    public static async Task AddTestAuth(this HttpClient client, WebApplicationFactory<Program> factory)
    {
        await factory.EnsureAuthUser();
        var userId = Guid.Parse("1f9ced69-ca26-4c1c-82e9-93c93a88615c");
        var token = await factory.GetTokenForUser(userId);
        client.AddAuthHeader(token);
    }
}
