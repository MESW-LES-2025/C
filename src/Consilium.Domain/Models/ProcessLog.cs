using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Consilium.Domain.Models
{
    [Table("process_log", Schema = "legal")]
    public class ProcessLog
    {
        #region Table Columns
        // Database columns mapping exactly to the legal.process_log table.

        [Key]
        [Column("process_log_id")]
        public Guid ID { get; set; }

        [Column("process_id")]
        [Required]
        public Guid ProcessID { get; set; }

        [Column("updated_by")]
        [Required]
        public Guid UpdatedByID { get; set; }

        [Column("updated_at")]
        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("old_value", TypeName = "jsonb")]
        [Required]
        public JsonElement OldValue { get; set; }

        [Column("new_value", TypeName = "jsonb")]
        [Required]
        public JsonElement NewValue { get; set; }

        [Column("action_log_type_id")]
        [Required]
        public int ActionLogTypeID { get; set; }
        #endregion

        #region Navigation Properties
        // Object-Relational Mapping (ORM) navigation properties.
        // Enables traversal between the Audit Log and its related domain entities.

        [ForeignKey("ProcessID")]
        public virtual Process Process { get; set; } = null!;

        [ForeignKey("UpdatedByID")]
        public virtual User UpdatedBy { get; set; } = null!;

        [ForeignKey("ActionLogTypeID")]
        public virtual ActionLogType ActionLogType { get; set; } = null!;
        #endregion
    }
}