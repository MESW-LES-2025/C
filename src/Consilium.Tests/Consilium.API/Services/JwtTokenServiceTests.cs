using Consilium.API.Services;
using Consilium.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Consilium.Tests.Consilium.API.Services;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_ShouldReturnValidJwt()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "test-secret-key-that-is-at-least-32-characters-long" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            })
            .Build();
        
        var service = new JwtTokenService(config);
        var user = new User
        {
            ID = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test User",
            Status = "ACTIVE"
        };

        var token = service.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts
    }
}
