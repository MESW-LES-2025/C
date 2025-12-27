using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consilium.Domain.Models
{
    [Table("process", Schema = "legal")]
    public class Process
    {
        [Column("process_id")]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Column("process_name")]
        [StringLength(255)]
        [Required]
        public string Name { get; set; } = null!;

        [Column("process_number")]
        [StringLength(255)]
        [Required]
        public string Number { get; set; } = null!;

        [Column("client_id")]
        [Required]
        public Guid ClientId { get; set; }

        [Column("lawyer_id")]
        public Guid? LawyerId { get; set; }

        [Column("process_adverse_part_name")]
        [StringLength(255)]
        public string? AdversePartName { get; set; }

        [Column("process_opposing_counsel_name")]
        [StringLength(255)]
        public string? OpposingCounselName { get; set; }


        [Column("process_created_at")]
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("process_closed_at")]
        public DateTime? ClosedAt { get; set; }

        [Column("process_dsc")]
        public string? Description { get; set; }

        [Column("process_next_hearing_date")]
        public DateTime? NextHearingDate { get; set; }

        [Column("process_priority")]
        [Required]
        public short Priority { get; set; }

        [Column("process_court_info")]
        [Required]
        public string CourtInfo { get; set; } = null!;

        [Column("process_type_phase_id")]
        [Required]
        public int ProcessTypePhaseId { get; set; }

        [Column("process_status_id")]
        [Required]
        public int ProcessStatusId { get; set; }

        // Mapeia ProcessTypePhaseId (FK para LEGAL.PROCESS_TYPE_PHASE)
        public ProcessTypePhase? ProcessTypePhase { get; set; }

        // Mapeia ProcessStatusId (FK para LEGAL.PROCESS_STATUS)
        public ProcessStatus Status { get; set; } = null!;

        // Mapeia ClientId (FK para CORE.CLIENT)
        public Client Client { get; set; } = null!;

        // Mapeia LawyerId (FK para LEGAL.LAWYER)
        public Lawyer? Lawyer { get; set; }

        // Mapeia CreatedBy (FK para CORE.ADMIN)
        // Usando 'CreatedByAdmin' para evitar conflito com 'CreatedBy' (Guid)
        // Related documents for this process
        public List<Document> Documents { get; set; } = new List<Document>();
    }
}
