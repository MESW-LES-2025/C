using Consilium.Domain.Models;
using System.Text.Json;

namespace Consilium.Tests.Models;

public class ProcessLogTests
{
    [Fact]
    public void ProcessLog_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var log = new ProcessLog();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, log.ID);
        Assert.Equal(Guid.Empty, log.ProcessID);
        Assert.Null(log.UpdatedByID);
        Assert.Equal(Guid.Empty, log.ActionLogTypeID);
        Assert.Null(log.OldValue);
        Assert.Null(log.NewValue);
        Assert.True(log.UpdatedAt >= beforeCreation && log.UpdatedAt <= afterCreation);
        Assert.Null(log.ActionLogType);
    }

    [Fact]
    public void ProcessLog_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var log = new ProcessLog();
        var id = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var updatedById = Guid.NewGuid();
        var actionTypeId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;
        var actionLogType = new ActionLogType { ID = actionTypeId, Name = "UPDATE" };

        var oldValueJson = JsonDocument.Parse("{\"name\":\"Old Name\"}").RootElement;
        var newValueJson = JsonDocument.Parse("{\"name\":\"New Name\"}").RootElement;

        // Act
        log.ID = id;
        log.ProcessID = processId;
        log.UpdatedByID = updatedById;
        log.ActionLogTypeID = actionTypeId;
        log.OldValue = oldValueJson;
        log.NewValue = newValueJson;
        log.UpdatedAt = updatedAt;
        log.ActionLogType = actionLogType;

        // Assert
        Assert.Equal(id, log.ID);
        Assert.Equal(processId, log.ProcessID);
        Assert.Equal(updatedById, log.UpdatedByID);
        Assert.Equal(actionTypeId, log.ActionLogTypeID);
        Assert.NotNull(log.OldValue);
        Assert.NotNull(log.NewValue);
        Assert.Equal("Old Name", log.OldValue.Value.GetProperty("name").GetString());
        Assert.Equal("New Name", log.NewValue.Value.GetProperty("name").GetString());
        Assert.Equal(updatedAt, log.UpdatedAt);
        Assert.Equal(actionLogType, log.ActionLogType);
    }

    [Fact]
    public void ProcessLog_UpdatedByID_CanBeNull()
    {
        // Arrange & Act
        var log = new ProcessLog { UpdatedByID = null };

        // Assert
        Assert.Null(log.UpdatedByID);
    }

    [Fact]
    public void ProcessLog_JsonValues_CanBeNull()
    {
        // Arrange & Act
        var log = new ProcessLog
        {
            OldValue = null,
            NewValue = null
        };

        // Assert
        Assert.Null(log.OldValue);
        Assert.Null(log.NewValue);
    }

    [Fact]
    public void ProcessLog_JsonValues_CanStoreComplexObjects()
    {
        // Arrange
        var log = new ProcessLog();
        var complexJson = JsonDocument.Parse(@"{
            ""name"": ""Process Name"",
            ""status"": ""active"",
            ""priority"": 5,
            ""nested"": {
                ""key1"": ""value1"",
                ""key2"": [1, 2, 3]
            }
        }").RootElement;

        // Act
        log.OldValue = complexJson;

        // Assert
        Assert.NotNull(log.OldValue);
        Assert.Equal("Process Name", log.OldValue.Value.GetProperty("name").GetString());
        Assert.Equal(5, log.OldValue.Value.GetProperty("priority").GetInt32());
        Assert.Equal("value1", log.OldValue.Value.GetProperty("nested").GetProperty("key1").GetString());
    }
}

public class DocumentLogTests
{
    [Fact]
    public void DocumentLog_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var log = new DocumentLog();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, log.Id);
        Assert.Equal(Guid.Empty, log.DocumentId);
        Assert.Equal(Guid.Empty, log.UpdatedBy);
        Assert.Equal(0, log.ActionLogTypeId);
        Assert.True(log.UpdatedAt >= beforeCreation && log.UpdatedAt <= afterCreation);
        
        // Check that default JSON is empty object
        Assert.Equal(JsonValueKind.Object, log.OldValue.ValueKind);
        Assert.Equal(JsonValueKind.Object, log.NewValue.ValueKind);
        
        Assert.Null(log.Document);
        Assert.Null(log.UpdatedByUser);
        Assert.Null(log.ActionLogType);
    }

    [Fact]
    public void DocumentLog_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var log = new DocumentLog();
        var id = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var updatedBy = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;
        var document = new Document { Id = documentId };
        var user = new User { ID = updatedBy };
        var actionLogType = new ActionLogType { ID = Guid.NewGuid(), Name = "CREATE" };

        var oldValueJson = JsonDocument.Parse("{\"fileName\":\"old.pdf\"}").RootElement;
        var newValueJson = JsonDocument.Parse("{\"fileName\":\"new.pdf\"}").RootElement;

        // Act
        log.Id = id;
        log.DocumentId = documentId;
        log.UpdatedBy = updatedBy;
        log.ActionLogTypeId = 1;
        log.UpdatedAt = updatedAt;
        log.OldValue = oldValueJson;
        log.NewValue = newValueJson;
        log.Document = document;
        log.UpdatedByUser = user;
        log.ActionLogType = actionLogType;

        // Assert
        Assert.Equal(id, log.Id);
        Assert.Equal(documentId, log.DocumentId);
        Assert.Equal(updatedBy, log.UpdatedBy);
        Assert.Equal(1, log.ActionLogTypeId);
        Assert.Equal(updatedAt, log.UpdatedAt);
        Assert.Equal("old.pdf", log.OldValue.GetProperty("fileName").GetString());
        Assert.Equal("new.pdf", log.NewValue.GetProperty("fileName").GetString());
        Assert.Equal(document, log.Document);
        Assert.Equal(user, log.UpdatedByUser);
        Assert.Equal(actionLogType, log.ActionLogType);
    }

    [Fact]
    public void DocumentLog_JsonValues_CanStoreEmptyObject()
    {
        // Arrange
        var log = new DocumentLog();
        var emptyJson = JsonDocument.Parse("{}").RootElement;

        // Act
        log.OldValue = emptyJson;
        log.NewValue = emptyJson;

        // Assert
        Assert.Equal(JsonValueKind.Object, log.OldValue.ValueKind);
        Assert.Equal(JsonValueKind.Object, log.NewValue.ValueKind);
    }

    [Fact]
    public void DocumentLog_JsonValues_CanStoreArrays()
    {
        // Arrange
        var log = new DocumentLog();
        var arrayJson = JsonDocument.Parse("[1, 2, 3, 4, 5]").RootElement;

        // Act
        log.NewValue = arrayJson;

        // Assert
        Assert.Equal(JsonValueKind.Array, log.NewValue.ValueKind);
        Assert.Equal(5, log.NewValue.GetArrayLength());
    }

    [Fact]
    public void DocumentLog_NavigationProperties_CanBeNull()
    {
        // Arrange & Act
        var log = new DocumentLog
        {
            Document = null,
            UpdatedByUser = null,
            ActionLogType = null
        };

        // Assert
        Assert.Null(log.Document);
        Assert.Null(log.UpdatedByUser);
        Assert.Null(log.ActionLogType);
    }
}

public class UserLogTests
{
    [Fact]
    public void UserLog_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var log = new UserLog();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, log.ID);
        Assert.Null(log.AffectedUserID);
        Assert.Null(log.UpdatedByID);
        Assert.Equal(Guid.Empty, log.ActionLogTypeID);
        Assert.Null(log.OldValue);
        Assert.Null(log.NewValue);
        Assert.True(log.UpdatedAt >= beforeCreation && log.UpdatedAt <= afterCreation);
        Assert.Null(log.ActionLogType);
    }

    [Fact]
    public void UserLog_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var log = new UserLog();
        var id = Guid.NewGuid();
        var affectedUserId = Guid.NewGuid();
        var updatedById = Guid.NewGuid();
        var actionTypeId = Guid.NewGuid();
        var updatedAt = DateTime.UtcNow;
        var actionLogType = new ActionLogType { ID = actionTypeId, Name = "DELETE" };

        var oldValueJson = JsonDocument.Parse("{\"email\":\"old@test.com\"}").RootElement;
        var newValueJson = JsonDocument.Parse("{\"email\":\"new@test.com\"}").RootElement;

        // Act
        log.ID = id;
        log.AffectedUserID = affectedUserId;
        log.UpdatedByID = updatedById;
        log.ActionLogTypeID = actionTypeId;
        log.OldValue = oldValueJson;
        log.NewValue = newValueJson;
        log.UpdatedAt = updatedAt;
        log.ActionLogType = actionLogType;

        // Assert
        Assert.Equal(id, log.ID);
        Assert.Equal(affectedUserId, log.AffectedUserID);
        Assert.Equal(updatedById, log.UpdatedByID);
        Assert.Equal(actionTypeId, log.ActionLogTypeID);
        Assert.NotNull(log.OldValue);
        Assert.NotNull(log.NewValue);
        Assert.Equal("old@test.com", log.OldValue.Value.GetProperty("email").GetString());
        Assert.Equal("new@test.com", log.NewValue.Value.GetProperty("email").GetString());
        Assert.Equal(updatedAt, log.UpdatedAt);
        Assert.Equal(actionLogType, log.ActionLogType);
    }

    [Fact]
    public void UserLog_OptionalGuids_CanBeNull()
    {
        // Arrange & Act
        var log = new UserLog
        {
            AffectedUserID = null,
            UpdatedByID = null
        };

        // Assert
        Assert.Null(log.AffectedUserID);
        Assert.Null(log.UpdatedByID);
    }

    [Fact]
    public void UserLog_OptionalGuids_CanHaveValues()
    {
        // Arrange
        var log = new UserLog();
        var affectedId = Guid.NewGuid();
        var updatedId = Guid.NewGuid();

        // Act
        log.AffectedUserID = affectedId;
        log.UpdatedByID = updatedId;

        // Assert
        Assert.Equal(affectedId, log.AffectedUserID);
        Assert.Equal(updatedId, log.UpdatedByID);
    }

    [Fact]
    public void UserLog_JsonValues_CanBeNull()
    {
        // Arrange & Act
        var log = new UserLog
        {
            OldValue = null,
            NewValue = null
        };

        // Assert
        Assert.Null(log.OldValue);
        Assert.Null(log.NewValue);
    }

    [Fact]
    public void UserLog_JsonValues_CanStoreNestedStructures()
    {
        // Arrange
        var log = new UserLog();
        var complexJson = JsonDocument.Parse(@"{
            ""user"": {
                ""name"": ""John Doe"",
                ""email"": ""john@test.com"",
                ""roles"": [""admin"", ""user""]
            },
            ""changes"": {
                ""field"": ""email"",
                ""from"": ""old@test.com"",
                ""to"": ""new@test.com""
            }
        }").RootElement;

        // Act
        log.NewValue = complexJson;

        // Assert
        Assert.NotNull(log.NewValue);
        Assert.Equal("John Doe", log.NewValue.Value.GetProperty("user").GetProperty("name").GetString());
        Assert.Equal("email", log.NewValue.Value.GetProperty("changes").GetProperty("field").GetString());
        Assert.Equal(2, log.NewValue.Value.GetProperty("user").GetProperty("roles").GetArrayLength());
    }
}

public class ActionLogTypeTests
{
    [Fact]
    public void ActionLogType_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var actionLogType = new ActionLogType();

        // Assert
        Assert.Equal(Guid.Empty, actionLogType.ID);
        Assert.Equal(string.Empty, actionLogType.Name);
        Assert.NotNull(actionLogType.UserLogs);
        Assert.Empty(actionLogType.UserLogs);
        Assert.NotNull(actionLogType.ProcessLogs);
        Assert.Empty(actionLogType.ProcessLogs);
    }

    [Fact]
    public void ActionLogType_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var actionLogType = new ActionLogType();
        var id = Guid.NewGuid();
        var userLogs = new List<UserLog> { new UserLog { ID = Guid.NewGuid() } };
        var processLogs = new List<ProcessLog> { new ProcessLog { ID = Guid.NewGuid() } };

        // Act
        actionLogType.ID = id;
        actionLogType.Name = "CREATE";
        actionLogType.UserLogs = userLogs;
        actionLogType.ProcessLogs = processLogs;

        // Assert
        Assert.Equal(id, actionLogType.ID);
        Assert.Equal("CREATE", actionLogType.Name);
        Assert.Single(actionLogType.UserLogs);
        Assert.Single(actionLogType.ProcessLogs);
    }

    [Fact]
    public void ActionLogType_Name_AcceptsMaxLength()
    {
        // Arrange
        var actionLogType = new ActionLogType();
        var maxName = new string('A', 100);

        // Act
        actionLogType.Name = maxName;

        // Assert
        Assert.Equal(100, actionLogType.Name.Length);
        Assert.Equal(maxName, actionLogType.Name);
    }

    [Fact]
    public void ActionLogType_UserLogs_CanAddMultiple()
    {
        // Arrange
        var actionLogType = new ActionLogType();
        var log1 = new UserLog { ID = Guid.NewGuid() };
        var log2 = new UserLog { ID = Guid.NewGuid() };
        var log3 = new UserLog { ID = Guid.NewGuid() };

        // Act
        actionLogType.UserLogs.Add(log1);
        actionLogType.UserLogs.Add(log2);
        actionLogType.UserLogs.Add(log3);

        // Assert
        Assert.Equal(3, actionLogType.UserLogs.Count);
        Assert.Contains(log1, actionLogType.UserLogs);
        Assert.Contains(log2, actionLogType.UserLogs);
        Assert.Contains(log3, actionLogType.UserLogs);
    }

    [Fact]
    public void ActionLogType_ProcessLogs_CanAddMultiple()
    {
        // Arrange
        var actionLogType = new ActionLogType();
        var log1 = new ProcessLog { ID = Guid.NewGuid() };
        var log2 = new ProcessLog { ID = Guid.NewGuid() };

        // Act
        actionLogType.ProcessLogs.Add(log1);
        actionLogType.ProcessLogs.Add(log2);

        // Assert
        Assert.Equal(2, actionLogType.ProcessLogs.Count);
        Assert.Contains(log1, actionLogType.ProcessLogs);
        Assert.Contains(log2, actionLogType.ProcessLogs);
    }

    [Fact]
    public void ActionLogType_Name_AcceptsDifferentActionTypes()
    {
        // Arrange & Act
        var actionTypes = new[] { "CREATE", "UPDATE", "DELETE", "READ", "ARCHIVE" };

        foreach (var actionType in actionTypes)
        {
            var action = new ActionLogType { Name = actionType };

            // Assert
            Assert.Equal(actionType, action.Name);
        }
    }

    [Fact]
    public void ActionLogType_Collections_CanBeReassigned()
    {
        // Arrange
        var actionLogType = new ActionLogType();
        var initialUserLogs = new List<UserLog> { new UserLog() };
        var newUserLogs = new List<UserLog> { new UserLog(), new UserLog() };

        // Act
        actionLogType.UserLogs = initialUserLogs;
        Assert.Single(actionLogType.UserLogs);

        actionLogType.UserLogs = newUserLogs;

        // Assert
        Assert.Equal(2, actionLogType.UserLogs.Count);
    }
}

// Edge cases and integration tests
public class ModelEdgeCaseTests
{
    [Fact]
    public void User_WithAllNavigationProperties_CreatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { ID = userId };
        var client = new Client { ID = userId, User = user };
        var lawyer = new Lawyer { ID = userId, User = user };
        var admin = new Admin { ID = userId, User = user };
        var phone1 = new Phone { ID = Guid.NewGuid(), UserID = userId, User = user };
        var phone2 = new Phone { ID = Guid.NewGuid(), UserID = userId, User = user };

        // Act
        user.Client = client;
        user.Lawyer = lawyer;
        user.Admin = admin;
        user.Phones.Add(phone1);
        user.Phones.Add(phone2);

        // Assert
        Assert.Equal(client, user.Client);
        Assert.Equal(lawyer, user.Lawyer);
        Assert.Equal(admin, user.Admin);
        Assert.Equal(2, user.Phones.Count);
        Assert.All(user.Phones, p => Assert.Equal(userId, p.UserID));
    }

    [Fact]
    public void Process_WithAllNavigationProperties_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var process = new Process { Id = processId };
        var client = new Client { ID = Guid.NewGuid() };
        var lawyer = new Lawyer { ID = Guid.NewGuid() };
        var status = new ProcessStatus { Id = 1, Name = "Active" };
        var typePhase = new ProcessTypePhase { Id = 1 };
        var doc1 = new Document { Id = Guid.NewGuid(), ProcessId = processId };
        var doc2 = new Document { Id = Guid.NewGuid(), ProcessId = processId };

        // Act
        process.Client = client;
        process.Lawyer = lawyer;
        process.Status = status;
        process.ProcessTypePhase = typePhase;
        process.Documents.Add(doc1);
        process.Documents.Add(doc2);

        // Assert
        Assert.Equal(client, process.Client);
        Assert.Equal(lawyer, process.Lawyer);
        Assert.Equal(status, process.Status);
        Assert.Equal(typePhase, process.ProcessTypePhase);
        Assert.Equal(2, process.Documents.Count);
    }

    [Fact]
    public void JsonElement_CanStoreDifferentDataTypes()
    {
        // Arrange
        var log = new ProcessLog();

        // Act & Assert - String
        var stringJson = JsonDocument.Parse("\"test string\"").RootElement;
        log.NewValue = stringJson;
        Assert.Equal(JsonValueKind.String, log.NewValue.Value.ValueKind);

        // Act & Assert - Number
        var numberJson = JsonDocument.Parse("12345").RootElement;
        log.NewValue = numberJson;
        Assert.Equal(JsonValueKind.Number, log.NewValue.Value.ValueKind);

        // Act & Assert - Boolean
        var boolJson = JsonDocument.Parse("true").RootElement;
        log.NewValue = boolJson;
        Assert.Equal(JsonValueKind.True, log.NewValue.Value.ValueKind);

        // Act & Assert - Null
        var nullJson = JsonDocument.Parse("null").RootElement;
        log.NewValue = nullJson;
        Assert.Equal(JsonValueKind.Null, log.NewValue.Value.ValueKind);

        // Act & Assert - Array
        var arrayJson = JsonDocument.Parse("[1,2,3]").RootElement;
        log.NewValue = arrayJson;
        Assert.Equal(JsonValueKind.Array, log.NewValue.Value.ValueKind);

        // Act & Assert - Object
        var objectJson = JsonDocument.Parse("{\"key\":\"value\"}").RootElement;
        log.NewValue = objectJson;
        Assert.Equal(JsonValueKind.Object, log.NewValue.Value.ValueKind);
    }

    [Fact]
    public void Document_WithLargeFile_HandlesCorrectly()
    {
        // Arrange
        var document = new Document();
        var largeFile = new byte[10485760]; // 10MB
        new Random().NextBytes(largeFile);

        // Act
        document.File = largeFile;
        document.FileSize = largeFile.Length;

        // Assert
        Assert.Equal(10485760, document.FileSize);
        Assert.Equal(largeFile.Length, document.File.Length);
    }

    [Fact]
    public void ProcessTypePhase_WithMultipleNavigationReferences_MaintainsIntegrity()
    {
        // Arrange
        var phase = new ProcessPhase { Id = 1, Name = "Initial" };
        var type = new ProcessType { Id = 1, Name = "Civil" };
        var typePhase = new ProcessTypePhase
        {
            Id = 1,
            ProcessPhaseId = 1,
            ProcessTypeId = 1,
            ProcessPhase = phase,
            ProcessType = type
        };

        // Act & Assert
        Assert.Equal(phase.Id, typePhase.ProcessPhaseId);
        Assert.Equal(type.Id, typePhase.ProcessTypeId);
        Assert.Equal(phase, typePhase.ProcessPhase);
        Assert.Equal(type, typePhase.ProcessType);
    }
}
