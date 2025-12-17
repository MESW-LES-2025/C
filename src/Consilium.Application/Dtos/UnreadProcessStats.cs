namespace Consilium.Application.Dtos;

public record UnreadProcessStats(Guid ProcessId, string ProcessName, string ProcessNumber, int Count);
