using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consilium.Domain.Models
{
    [Table("action_log_type", Schema = "core")]
    public class ActionLogType
    {
        [Key]
        [Column("action_log_type_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Column("action_log_type_name")]
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Navigation
        public ICollection<UserLog> UserLogs { get; set; } = new List<UserLog>();
        public ICollection<ProcessLog> ProcessLogs { get; set; } = new List<ProcessLog>();
    }
}