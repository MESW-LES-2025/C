using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Consilium.API.Services;
using Consilium.Infrastructure.Data;

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
}
