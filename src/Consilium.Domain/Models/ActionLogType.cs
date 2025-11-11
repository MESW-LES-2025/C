using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consilium.Domain.Models;

/// <summary>
/// Catalog of action types for audit logging
/// </summary>
[Table("action_log_type", Schema = "core")]
public class ActionLogType
{
    [Key]
    [Column("action_log_type_id")]
    public Guid ID { get; set; }

    [Column("action_log_type_name")]
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<UserLog> UserLogs { get; set; } = new List<UserLog>();
    public ICollection<ProcessLog> ProcessLogs { get; set; } = new List<ProcessLog>();
}
