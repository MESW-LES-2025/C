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
        [Required]
        public Guid ProcessID { get; set; }

        [Column("updated_by")]
        [Required]
        public Guid UpdatedByID { get; set; }


        // Database-generated timestamp. The private set prevents manual assignment in C# code.
        [Column("updated_at")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime UpdatedAt { get; private set; }

        [Column("old_value", TypeName = "jsonb")]
        [Required]
        public JsonElement OldValue { get; set; }

        [Column("new_value", TypeName = "jsonb")]
        [Required]
        public JsonElement NewValue { get; set; }

        [Column("action_log_type_id")]
        [Required]
        public int ActionLogTypeID { get; set; }

        #region Navigation Properties
        [ForeignKey("ProcessID")]
        public virtual Process Process { get; set; } = null!;

        [ForeignKey("UpdatedByID")]
        public virtual User UpdatedBy { get; set; } = null!;

        [ForeignKey("ActionLogTypeID")]
        public virtual ActionLogType ActionLogType { get; set; } = null!;
        #endregion
    }
}