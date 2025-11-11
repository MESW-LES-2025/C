using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Consilium.Domain.Models;

/// <summary>
/// Audit log for User entity changes
/// </summary>
[Table("user_log", Schema = "core")]
public class UserLog
{
    [Key]
    [Column("user_log_id")]
    public Guid ID { get; set; }

    [Column("affected_user_id")]
    public Guid? AffectedUserID { get; set; }

    [Column("updated_by_id")]
    public Guid? UpdatedByID { get; set; }

    [Column("action_log_type_id")]
    public Guid ActionLogTypeID { get; set; }

    [Column("user_log_old_value")]
    public JsonElement? OldValue { get; set; }

    [Column("user_log_new_value")]
    public JsonElement? NewValue { get; set; }

    [Column("user_log_updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("AffectedUserID")]
    public User? AffectedUser { get; set; }

    [ForeignKey("UpdatedByID")]
    public User? UpdatedBy { get; set; }

    [ForeignKey("ActionLogTypeID")]
    public ActionLogType? ActionLogType { get; set; }
}
