using Consilium.Domain.Models;

namespace Consilium.Tests.Domain;

public class DomainModelTests
{
    [Fact]
    public void Document_Getters_SetAndRead()
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            ProcessId = Guid.NewGuid(),
            FileName = "file.txt",
            File = new byte[] { 1, 2, 3 },
            FileMimeType = "text/plain",
            FileSize = 3,
            CreatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, doc.Id);
        Assert.NotEqual(Guid.Empty, doc.ProcessId);
        Assert.Equal("file.txt", doc.FileName);
        Assert.Equal(3, doc.FileSize);
        Assert.NotNull(doc.File);
    }

    [Fact]
    public void DocumentLog_Getters_SetAndRead()
    {
        var log = new DocumentLog
        {
            Id = Guid.NewGuid(),
            DocumentId = Guid.NewGuid(),
            UpdatedBy = Guid.NewGuid(),
            ActionLogTypeId = 1,
            UpdatedAt = DateTime.UtcNow,
            OldValue = System.Text.Json.JsonSerializer.SerializeToElement(new { a = 1 }),
            NewValue = System.Text.Json.JsonSerializer.SerializeToElement(new { b = 2 }),
        };

        Assert.NotEqual(Guid.Empty, log.Id);
        Assert.NotEqual(Guid.Empty, log.DocumentId);
        Assert.NotEqual(Guid.Empty, log.UpdatedBy);
        Assert.NotEqual(0, log.ActionLogTypeId);
        Assert.NotEqual<string>("{}", log.OldValue.ToString());
        Assert.NotEqual<string>("{}", log.NewValue.ToString());
    }

    [Fact]
    public void ProcessLog_Getters_SetAndRead()
    {
        var log = new ProcessLog
        {
            ID = Guid.NewGuid(),
            ProcessID = Guid.NewGuid(),
            UpdatedByID = Guid.NewGuid(),
            ActionLogTypeID = 1,
            OldValue = System.Text.Json.JsonSerializer.SerializeToElement(new { foo = 1 }),
            NewValue = System.Text.Json.JsonSerializer.SerializeToElement(new { bar = 2 }),
            UpdatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, log.ID);
        Assert.NotEqual(Guid.Empty, log.ProcessID);
        Assert.NotEqual(Guid.Empty, log.UpdatedByID);
    Assert.NotEqual(0, log.ActionLogTypeID);
        Assert.NotEqual<string>("{}", log.OldValue?.ToString());
        Assert.NotEqual<string>("{}", log.NewValue?.ToString());
    }

    [Fact]
    public void ProcessPhase_DefaultProperties()
    {
        var phase = new ProcessPhase { Id = 1, Name = "Phase1", Description = null, IsActive = true };
        Assert.Equal(1, phase.Id);
        Assert.Equal("Phase1", phase.Name);
        Assert.Null(phase.Description);
        Assert.True(phase.IsActive);
    }
}
