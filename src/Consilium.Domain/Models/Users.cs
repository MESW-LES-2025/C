using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consilium.Domain.Models
{
    [Table("user", Schema = "core")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public Guid ID { get; set; }

        [Column("user_name")]
        [Required]
        [StringLength(254)]
        public string Name { get; set; } = null!;

        [Column("user_nif")]
        [Required]
        [StringLength(9)]
        public string NIF { get; set; } = null!;

        [Column("user_email")]
        [Required]
        [StringLength(254)]
        public string Email { get; set; } = null!;

        [Column("user_password_hash")]
        [Required]
        public string PasswordHash { get; set; } = null!;

        [Column("user_is_active")]
        [Required]
        public bool IsActive { get; set; } = true;


        // Navigation properties
        public Client? Client { get; set; }
        public Lawyer? Lawyer { get; set; }
        public Admin? Admin { get; set; }
        public ICollection<Phone> Phones { get; set; } = new List<Phone>();
    }
}