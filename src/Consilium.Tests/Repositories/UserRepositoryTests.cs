using Microsoft.EntityFrameworkCore;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.Infrastructure.Repositories;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Consilium.Tests.Repositories;

public class UserRepositoryTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAll_ReturnsUsers()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new UserRepository(context);
        
        var u1 = new User { ID = Guid.NewGuid(), Name = "U1", Email = "1@t.com", NIF = "1", PasswordHash = "x", IsActive = true };
        var u2 = new User { ID = Guid.NewGuid(), Name = "U2", Email = "2@t.com", NIF = "2", PasswordHash = "x", IsActive = true };
        await context.Users.AddRangeAsync(u1, u2);
        await context.SaveChangesAsync();
        
        var users = await repo.GetAll();
        Assert.Equal(2, users.Count);
    }
    
    [Fact]
    public async Task GetById_ReturnsUser()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new UserRepository(context);
        
        var u1 = new User { ID = Guid.NewGuid(), Name = "U1", Email = "1@t.com", NIF = "1", PasswordHash = "x", IsActive = true };
        await context.Users.AddAsync(u1);
        await context.SaveChangesAsync();
        
        var result = await repo.GetById(u1.ID);
        Assert.NotNull(result);
        Assert.Equal("U1", result.Name);
    }

    [Fact]
    public async Task Update_UpdatesFields()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new UserRepository(context);
        
        var u1 = new User { ID = Guid.NewGuid(), Name = "Old", Email = "old@t.com", NIF = "1", PasswordHash = "oldpass", IsActive = true };
        await context.Users.AddAsync(u1);
        await context.SaveChangesAsync();
        
        var updates = new User { Name = "New", Email = null, PasswordHash = "newpass" };
        var result = await repo.Update(u1.ID, updates);
        
        Assert.NotNull(result);
        Assert.Equal("New", result.Name);
        Assert.Equal("old@t.com", result.Email); // Should not change
        Assert.Equal("newpass", result.PasswordHash);
        
        var inDb = await context.Users.FindAsync(u1.ID);
        Assert.Equal("New", inDb.Name);
    }
    
    [Fact]
    public async Task Update_NotFound_ReturnsNull()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new UserRepository(context);
        
        var result = await repo.Update(Guid.NewGuid(), new User());
        Assert.Null(result);
    }
}
