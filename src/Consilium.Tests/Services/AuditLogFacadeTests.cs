using Microsoft.EntityFrameworkCore;
using Consilium.Infrastructure.Data;
using System.Text.Json;
using Consilium.Infrastructure.Services;
using Consilium.Domain.Models;

namespace Consilium.Tests.Services;

public class AuditLogFacadeTests
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
    public async Task AddCreateClientLogAsync_CreatesActionTypeAndUserLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        // Seed client and user
        var user = new User { ID = Guid.NewGuid(), Name = "Client", Email = "c@test", NIF = "111111111", PasswordHash = "x", IsActive = true };
        var client = new Client { ID = user.ID, Address = "Addr" };
        await context.Users.AddAsync(user);
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        // Act
        await audit.AddCreateClientLogAsync(client.ID, client.ID);

        // Assert
        var types = await context.ActionLogTypes.ToListAsync();
        Assert.NotEmpty(types);
        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Equal(1, logs.Count);
    }

    [Fact]
    public async Task AddDeleteClientLogAsync_CreatesActionTypeAndUserLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Client", Email = "c@test", NIF = "222222222", PasswordHash = "x", IsActive = true };
        var client = new Client { ID = user.ID, Address = "Addr" };
        var phone = new Phone { ID = Guid.NewGuid(), Number = "12345", CountryCode = 351, IsMain = true, UserID = user.ID };
        await context.Users.AddAsync(user);
        await context.Clients.AddAsync(client);
        await context.Phones.AddAsync(phone);
        await context.SaveChangesAsync();

        await audit.AddDeleteClientLogAsync(client.ID, user.ID);

        var types = await context.ActionLogTypes.ToListAsync();
        Assert.NotEmpty(types);
        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Equal(1, logs.Count);
    }

    [Fact]
    public async Task AddCreateLawyerLogAsync_CreatesActionTypeAndLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Lawyer", Email = "l@test", NIF = "333333333", PasswordHash = "x", IsActive = true };
        var lawyer = new Lawyer { ID = user.ID, ProfessionalRegister = "PR123"};
        var phone = new Phone { ID = Guid.NewGuid(), Number = "54321", CountryCode = 44, IsMain = true, UserID = user.ID };
        await context.Users.AddAsync(user);
        await context.Lawyers.AddAsync(lawyer);
        await context.Phones.AddAsync(phone);
        await context.SaveChangesAsync();

        await audit.AddCreateLawyerLogAsync(lawyer.ID, user.ID);

        var types = await context.ActionLogTypes.ToListAsync();
        Assert.NotEmpty(types);
        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Equal(1, logs.Count);
    }

    [Fact]
    public async Task AddDeleteLawyerLogAsync_CreatesActionTypeAndLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Lawyer", Email = "l@test", NIF = "444444444", PasswordHash = "x", IsActive = true };
        var lawyer = new Lawyer { ID = user.ID, ProfessionalRegister = "PR124"};
        var phone = new Phone { ID = Guid.NewGuid(), Number = "54321", CountryCode = 44, IsMain = true, UserID = user.ID };
        await context.Users.AddAsync(user);
        await context.Lawyers.AddAsync(lawyer);
        await context.Phones.AddAsync(phone);
        await context.SaveChangesAsync();

        await audit.AddDeleteLawyerLogAsync(lawyer.ID, user.ID);

        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Equal(1, logs.Count);
    }

    [Fact]
    public async Task AddCreateAdminLogAsync_CreatesActionTypeAndLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Admin", Email = "admin@test", NIF = "777777777", PasswordHash = "x", IsActive = true };
        var admin = new Admin { ID = user.ID, StartedAt = DateTime.UtcNow };
        var phone = new Phone { ID = Guid.NewGuid(), Number = "1010", CountryCode = 351, IsMain = true, UserID = user.ID };
        await context.Users.AddAsync(user);
        await context.Admins.AddAsync(admin);
        await context.Phones.AddAsync(phone);
        await context.SaveChangesAsync();

        await audit.AddCreateAdminLogAsync(admin.ID, user.ID);

        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AddDeleteAdminLogAsync_CreatesActionTypeAndLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Admin", Email = "admin@test", NIF = "888888888", PasswordHash = "x", IsActive = true };
        var admin = new Admin { ID = user.ID, StartedAt = DateTime.UtcNow };
        var phone = new Phone { ID = Guid.NewGuid(), Number = "2020", CountryCode = 351, IsMain = true, UserID = user.ID };
        await context.Users.AddAsync(user);
        await context.Admins.AddAsync(admin);
        await context.Phones.AddAsync(phone);
        await context.SaveChangesAsync();

        await audit.AddDeleteAdminLogAsync(admin.ID, user.ID);

        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AddUpdateAdminLogAsync_CreatesActionTypeAndLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Admin", Email = "admin@test", NIF = "999999998", PasswordHash = "x", IsActive = true };
        var admin = new Admin { ID = user.ID, StartedAt = DateTime.UtcNow };
        await context.Users.AddAsync(user);
        await context.Admins.AddAsync(admin);
        await context.SaveChangesAsync();

        var oldSnapshot = JsonSerializer.SerializeToElement(new { A = 1 });
        var newSnapshot = JsonSerializer.SerializeToElement(new { A = 2 });

        await audit.AddUpdateAdminLogAsync(admin.ID, user.ID, oldSnapshot, newSnapshot);

        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AddUpdateClientLogAsync_CreatesActionTypeAndLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Client2", Email = "client2@test", NIF = "123123123", PasswordHash = "x", IsActive = true };
        var client = new Client { ID = user.ID, Address = "Addr2" };
        await context.Users.AddAsync(user);
        await context.Clients.AddAsync(client);
        await context.SaveChangesAsync();

        var oldSnapshot = JsonSerializer.SerializeToElement(new { Foo = 1 });
        var newSnapshot = JsonSerializer.SerializeToElement(new { Foo = 2 });
        await audit.AddUpdateClientLogAsync(client.ID, user.ID, oldSnapshot, newSnapshot);

        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AddUpdateLawyerLogAsync_CreatesActionTypeAndLog()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "Lawyer2", Email = "law2@test", NIF = "222333444", PasswordHash = "x", IsActive = true };
        var lawyer = new Lawyer { ID = user.ID, ProfessionalRegister = "PR99" };
        await context.Users.AddAsync(user);
        await context.Lawyers.AddAsync(lawyer);
        await context.SaveChangesAsync();

        var oldSnapshot = JsonSerializer.SerializeToElement(new { L = 1 });
        var newSnapshot = JsonSerializer.SerializeToElement(new { L = 2 });
        await audit.AddUpdateLawyerLogAsync(lawyer.ID, user.ID, oldSnapshot, newSnapshot);

        var logs = await context.UserLogs.ToListAsync();
        Assert.NotEmpty(logs);
        Assert.Single(logs);
    }

    [Fact]
    public async Task AnonymizeUserLogsAsync_RemovesSensitiveData()
    {
        await using var context = GetInMemoryDbContext();
        var audit = new AuditLogFacade(context);

        var user = new User { ID = Guid.NewGuid(), Name = "ClientA", Email = "a@test", NIF = "555555555", PasswordHash = "x", IsActive = true };
        await context.Users.AddAsync(user);

        // create user log where user is affected
    var actionType = new ActionLogType { ID = 1, Name = "TEST" };
        await context.ActionLogTypes.AddAsync(actionType);
        await context.SaveChangesAsync();

        var logAffected = new UserLog { ID = Guid.NewGuid(), AffectedUserID = user.ID, ActionLogTypeID = actionType.ID, OldValue = System.Text.Json.JsonSerializer.SerializeToElement(new { foo = 1 }), NewValue = System.Text.Json.JsonSerializer.SerializeToElement(new { baz = 2 }), UpdatedAt = DateTime.UtcNow };
        var logUpdatedBy = new UserLog { ID = Guid.NewGuid(), AffectedUserID = Guid.NewGuid(), UpdatedByID = user.ID, ActionLogTypeID = actionType.ID, OldValue = System.Text.Json.JsonSerializer.SerializeToElement(new { foo = 5 }), NewValue = System.Text.Json.JsonSerializer.SerializeToElement(new { baz = 6 }), UpdatedAt = DateTime.UtcNow };
        await context.UserLogs.AddAsync(logAffected);
        await context.UserLogs.AddAsync(logUpdatedBy);
        await context.SaveChangesAsync();

        await audit.AnonymizeUserLogsAsync(user.ID);

        var logs = await context.UserLogs.ToListAsync();
        Assert.All(logs, l => Assert.Equal(System.Text.Json.JsonSerializer.SerializeToElement(new { }).ToString(), l.OldValue?.ToString()));
        Assert.All(logs, l => Assert.Equal(System.Text.Json.JsonSerializer.SerializeToElement(new { }).ToString(), l.NewValue?.ToString()));
        // the logUpdatedBy should have UpdatedByID cleared
        var updatedByLog = logs.Where(l => l.ID == logUpdatedBy.ID).Single();
        Assert.Null(updatedByLog.UpdatedByID);
    }
}
