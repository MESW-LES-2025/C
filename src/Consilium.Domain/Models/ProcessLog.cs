using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Consilium.Domain.Models
{
    [Table("process_log", Schema = "legal")]
    public class ProcessLog
    {
        [Key]
        [Column("process_log_id")]
        public Guid ID { get; set; }

        [Column("process_id")]
        public Guid ProcessID { get; set; }

        [Column("updated_by_id")]
        public Guid? UpdatedByID { get; set; }

    [Column("action_log_type_id")]
    public int ActionLogTypeID { get; set; }

        [Column("process_log_old_value")]
        public JsonElement? OldValue { get; set; }

        [Column("process_log_new_value")]
        public JsonElement? NewValue { get; set; }

        [Column("process_log_updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ActionLogType? ActionLogType { get; set; }
    }
}