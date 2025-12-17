using Microsoft.EntityFrameworkCore;
using Consilium.Domain.Models;
using Consilium.Infrastructure.Data;
using Consilium.Infrastructure.Repositories; // Just in case, though Repo is in Data namespace
using Consilium.Application.Interfaces;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Consilium.Tests.Repositories;

public class MessageRepositoryTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }
    
    // Helper to seed basic entities needed for messages
    private async Task<(User ClientUser, User LawyerUser, Process Process)> SeedBasics(AppDbContext context)
    {
        var clientUser = new User { ID = Guid.NewGuid(), Name = "Client", Email = "c@t.com", NIF = "1", PasswordHash = "x", IsActive = true };
        var lawyerUser = new User { ID = Guid.NewGuid(), Name = "Lawyer", Email = "l@t.com", NIF = "2", PasswordHash = "x", IsActive = true };
        
        var client = new Client { ID = clientUser.ID, Address = "Addr" };
        var lawyer = new Lawyer { ID = lawyerUser.ID, ProfessionalRegister = "REG" };
        
        await context.Users.AddRangeAsync(clientUser, lawyerUser);
        await context.Clients.AddAsync(client);
        await context.Lawyers.AddAsync(lawyer);
        
        var ptype = new ProcessType { Name = "Civil" };
        var pphase = new ProcessPhase { Name = "Initial" };
        var pstatus = new ProcessStatus { Name = "Open" };
        
        await context.ProcessTypes.AddAsync(ptype);
        await context.ProcessPhases.AddAsync(pphase);
        await context.ProcessStatuses.AddAsync(pstatus);
        await context.SaveChangesAsync();
        
        var typePhase = new ProcessTypePhase { ProcessPhaseId = pphase.Id, ProcessTypeId = ptype.Id, TypePhaseOrder = 1 };
        await context.ProcessTypePhases.AddAsync(typePhase);
        await context.SaveChangesAsync();
        
        var process = new Process 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test Process", 
            Number = "P-1", 
            ClientId = client.ID, 
            LawyerId = lawyer.ID,
            ProcessTypePhaseId = typePhase.Id,
            ProcessStatusId = pstatus.Id,
            CreatedAt = DateTime.UtcNow,
            CourtInfo = "Court A"
        };
        await context.Processes.AddAsync(process);
        await context.SaveChangesAsync();
        
        return (clientUser, lawyerUser, process);
    }

    [Fact]
    public async Task GetAll_ReturnsMessages()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        
        var (client, lawyer, process) = await SeedBasics(context);
        
        var msg1 = new Message { SenderId = lawyer.ID, RecipientId = client.ID, ProcessId = process.Id, Subject = "S1", Body = "B1", CreatedAt = DateTime.UtcNow };
        var msg2 = new Message { SenderId = client.ID, RecipientId = lawyer.ID, ProcessId = process.Id, Subject = "S2", Body = "B2", CreatedAt = DateTime.UtcNow.AddMinutes(1) };
        
        await context.Messages.AddRangeAsync(msg1, msg2);
        await context.SaveChangesAsync();
        
        var (items, count) = await repo.GetAll(null, 1, 10, "date", "desc");
        
        Assert.Equal(2, count);
        Assert.Equal(2, items.Count());
        Assert.Equal("S2", items.First().Subject); // Descending order
    }
    
    [Fact]
    public async Task GetAll_Search_ReturnsFiltered()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        
        var (client, lawyer, process) = await SeedBasics(context);
        
        var msg1 = new Message { SenderId = lawyer.ID, RecipientId = client.ID, ProcessId = process.Id, Subject = "FindMe", Body = "B1", CreatedAt = DateTime.UtcNow };
        var msg2 = new Message { SenderId = client.ID, RecipientId = lawyer.ID, ProcessId = process.Id, Subject = "Ignore", Body = "B2", CreatedAt = DateTime.UtcNow };
        
        await context.Messages.AddRangeAsync(msg1, msg2);
        await context.SaveChangesAsync();
        
        var (items, count) = await repo.GetAll("findme", 1, 10, null, null);
        
        Assert.Equal(1, count);
        Assert.Equal("FindMe", items.First().Subject);
    }
    
    [Fact]
    public async Task GetByProcessId_ReturnsMessagesForProcess()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        
        var (client, lawyer, process) = await SeedBasics(context);
        
        var msg1 = new Message { SenderId = lawyer.ID, RecipientId = client.ID, ProcessId = process.Id, Subject = "M1", Body = "B1", CreatedAt = DateTime.UtcNow };
        await context.Messages.AddAsync(msg1);
        await context.SaveChangesAsync();
        
        var (items, count) = await repo.GetByProcessId(process.Id, 1, 10);
        Assert.Equal(1, count);
        Assert.Equal(msg1.Id, items.First().Id);
    }

    [Fact]
    public async Task UpdateLawyer_UpdatesCorrectParticipant()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        
        var (client, lawyer, process) = await SeedBasics(context);
        
        // Message where Sender is Lawyer
        var msg1 = new Message { SenderId = lawyer.ID, RecipientId = client.ID, ProcessId = process.Id, Subject = "M1", Body = "B1", CreatedAt = DateTime.UtcNow };
        await context.Messages.AddAsync(msg1);
        await context.SaveChangesAsync();
        
        // New Lawyer
        var newLawyerUser = new User { ID = Guid.NewGuid(), Name = "NewL", Email = "nl@t.com", NIF = "3", PasswordHash = "x", IsActive = true };
        var newLawyer = new Lawyer { ID = newLawyerUser.ID, ProfessionalRegister = "REG3" };
        await context.Users.AddAsync(newLawyerUser);
        await context.Lawyers.AddAsync(newLawyer);
        await context.SaveChangesAsync();
        
        var updated = await repo.UpdateLawyer(msg1.Id, newLawyer.ID);
        
        Assert.NotNull(updated);
        // Ensure Sender is now the new lawyer
        Assert.Equal(newLawyer.ID, updated.SenderId);
        // Recipient should remain client
        Assert.Equal(client.ID, updated.RecipientId);
    }
    
    [Fact]
    public async Task UpdateLawyer_WhenRecipientIsLawyer_UpdatesRecipient()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        
        var (client, lawyer, process) = await SeedBasics(context);
        
        // Message where Recipient is Lawyer
        var msg1 = new Message { SenderId = client.ID, RecipientId = lawyer.ID, ProcessId = process.Id, Subject = "M1", Body = "B1", CreatedAt = DateTime.UtcNow };
        await context.Messages.AddAsync(msg1);
        await context.SaveChangesAsync();
        
        // New Lawyer
        var newLawyerUser = new User { ID = Guid.NewGuid(), Name = "NewL", Email = "nl@t.com", NIF = "3", PasswordHash = "x", IsActive = true };
        var newLawyer = new Lawyer { ID = newLawyerUser.ID, ProfessionalRegister = "REG3" };
        await context.Users.AddAsync(newLawyerUser);
        await context.Lawyers.AddAsync(newLawyer);
        await context.SaveChangesAsync();
        
        var updated = await repo.UpdateLawyer(msg1.Id, newLawyer.ID);
        
        Assert.NotNull(updated);
        // Ensure Recipient is now the new lawyer
        Assert.Equal(newLawyer.ID, updated.RecipientId);
        // Sender should remain client
        Assert.Equal(client.ID, updated.SenderId);
    }
    
    [Fact]
    public async Task UpdateLawyer_NotFound_ReturnsNull()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        var res = await repo.UpdateLawyer(123, Guid.NewGuid());
        Assert.Null(res);
    }

    [Fact]
    public async Task GetAll_SortBySubject_Asc()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        
        var (client, lawyer, process) = await SeedBasics(context);
        
        var msg1 = new Message { SenderId = lawyer.ID, RecipientId = client.ID, ProcessId = process.Id, Subject = "Zebra", Body = "B1", CreatedAt = DateTime.UtcNow };
        var msg2 = new Message { SenderId = client.ID, RecipientId = lawyer.ID, ProcessId = process.Id, Subject = "Alpha", Body = "B2", CreatedAt = DateTime.UtcNow };
        
        await context.Messages.AddRangeAsync(msg1, msg2);
        await context.SaveChangesAsync();
        
        var (items, count) = await repo.GetAll(null, 1, 10, "subject", "asc");
        
        Assert.Equal("Alpha", items.First().Subject);
    }
    
    [Fact]
    public async Task GetAll_DefaultSort()
    {
        await using var context = GetInMemoryDbContext();
        var repo = new MessageRepository(context);
        
        var (client, lawyer, process) = await SeedBasics(context);
        
        var msg1 = new Message { SenderId = lawyer.ID, RecipientId = client.ID, ProcessId = process.Id, Subject = "A", Body = "B1", CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var msg2 = new Message { SenderId = client.ID, RecipientId = lawyer.ID, ProcessId = process.Id, Subject = "B", Body = "B2", CreatedAt = DateTime.UtcNow };
        
        await context.Messages.AddRangeAsync(msg1, msg2);
        await context.SaveChangesAsync();
        
        var (items, count) = await repo.GetAll(null, 1, 10, null, null);
        
        Assert.Equal("B", items.First().Subject); // Default is CreatedAt Desc
    }
}
