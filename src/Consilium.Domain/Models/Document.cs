using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consilium.Domain.Models
{
    [Table("document", Schema = "legal")]
    public class Document
    {
        #region Table Columns
        // Database columns mapping exactly to the legal.document table.

        [Key]
        [Column("document_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("process_id")]
        [Required]
        public Guid ProcessId { get; set; }

        [Column("file_name")]
        [StringLength(100)]
        [Required]
        public string FileName { get; set; } = null!;

        [Column("file")]
        [Required]
        public byte[] File { get; set; } = null!;

        [Column("file_mimetype")]
        [StringLength(50)]
        [Required]
        public string FileMimeType { get; set; } = null!;

        [Column("file_size")]
        [Required]
        public long FileSize { get; set; }

        [Column("created_at")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        #endregion

        #region Navigation Properties
        // Object-Relational Mapping (ORM) navigation properties. Links the document to its parent process and enables eager loading.

        [ForeignKey("ProcessId")]
        public virtual Process Process { get; set; } = null!;
        #endregion
    }
}