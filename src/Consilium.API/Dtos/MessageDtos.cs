
using System.ComponentModel.DataAnnotations;

namespace Consilium.API.Dtos;

public record MessageResponse(
    int Id,
    Guid SenderId,
    string SenderName,
    Guid RecipientId,
    string RecipientName,
    Guid ProcessId,
    string ProcessName,
    string Subject,
    string Body,
    DateTime CreatedAt,
    DateTime? ReadAt
);

public record CreateMessageRequest(
    [Required] Guid SenderId,
    [Required] Guid RecipientId,
    [Required] Guid ProcessId,
    [Required] string Subject,
    [Required] string Body
);

public record UpdateMessageLawyerRequest(
    [Required] Guid NewLawyerId
);

public record MarkAsReadRequest(
    [Required] Guid RecipientId
);
