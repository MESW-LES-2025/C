using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Consilium.Domain.Models;

/// <summary>
/// Audit log for Process entity changes
/// </summary>
[Table("process_log", Schema = "legal")]
public class ProcessLog
{
    [Key]
    [Column("process_log_id")]
    public Guid ID { get; set; }

    [Column("process_id")]
    public Guid ProcessID { get; set; }

    [Column("process_log_updated_by")]
    public Guid? UpdatedByID { get; set; }

    [Column("action_log_type_id")]
    public Guid ActionLogTypeID { get; set; }

    [Column("process_log_old_value")]
    public JsonElement? OldValue { get; set; }

    [Column("process_log_new_value")]
    public JsonElement? NewValue { get; set; }

    [Column("process_log_updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UpdatedByID")]
    public User? UpdatedBy { get; set; }

    [ForeignKey("ActionLogTypeID")]
    public ActionLogType? ActionLogType { get; set; }
}
