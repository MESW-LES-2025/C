using System.ComponentModel.DataAnnotations.Schema;

namespace Consilium.Domain.Models;

[Table("message", Schema = "communication")]
public class Message
{
    [Column("message_id")]
    public int Id { get; set; }

    [Column("message_sender_id")]
    public Guid SenderId { get; set; }

    [Column("message_recipient_id")]
    public Guid RecipientId { get; set; }

    [Column("process_id")]
    public Guid ProcessId { get; set; }

    [Column("message_subject")]
    public string Subject { get; set; } = string.Empty;

    [Column("message_body")]
    public string Body { get; set; } = string.Empty;

    [Column("message_created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("message_read_at")]
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public virtual Process Process { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
    public virtual User Recipient { get; set; } = null!;
}
