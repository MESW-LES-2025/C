using Consilium.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace Consilium.Tests.Consilium.Infrastructure.Services;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHash()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("password123");
        
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("password123");
        
        var result = hasher.VerifyPassword("password123", hash);
        
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("password123");
        
        var result = hasher.VerifyPassword("wrongpassword", hash);
        
        result.Should().BeFalse();
    }
}
