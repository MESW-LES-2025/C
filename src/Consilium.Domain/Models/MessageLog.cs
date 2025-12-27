using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Consilium.Domain.Models
{
    [Table("message_log", Schema = "communication")]
    public class MessageLog
    {
        #region Table Columns
        // Database columns mapping exactly to the communication.message_log table.

        [Key]
        [Column("message_log_id")]
        public long ID { get; set; } // BIGSERIAL maps to long

        [Column("message_id")]
        public int? MessageID { get; set; } // Nullable due to ON DELETE SET NULL

        [Column("updated_by_id")]
        [Required]
        public Guid UpdatedByID { get; set; }

        [Column("action_log_type_id")]
        [Required]
        public int ActionLogTypeID { get; set; }

        [Column("updated_at")]
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("new_value", TypeName = "jsonb")]
        [Required]
        public JsonElement NewValue { get; set; }

        [Column("old_value", TypeName = "jsonb")]
        [Required]
        public JsonElement OldValue { get; set; }
        #endregion

        #region Navigation Properties
        // Object-Relational Mapping (ORM) navigation properties.
        // Enables traversal between the Communication Log and its related domain entities.

        [ForeignKey("MessageID")]
        public virtual Message? Message { get; set; }

        [ForeignKey("UpdatedByID")]
        public virtual User UpdatedBy { get; set; } = null!;

        [ForeignKey("ActionLogTypeID")]
        public virtual ActionLogType ActionLogType { get; set; } = null!;
        #endregion
    }
}