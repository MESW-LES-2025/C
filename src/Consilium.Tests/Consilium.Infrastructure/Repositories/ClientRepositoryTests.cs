using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Consilium.Tests.Consilium.Infrastructure.Repositories;

public class ClientRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ClientRepository _repository;

    public ClientRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AppDbContext(options);
        _repository = new ClientRepository(_context);
    }

    [Fact]
    public async Task Create_ShouldAddClientAndUser()
    {
        var user = new User
        {
            Email = "client@test.com",
            Name = "Test Client",
            PasswordHash = "hash123",
            Status = "ACTIVE"
        };
        
        var client = new Client
        {
            NIF = 123456789,
            Address = "Test Address"
        };

        var result = await _repository.Create(user, client);

        result.Should().NotBeNull();
        result.NIF.Should().Be(123456789);
        
        var savedClient = await _context.Clients.Include(c => c.User).FirstOrDefaultAsync();
        savedClient.Should().NotBeNull();
        savedClient!.User.Email.Should().Be("client@test.com");
    }

    [Fact]
    public async Task GetById_ShouldReturnClient_WhenExists()
    {
        var user = new User
        {
            Email = "client@test.com",
            Name = "Test Client",
            PasswordHash = "hash123",
            Status = "ACTIVE"
        };
        
        var client = new Client
        {
            NIF = 987654321,
            Address = "Test Address"
        };
        
        await _repository.Create(user, client);

        var result = await _repository.GetById(client.ID);

        result.Should().NotBeNull();
        result!.NIF.Should().Be(987654321);
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllClients()
    {
        var clients = new[]
        {
            (new User { Email = "c1@test.com", Name = "Client 1", PasswordHash = "h1", Status = "ACTIVE" },
             new Client { NIF = 111111111, Address = "Address 1" }),
            (new User { Email = "c2@test.com", Name = "Client 2", PasswordHash = "h2", Status = "ACTIVE" },
             new Client { NIF = 222222222, Address = "Address 2" })
        };

        foreach (var (user, client) in clients)
        {
            await _repository.Create(user, client);
        }

        var result = await _repository.GetAll();

        result.Should().HaveCount(2);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
