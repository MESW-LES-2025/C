using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Consilium.Tests.Consilium.Infrastructure.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task GetById_ShouldReturnUser_WhenExists()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            ID = userId,
            Email = "test@example.com",
            Name = "Test User",
            PasswordHash = "hash123",
            Status = "ACTIVE"
        };
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _repository.GetById(userId);

        result.Should().NotBeNull();
        result!.ID.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetById_ShouldReturnNull_WhenNotExists()
    {
        var result = await _repository.GetById(Guid.NewGuid());
        
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllUsers()
    {
        var users = new[]
        {
            new User { ID = Guid.NewGuid(), Email = "user1@test.com", Name = "User 1", PasswordHash = "hash1", Status = "ACTIVE" },
            new User { ID = Guid.NewGuid(), Email = "user2@test.com", Name = "User 2", PasswordHash = "hash2", Status = "INACTIVE" }
        };
        
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAll();

        result.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
