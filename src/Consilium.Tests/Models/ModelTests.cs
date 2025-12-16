using Consilium.Domain.Models;
using System.Text.Json;

namespace Consilium.Tests.Models;

public class UserTests
{
    [Fact]
    public void User_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.Equal(Guid.Empty, user.ID);
        Assert.Equal(string.Empty, user.Name);
        Assert.Equal(string.Empty, user.NIF);
        Assert.Equal(string.Empty, user.Email);
        Assert.Equal(string.Empty, user.PasswordHash);
        Assert.True(user.IsActive);
        Assert.Null(user.Client);
        Assert.Null(user.Lawyer);
        Assert.Null(user.Admin);
        Assert.NotNull(user.Phones);
        Assert.Empty(user.Phones);
    }

    [Fact]
    public void User_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var user = new User();
        var id = Guid.NewGuid();
        var client = new Client { ID = id };
        var lawyer = new Lawyer { ID = id };
        var admin = new Admin { ID = id };
        var phones = new List<Phone> { new Phone { ID = Guid.NewGuid() } };

        // Act
        user.ID = id;
        user.Name = "John Doe";
        user.NIF = "123456789";
        user.Email = "john@test.com";
        user.PasswordHash = "hashedpassword";
        user.IsActive = false;
        user.Client = client;
        user.Lawyer = lawyer;
        user.Admin = admin;
        user.Phones = phones;

        // Assert
        Assert.Equal(id, user.ID);
        Assert.Equal("John Doe", user.Name);
        Assert.Equal("123456789", user.NIF);
        Assert.Equal("john@test.com", user.Email);
        Assert.Equal("hashedpassword", user.PasswordHash);
        Assert.False(user.IsActive);
        Assert.Equal(client, user.Client);
        Assert.Equal(lawyer, user.Lawyer);
        Assert.Equal(admin, user.Admin);
        Assert.Single(user.Phones);
    }

    [Fact]
    public void User_IsActive_TogglesBetweenTrueAndFalse()
    {
        // Arrange
        var user = new User { IsActive = true };

        // Act & Assert
        Assert.True(user.IsActive);
        user.IsActive = false;
        Assert.False(user.IsActive);
        user.IsActive = true;
        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_Phones_CanAddMultiplePhones()
    {
        // Arrange
        var user = new User();
        var phone1 = new Phone { ID = Guid.NewGuid(), Number = "111111111" };
        var phone2 = new Phone { ID = Guid.NewGuid(), Number = "222222222" };

        // Act
        user.Phones.Add(phone1);
        user.Phones.Add(phone2);

        // Assert
        Assert.Equal(2, user.Phones.Count);
        Assert.Contains(phone1, user.Phones);
        Assert.Contains(phone2, user.Phones);
    }

    [Fact]
    public void User_NavigationProperties_CanBeNull()
    {
        // Arrange
        var user = new User
        {
            Client = null,
            Lawyer = null,
            Admin = null
        };

        // Assert
        Assert.Null(user.Client);
        Assert.Null(user.Lawyer);
        Assert.Null(user.Admin);
    }
}

public class ClientTests
{
    [Fact]
    public void Client_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var client = new Client();

        // Assert
        Assert.Equal(Guid.Empty, client.ID);
        Assert.Equal(string.Empty, client.Address);
        Assert.Null(client.User);
    }

    [Fact]
    public void Client_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var client = new Client();
        var id = Guid.NewGuid();
        var user = new User { ID = id };

        // Act
        client.ID = id;
        client.Address = "123 Main St";
        client.User = user;

        // Assert
        Assert.Equal(id, client.ID);
        Assert.Equal("123 Main St", client.Address);
        Assert.Equal(user, client.User);
        Assert.Equal(id, client.User.ID);
    }

    [Fact]
    public void Client_Address_AcceptsLongStrings()
    {
        // Arrange
        var client = new Client();
        var longAddress = new string('A', 500);

        // Act
        client.Address = longAddress;

        // Assert
        Assert.Equal(500, client.Address.Length);
        Assert.Equal(longAddress, client.Address);
    }

    [Fact]
    public void Client_Address_AcceptsEmptyString()
    {
        // Arrange & Act
        var client = new Client { Address = "" };

        // Assert
        Assert.Equal(string.Empty, client.Address);
    }
}

public class LawyerTests
{
    [Fact]
    public void Lawyer_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var lawyer = new Lawyer();

        // Assert
        Assert.Equal(Guid.Empty, lawyer.ID);
        Assert.Equal(string.Empty, lawyer.ProfessionalRegister);
        Assert.Null(lawyer.User);
    }

    [Fact]
    public void Lawyer_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var lawyer = new Lawyer();
        var id = Guid.NewGuid();
        var user = new User { ID = id };

        // Act
        lawyer.ID = id;
        lawyer.ProfessionalRegister = "OAB123456";
        lawyer.User = user;

        // Assert
        Assert.Equal(id, lawyer.ID);
        Assert.Equal("OAB123456", lawyer.ProfessionalRegister);
        Assert.Equal(user, lawyer.User);
    }

    [Fact]
    public void Lawyer_ProfessionalRegister_AcceptsMaxLength()
    {
        // Arrange
        var lawyer = new Lawyer();
        var maxRegister = new string('1', 20);

        // Act
        lawyer.ProfessionalRegister = maxRegister;

        // Assert
        Assert.Equal(20, lawyer.ProfessionalRegister.Length);
    }
}

public class AdminTests
{
    [Fact]
    public void Admin_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var admin = new Admin();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, admin.ID);
        Assert.True(admin.StartedAt >= beforeCreation && admin.StartedAt <= afterCreation);
        Assert.Null(admin.User);
    }

    [Fact]
    public void Admin_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var admin = new Admin();
        var id = Guid.NewGuid();
        var user = new User { ID = id };
        var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        admin.ID = id;
        admin.StartedAt = startDate;
        admin.User = user;

        // Assert
        Assert.Equal(id, admin.ID);
        Assert.Equal(startDate, admin.StartedAt);
        Assert.Equal(user, admin.User);
    }

    [Fact]
    public void Admin_StartedAt_CanBeSetToDifferentTimes()
    {
        // Arrange
        var admin = new Admin();
        var date1 = DateTime.UtcNow.AddDays(-30);
        var date2 = DateTime.UtcNow;
        var date3 = DateTime.UtcNow.AddDays(30);

        // Act & Assert
        admin.StartedAt = date1;
        Assert.Equal(date1, admin.StartedAt);

        admin.StartedAt = date2;
        Assert.Equal(date2, admin.StartedAt);

        admin.StartedAt = date3;
        Assert.Equal(date3, admin.StartedAt);
    }
}

public class ProcessTests
{
    [Fact]
    public void Process_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var process = new Process();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, process.Id);
        Assert.Null(process.Name);
        Assert.Null(process.Number);
        Assert.Equal(Guid.Empty, process.ClientId);
        Assert.Equal(Guid.Empty, process.LawyerId);
        Assert.Null(process.AdversePartName);
        Assert.Null(process.OpposingCounselName);
        Assert.True(process.CreatedAt >= beforeCreation && process.CreatedAt <= afterCreation);
        Assert.Null(process.ClosedAt);
        Assert.Null(process.Description);
        Assert.Null(process.NextHearingDate);
        Assert.Equal((short)0, process.Priority);
        Assert.Null(process.CourtInfo);
        Assert.Equal(0, process.ProcessTypePhaseId);
        Assert.Equal(0, process.ProcessStatusId);
        Assert.NotNull(process.Documents);
        Assert.Empty(process.Documents);
    }

    [Fact]
    public void Process_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var process = new Process();
        var id = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var lawyerId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var closedAt = DateTime.UtcNow.AddDays(90);
        var nextHearing = DateTime.UtcNow.AddDays(30);

        // Act
        process.Id = id;
        process.Name = "Civil Case";
        process.Number = "123-2024";
        process.ClientId = clientId;
        process.LawyerId = lawyerId;
        process.AdversePartName = "Adverse Party";
        process.OpposingCounselName = "Opposing Counsel";
        process.CreatedAt = createdAt;
        process.ClosedAt = closedAt;
        process.Description = "Process description";
        process.NextHearingDate = nextHearing;
        process.Priority = 5;
        process.CourtInfo = "District Court";
        process.ProcessTypePhaseId = 1;
        process.ProcessStatusId = 2;

        // Assert
        Assert.Equal(id, process.Id);
        Assert.Equal("Civil Case", process.Name);
        Assert.Equal("123-2024", process.Number);
        Assert.Equal(clientId, process.ClientId);
        Assert.Equal(lawyerId, process.LawyerId);
        Assert.Equal("Adverse Party", process.AdversePartName);
        Assert.Equal("Opposing Counsel", process.OpposingCounselName);
        Assert.Equal(createdAt, process.CreatedAt);
        Assert.Equal(closedAt, process.ClosedAt);
        Assert.Equal("Process description", process.Description);
        Assert.Equal(nextHearing, process.NextHearingDate);
        Assert.Equal((short)5, process.Priority);
        Assert.Equal("District Court", process.CourtInfo);
        Assert.Equal(1, process.ProcessTypePhaseId);
        Assert.Equal(2, process.ProcessStatusId);
    }

    [Fact]
    public void Process_Priority_AcceptsDifferentValues()
    {
        // Arrange
        var process = new Process();
        short[] priorities = { 1, 5, 10, 100, short.MaxValue };

        // Act & Assert
        foreach (var priority in priorities)
        {
            process.Priority = priority;
            Assert.Equal(priority, process.Priority);
        }
    }

    [Fact]
    public void Process_Documents_CanAddMultipleDocuments()
    {
        // Arrange
        var process = new Process();
        var doc1 = new Document { Id = Guid.NewGuid(), FileName = "doc1.pdf" };
        var doc2 = new Document { Id = Guid.NewGuid(), FileName = "doc2.pdf" };

        // Act
        process.Documents.Add(doc1);
        process.Documents.Add(doc2);

        // Assert
        Assert.Equal(2, process.Documents.Count);
        Assert.Contains(doc1, process.Documents);
        Assert.Contains(doc2, process.Documents);
    }

    [Fact]
    public void Process_NavigationProperties_CanBeSet()
    {
        // Arrange
        var process = new Process();
        var client = new Client { ID = Guid.NewGuid() };
        var lawyer = new Lawyer { ID = Guid.NewGuid() };
        var status = new ProcessStatus { Id = 1, Name = "Active" };
        var typePhase = new ProcessTypePhase { Id = 1 };

        // Act
        process.Client = client;
        process.Lawyer = lawyer;
        process.Status = status;
        process.ProcessTypePhase = typePhase;

        // Assert
        Assert.Equal(client, process.Client);
        Assert.Equal(lawyer, process.Lawyer);
        Assert.Equal(status, process.Status);
        Assert.Equal(typePhase, process.ProcessTypePhase);
    }

    [Fact]
    public void Process_OptionalFields_CanBeNull()
    {
        // Arrange & Act
        var process = new Process
        {
            AdversePartName = null,
            OpposingCounselName = null,
            ClosedAt = null,
            Description = null,
            NextHearingDate = null,
            ProcessTypePhase = null
        };

        // Assert
        Assert.Null(process.AdversePartName);
        Assert.Null(process.OpposingCounselName);
        Assert.Null(process.ClosedAt);
        Assert.Null(process.Description);
        Assert.Null(process.NextHearingDate);
        Assert.Null(process.ProcessTypePhase);
    }
}

public class DocumentTests
{
    [Fact]
    public void Document_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var document = new Document();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.Equal(Guid.Empty, document.Id);
        Assert.Equal(Guid.Empty, document.ProcessId);
        Assert.Null(document.FileName);
        Assert.Null(document.File);
        Assert.Null(document.FileMimeType);
        Assert.Equal(0L, document.FileSize);
        Assert.True(document.CreatedAt >= beforeCreation && document.CreatedAt <= afterCreation);
        Assert.Null(document.Process);
    }

    [Fact]
    public void Document_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var document = new Document();
        var id = Guid.NewGuid();
        var processId = Guid.NewGuid();
        var fileBytes = new byte[] { 1, 2, 3, 4, 5 };
        var createdAt = DateTime.UtcNow;
        var process = new Process { Id = processId };

        // Act
        document.Id = id;
        document.ProcessId = processId;
        document.FileName = "contract.pdf";
        document.File = fileBytes;
        document.FileMimeType = "application/pdf";
        document.FileSize = 1024000;
        document.CreatedAt = createdAt;
        document.Process = process;

        // Assert
        Assert.Equal(id, document.Id);
        Assert.Equal(processId, document.ProcessId);
        Assert.Equal("contract.pdf", document.FileName);
        Assert.Equal(fileBytes, document.File);
        Assert.Equal("application/pdf", document.FileMimeType);
        Assert.Equal(1024000, document.FileSize);
        Assert.Equal(createdAt, document.CreatedAt);
        Assert.Equal(process, document.Process);
    }

    [Fact]
    public void Document_FileSize_AcceptsDifferentValues()
    {
        // Arrange
        var document = new Document();
        long[] sizes = { 0, 1024, 1048576, 10485760, long.MaxValue };

        // Act & Assert
        foreach (var size in sizes)
        {
            document.FileSize = size;
            Assert.Equal(size, document.FileSize);
        }
    }

    [Fact]
    public void Document_File_CanStoreEmptyArray()
    {
        // Arrange
        var document = new Document();
        var emptyArray = Array.Empty<byte>();

        // Act
        document.File = emptyArray;

        // Assert
        Assert.NotNull(document.File);
        Assert.Empty(document.File);
    }

    [Fact]
    public void Document_FileMimeType_AcceptsDifferentTypes()
    {
        // Arrange
        var document = new Document();
        var mimeTypes = new[] { "application/pdf", "image/jpeg", "text/plain", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

        // Act & Assert
        foreach (var mimeType in mimeTypes)
        {
            document.FileMimeType = mimeType;
            Assert.Equal(mimeType, document.FileMimeType);
        }
    }
}

public class ProcessTypeTests
{
    [Fact]
    public void ProcessType_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var processType = new ProcessType();

        // Assert
        Assert.Equal(0, processType.Id);
        Assert.Null(processType.Name);
        Assert.True(processType.IsActive);
    }

    [Fact]
    public void ProcessType_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var processType = new ProcessType();

        // Act
        processType.Id = 1;
        processType.Name = "Civil Process";
        processType.IsActive = false;

        // Assert
        Assert.Equal(1, processType.Id);
        Assert.Equal("Civil Process", processType.Name);
        Assert.False(processType.IsActive);
    }

    [Fact]
    public void ProcessType_IsActive_TogglesBetweenTrueAndFalse()
    {
        // Arrange
        var processType = new ProcessType { IsActive = true };

        // Act & Assert
        Assert.True(processType.IsActive);
        processType.IsActive = false;
        Assert.False(processType.IsActive);
        processType.IsActive = true;
        Assert.True(processType.IsActive);
    }
}

public class ProcessPhaseTests
{
    [Fact]
    public void ProcessPhase_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var phase = new ProcessPhase();

        // Assert
        Assert.Equal(0, phase.Id);
        Assert.Null(phase.Name);
        Assert.Null(phase.Description);
        Assert.True(phase.IsActive);
        Assert.NotNull(phase.ProcessTypePhases);
        Assert.Empty(phase.ProcessTypePhases);
    }

    [Fact]
    public void ProcessPhase_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var phase = new ProcessPhase();
        var typePhases = new List<ProcessTypePhase>
        {
            new ProcessTypePhase { Id = 1 }
        };

        // Act
        phase.Id = 1;
        phase.Name = "Initial Phase";
        phase.Description = "First phase of the process";
        phase.IsActive = false;
        phase.ProcessTypePhases = typePhases;

        // Assert
        Assert.Equal(1, phase.Id);
        Assert.Equal("Initial Phase", phase.Name);
        Assert.Equal("First phase of the process", phase.Description);
        Assert.False(phase.IsActive);
        Assert.Single(phase.ProcessTypePhases);
    }

    [Fact]
    public void ProcessPhase_Description_CanBeNull()
    {
        // Arrange & Act
        var phase = new ProcessPhase { Description = null };

        // Assert
        Assert.Null(phase.Description);
    }

    [Fact]
    public void ProcessPhase_ProcessTypePhases_CanAddMultiple()
    {
        // Arrange
        var phase = new ProcessPhase();
        var tp1 = new ProcessTypePhase { Id = 1 };
        var tp2 = new ProcessTypePhase { Id = 2 };

        // Act
        phase.ProcessTypePhases.Add(tp1);
        phase.ProcessTypePhases.Add(tp2);

        // Assert
        Assert.Equal(2, phase.ProcessTypePhases.Count);
    }
}

public class ProcessStatusTests
{
    [Fact]
    public void ProcessStatus_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var status = new ProcessStatus();

        // Assert
        Assert.Equal(0, status.Id);
        Assert.Null(status.Name);
        Assert.False(status.IsFinal);
        Assert.False(status.IsDefault);
        Assert.True(status.IsActive);
    }

    [Fact]
    public void ProcessStatus_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var status = new ProcessStatus();

        // Act
        status.Id = 1;
        status.Name = "In Progress";
        status.IsFinal = true;
        status.IsDefault = true;
        status.IsActive = false;

        // Assert
        Assert.Equal(1, status.Id);
        Assert.Equal("In Progress", status.Name);
        Assert.True(status.IsFinal);
        Assert.True(status.IsDefault);
        Assert.False(status.IsActive);
    }

    [Fact]
    public void ProcessStatus_BooleanFlags_AllCombinations()
    {
        // Test all 8 combinations
        var combinations = new[]
        {
            (IsFinal: true, IsDefault: true, IsActive: true),
            (IsFinal: true, IsDefault: true, IsActive: false),
            (IsFinal: true, IsDefault: false, IsActive: true),
            (IsFinal: true, IsDefault: false, IsActive: false),
            (IsFinal: false, IsDefault: true, IsActive: true),
            (IsFinal: false, IsDefault: true, IsActive: false),
            (IsFinal: false, IsDefault: false, IsActive: true),
            (IsFinal: false, IsDefault: false, IsActive: false)
        };

        foreach (var combo in combinations)
        {
            // Arrange & Act
            var status = new ProcessStatus
            {
                IsFinal = combo.IsFinal,
                IsDefault = combo.IsDefault,
                IsActive = combo.IsActive
            };

            // Assert
            Assert.Equal(combo.IsFinal, status.IsFinal);
            Assert.Equal(combo.IsDefault, status.IsDefault);
            Assert.Equal(combo.IsActive, status.IsActive);
        }
    }
}

public class ProcessTypePhaseTests
{
    [Fact]
    public void ProcessTypePhase_DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var typePhase = new ProcessTypePhase();

        // Assert
        Assert.Equal(0, typePhase.Id);
        Assert.Equal(0, typePhase.ProcessPhaseId);
        Assert.Equal(0, typePhase.ProcessTypeId);
        Assert.Equal((short)0, typePhase.TypePhaseOrder);
        Assert.False(typePhase.IsOptional);
        Assert.True(typePhase.IsActive);
        Assert.Null(typePhase.ProcessPhase);
        Assert.Null(typePhase.ProcessType);
    }

    [Fact]
    public void ProcessTypePhase_AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange
        var typePhase = new ProcessTypePhase();
        var phase = new ProcessPhase { Id = 1, Name = "Phase" };
        var type = new ProcessType { Id = 1, Name = "Type" };

        // Act
        typePhase.Id = 1;
        typePhase.ProcessPhaseId = 10;
        typePhase.ProcessTypeId = 20;
        typePhase.TypePhaseOrder = 5;
        typePhase.IsOptional = true;
        typePhase.IsActive = false;
        typePhase.ProcessPhase = phase;
        typePhase.ProcessType = type;

        // Assert
        Assert.Equal(1, typePhase.Id);
        Assert.Equal(10, typePhase.ProcessPhaseId);
        Assert.Equal(20, typePhase.ProcessTypeId);
        Assert.Equal((short)5, typePhase.TypePhaseOrder);
        Assert.True(typePhase.IsOptional);
        Assert.False(typePhase.IsActive);
        Assert.Equal(phase, typePhase.ProcessPhase);
        Assert.Equal(type, typePhase.ProcessType);
    }

    [Fact]
    public void ProcessTypePhase_TypePhaseOrder_AcceptsDifferentValues()
    {
        // Arrange
        var typePhase = new ProcessTypePhase();
        short[] orders = { 1, 5, 10, 100, short.MaxValue };

        // Act & Assert
        foreach (var order in orders)
        {
            typePhase.TypePhaseOrder = order;
            Assert.Equal(order, typePhase.TypePhaseOrder);
        }
    }

    [Fact]
    public void ProcessTypePhase_BooleanFlags_AllCombinations()
    {
        // Test all 4 combinations
        var combinations = new[]
        {
            (IsOptional: true, IsActive: true),
            (IsOptional: true, IsActive: false),
            (IsOptional: false, IsActive: true),
            (IsOptional: false, IsActive: false)
        };

        foreach (var combo in combinations)
        {
            // Arrange & Act
            var typePhase = new ProcessTypePhase
            {
                IsOptional = combo.IsOptional,
                IsActive = combo.IsActive
            };

            // Assert
            Assert.Equal(combo.IsOptional, typePhase.IsOptional);
            Assert.Equal(combo.IsActive, typePhase.IsActive);
        }
    }
}
