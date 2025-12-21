using Consilium.Application.Dtos;

namespace Consilium.Tests.DTOs;

/// <summary>
/// Tests for UnreadProcessStats record to ensure 100% branch coverage
/// </summary>
public class UnreadProcessStatsTests
{
    [Fact]
    public void UnreadProcessStats_WithAllValidParameters_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var processName = "Civil Lawsuit";
        var processNumber = "2024-001";
        var count = 5;

        // Act
        var stats = new UnreadProcessStats(processId, processName, processNumber, count);

        // Assert
        Assert.Equal(processId, stats.ProcessId);
        Assert.Equal(processName, stats.ProcessName);
        Assert.Equal(processNumber, stats.ProcessNumber);
        Assert.Equal(count, stats.Count);
    }

    [Fact]
    public void UnreadProcessStats_WithZeroCount_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var processName = "Criminal Case";
        var processNumber = "2024-002";
        var count = 0;

        // Act
        var stats = new UnreadProcessStats(processId, processName, processNumber, count);

        // Assert
        Assert.Equal(0, stats.Count);
    }

    [Fact]
    public void UnreadProcessStats_WithNegativeCount_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var count = -1;

        // Act
        var stats = new UnreadProcessStats(processId, "Process", "123", count);

        // Assert
        Assert.Equal(-1, stats.Count);
    }

    [Fact]
    public void UnreadProcessStats_WithMaxCount_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var count = int.MaxValue;

        // Act
        var stats = new UnreadProcessStats(processId, "Process", "123", count);

        // Assert
        Assert.Equal(int.MaxValue, stats.Count);
    }

    [Fact]
    public void UnreadProcessStats_WithEmptyGuid_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.Empty;

        // Act
        var stats = new UnreadProcessStats(processId, "Process", "123", 5);

        // Assert
        Assert.Equal(Guid.Empty, stats.ProcessId);
    }

    [Fact]
    public void UnreadProcessStats_WithEmptyProcessName_CreatesSuccessfully()
    {
        // Arrange
        var processName = string.Empty;

        // Act
        var stats = new UnreadProcessStats(Guid.NewGuid(), processName, "123", 5);

        // Assert
        Assert.Equal(string.Empty, stats.ProcessName);
    }

    [Fact]
    public void UnreadProcessStats_WithEmptyProcessNumber_CreatesSuccessfully()
    {
        // Arrange
        var processNumber = string.Empty;

        // Act
        var stats = new UnreadProcessStats(Guid.NewGuid(), "Process", processNumber, 5);

        // Assert
        Assert.Equal(string.Empty, stats.ProcessNumber);
    }

    [Fact]
    public void UnreadProcessStats_WithLongStrings_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var longName = new string('A', 1000);
        var longNumber = new string('1', 500);
        var count = 10;

        // Act
        var stats = new UnreadProcessStats(processId, longName, longNumber, count);

        // Assert
        Assert.Equal(1000, stats.ProcessName.Length);
        Assert.Equal(500, stats.ProcessNumber.Length);
    }

    [Fact]
    public void UnreadProcessStats_WithSpecialCharacters_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var processName = "Processo Cível & Trabalhista (Urgente)";
        var processNumber = "2024-001/SP-123";
        var count = 3;

        // Act
        var stats = new UnreadProcessStats(processId, processName, processNumber, count);

        // Assert
        Assert.Equal(processName, stats.ProcessName);
        Assert.Equal(processNumber, stats.ProcessNumber);
    }

    [Fact]
    public void UnreadProcessStats_WithUnicodeCharacters_CreatesSuccessfully()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var processName = "Процесс 法律案件 🏛️";
        var processNumber = "2024-001-מס";
        var count = 7;

        // Act
        var stats = new UnreadProcessStats(processId, processName, processNumber, count);

        // Assert
        Assert.Equal(processName, stats.ProcessName);
        Assert.Equal(processNumber, stats.ProcessNumber);
        Assert.Contains("🏛️", stats.ProcessName);
    }

    [Fact]
    public void UnreadProcessStats_Equality_SameValues_AreEqual()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats1 = new UnreadProcessStats(processId, "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(processId, "Process A", "001", 5);

        // Act & Assert
        Assert.Equal(stats1, stats2);
        Assert.True(stats1 == stats2);
        Assert.False(stats1 != stats2);
    }

    [Fact]
    public void UnreadProcessStats_Equality_DifferentProcessId_AreNotEqual()
    {
        // Arrange
        var stats1 = new UnreadProcessStats(Guid.NewGuid(), "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(Guid.NewGuid(), "Process A", "001", 5);

        // Act & Assert
        Assert.NotEqual(stats1, stats2);
        Assert.False(stats1 == stats2);
        Assert.True(stats1 != stats2);
    }

    [Fact]
    public void UnreadProcessStats_Equality_DifferentProcessName_AreNotEqual()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats1 = new UnreadProcessStats(processId, "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(processId, "Process B", "001", 5);

        // Act & Assert
        Assert.NotEqual(stats1, stats2);
    }

    [Fact]
    public void UnreadProcessStats_Equality_DifferentProcessNumber_AreNotEqual()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats1 = new UnreadProcessStats(processId, "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(processId, "Process A", "002", 5);

        // Act & Assert
        Assert.NotEqual(stats1, stats2);
    }

    [Fact]
    public void UnreadProcessStats_Equality_DifferentCount_AreNotEqual()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats1 = new UnreadProcessStats(processId, "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(processId, "Process A", "001", 10);

        // Act & Assert
        Assert.NotEqual(stats1, stats2);
    }

    [Fact]
    public void UnreadProcessStats_GetHashCode_SameValues_SameHashCode()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats1 = new UnreadProcessStats(processId, "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(processId, "Process A", "001", 5);

        // Act
        var hash1 = stats1.GetHashCode();
        var hash2 = stats2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void UnreadProcessStats_GetHashCode_DifferentValues_DifferentHashCode()
    {
        // Arrange
        var stats1 = new UnreadProcessStats(Guid.NewGuid(), "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(Guid.NewGuid(), "Process B", "002", 10);

        // Act
        var hash1 = stats1.GetHashCode();
        var hash2 = stats2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void UnreadProcessStats_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats = new UnreadProcessStats(processId, "Civil Case", "2024-001", 5);

        // Act
        var result = stats.ToString();

        // Assert
        Assert.Contains("Civil Case", result);
        Assert.Contains("2024-001", result);
        Assert.Contains("5", result);
        Assert.Contains(processId.ToString(), result);
    }

    [Fact]
    public void UnreadProcessStats_Deconstruction_WorksCorrectly()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var processName = "Criminal Case";
        var processNumber = "2024-002";
        var count = 7;
        var stats = new UnreadProcessStats(processId, processName, processNumber, count);

        // Act
        var (id, name, number, cnt) = stats;

        // Assert
        Assert.Equal(processId, id);
        Assert.Equal(processName, name);
        Assert.Equal(processNumber, number);
        Assert.Equal(count, cnt);
    }

    [Fact]
    public void UnreadProcessStats_WithCopying_CreatesNewInstance()
    {
        // Arrange
        var original = new UnreadProcessStats(Guid.NewGuid(), "Process A", "001", 5);

        // Act
        var copy = original with { Count = 10 };

        // Assert
        Assert.Equal(original.ProcessId, copy.ProcessId);
        Assert.Equal(original.ProcessName, copy.ProcessName);
        Assert.Equal(original.ProcessNumber, copy.ProcessNumber);
        Assert.NotEqual(original.Count, copy.Count);
        Assert.Equal(10, copy.Count);
    }

    [Fact]
    public void UnreadProcessStats_WithCopyingAllProperties_CreatesNewInstance()
    {
        // Arrange
        var original = new UnreadProcessStats(Guid.NewGuid(), "Process A", "001", 5);
        var newId = Guid.NewGuid();

        // Act
        var copy = original with 
        { 
            ProcessId = newId,
            ProcessName = "Process B",
            ProcessNumber = "002",
            Count = 15
        };

        // Assert
        Assert.Equal(newId, copy.ProcessId);
        Assert.Equal("Process B", copy.ProcessName);
        Assert.Equal("002", copy.ProcessNumber);
        Assert.Equal(15, copy.Count);
        Assert.NotEqual(original, copy);
    }

    [Fact]
    public void UnreadProcessStats_MultipleInstances_AreIndependent()
    {
        // Arrange
        var stats1 = new UnreadProcessStats(Guid.NewGuid(), "Process 1", "001", 5);
        var stats2 = new UnreadProcessStats(Guid.NewGuid(), "Process 2", "002", 10);
        var stats3 = new UnreadProcessStats(Guid.NewGuid(), "Process 3", "003", 15);

        // Assert
        Assert.NotEqual(stats1.ProcessId, stats2.ProcessId);
        Assert.NotEqual(stats2.ProcessId, stats3.ProcessId);
        Assert.NotEqual(stats1.Count, stats2.Count);
        Assert.NotEqual(stats2.Count, stats3.Count);
    }

    [Fact]
    public void UnreadProcessStats_InCollection_CanBeQueried()
    {
        // Arrange
        var stats1 = new UnreadProcessStats(Guid.NewGuid(), "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(Guid.NewGuid(), "Process B", "002", 10);
        var stats3 = new UnreadProcessStats(Guid.NewGuid(), "Process C", "003", 0);

        var collection = new List<UnreadProcessStats> { stats1, stats2, stats3 };

        // Act
        var withMessages = collection.Where(s => s.Count > 0).ToList();
        var withoutMessages = collection.Where(s => s.Count == 0).ToList();
        var totalCount = collection.Sum(s => s.Count);

        // Assert
        Assert.Equal(2, withMessages.Count);
        Assert.Single(withoutMessages);
        Assert.Equal(15, totalCount);
    }

    [Fact]
    public void UnreadProcessStats_InDictionary_CanBeUsedAsKey()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats1 = new UnreadProcessStats(processId, "Process A", "001", 5);
        var stats2 = new UnreadProcessStats(processId, "Process A", "001", 5); // Same values

        var dictionary = new Dictionary<UnreadProcessStats, string>
        {
            { stats1, "First entry" }
        };

        // Act
        var containsStats1 = dictionary.ContainsKey(stats1);
        var containsStats2 = dictionary.ContainsKey(stats2);

        // Assert
        Assert.True(containsStats1);
        Assert.True(containsStats2); // Should be true because records with same values are equal
        Assert.Equal("First entry", dictionary[stats2]);
    }

    [Fact]
    public void UnreadProcessStats_Serialization_PreservesAllProperties()
    {
        // Arrange
        var processId = Guid.NewGuid();
        var stats = new UnreadProcessStats(processId, "Process A", "2024-001", 5);

        // Act - Simulate serialization/deserialization with deconstruction and reconstruction
        var (id, name, number, count) = stats;
        var reconstructed = new UnreadProcessStats(id, name, number, count);

        // Assert
        Assert.Equal(stats, reconstructed);
        Assert.Equal(stats.ProcessId, reconstructed.ProcessId);
        Assert.Equal(stats.ProcessName, reconstructed.ProcessName);
        Assert.Equal(stats.ProcessNumber, reconstructed.ProcessNumber);
        Assert.Equal(stats.Count, reconstructed.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void UnreadProcessStats_WithVariousCounts_CreatesSuccessfully(int count)
    {
        // Arrange & Act
        var stats = new UnreadProcessStats(Guid.NewGuid(), "Process", "001", count);

        // Assert
        Assert.Equal(count, stats.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("Process Name")]
    [InlineData("Processo Cível & Trabalhista")]
    [InlineData("Very Long Process Name With Many Characters That Exceeds Normal Length")]
    public void UnreadProcessStats_WithVariousProcessNames_CreatesSuccessfully(string processName)
    {
        // Arrange & Act
        var stats = new UnreadProcessStats(Guid.NewGuid(), processName, "001", 5);

        // Assert
        Assert.Equal(processName, stats.ProcessName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("001")]
    [InlineData("2024-001")]
    [InlineData("2024-001-SP-CNJ-12345")]
    public void UnreadProcessStats_WithVariousProcessNumbers_CreatesSuccessfully(string processNumber)
    {
        // Arrange & Act
        var stats = new UnreadProcessStats(Guid.NewGuid(), "Process", processNumber, 5);

        // Assert
        Assert.Equal(processNumber, stats.ProcessNumber);
    }

    [Fact]
    public void UnreadProcessStats_NullComparison_ReturnsFalse()
    {
        // Arrange
        var stats = new UnreadProcessStats(Guid.NewGuid(), "Process", "001", 5);
        UnreadProcessStats? nullStats = null;

        // Act & Assert
        Assert.False(stats.Equals(nullStats));
        Assert.False(stats == nullStats);
        Assert.True(stats != nullStats);
    }

    [Fact]
    public void UnreadProcessStats_CompareToNull_HandlesGracefully()
    {
        // Arrange
        UnreadProcessStats? nullStats1 = null;
        UnreadProcessStats? nullStats2 = null;

        // Act & Assert
        Assert.True(nullStats1 == nullStats2);
        Assert.False(nullStats1 != nullStats2);
    }
}
