using Consilium.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Consilium.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /*************************************************************************
        ************************** CORE SCHEMA ENTITIES **************************
        *************************************************************************/
        public DbSet<ActionLogType> ActionLogTypes { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Lawyer> Lawyers { get; set; }
        public DbSet<Phone> Phones { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }

        /*************************************************************************
        ************************** END OF CORE SCHEMA ENTITIES *******************
        *************************************************************************/

        /*************************************************************************
        ************************** LEGAL SCHEMA ENTITIES **************************
        *************************************************************************/
        public DbSet<Document> Documents { get; set; }
        public DbSet<Process> Processes { get; set; }
        public DbSet<ProcessLog> ProcessLogs { get; set; }
        public DbSet<ProcessPhase> ProcessPhases { get; set; }
        public DbSet<ProcessStatus> ProcessStatuses { get; set; }
        public DbSet<ProcessType> ProcessTypes { get; set; }
        public DbSet<ProcessTypePhase> ProcessTypePhases { get; set; }

        /*************************************************************************
        ************************** END OF LEGAL SCHEMA ENTITIES *******************
        *************************************************************************/

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /*************************************************************************
            ************************** CORE SCHEMA ENTITIES **************************
            *************************************************************************/
            modelBuilder.HasDefaultSchema("core");

            // **************** ACTION_LOG_TYPE **************** //
            modelBuilder.Entity<ActionLogType>(entity =>
            {
                entity.ToTable("action_log_type", "core");

                // PK
                entity.HasKey(e => e.ID).HasName("pk_action_log_type");

                // COLUMNS
                entity.Property(e => e.ID)
                    .HasColumnName("action_log_type_id")
                    .HasColumnType("integer") // Omitível, mas explicitando o tipo INTEGER
                    .ValueGeneratedOnAdd(); // Necessário para refletir o DEFAULT nextval no banco de dados

                entity.Property(e => e.Name)
                    .HasColumnName("action_log_type_name")
                    .HasColumnType("character varying(100)")
                    .HasMaxLength(100)
                    .IsRequired();

                // UK
                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("uk_action_log_type_01");
            });

            // **************** CLIENT **************** //
            modelBuilder.Entity<Client>()
                .HasKey(c => c.ID)
                // PK
                .HasName("PK_CLIENT");

            modelBuilder.Entity<Client>()
                .Property(c => c.ID)
                .HasColumnName("client_id");

            modelBuilder.Entity<Client>()
                .Property(c => c.Address)
                .HasColumnName("client_address")
                .HasMaxLength(500)
                .IsRequired();

            // FK
            modelBuilder.Entity<Client>()
                .HasOne(c => c.User)
                .WithOne(u => u.Client)
                .HasForeignKey<Client>(c => c.ID)
                .HasConstraintName("fk_client_user_01")
                .OnDelete(DeleteBehavior.Cascade);

            // **************** LAWYER **************** //
            modelBuilder.Entity<Lawyer>()
                .HasKey(l => l.ID)
                // PK
                .HasName("pk_lawyer");

            modelBuilder.Entity<Lawyer>()
                .Property(l => l.ID)
                .HasColumnName("lawyer_id");

            modelBuilder.Entity<Lawyer>()
                .Property(l => l.ProfessionalRegister)
                .HasColumnName("lawyer_professional_register")
                .HasMaxLength(20)
                .IsRequired();

            // UK
            modelBuilder.Entity<Lawyer>()
                .HasAlternateKey(l => l.ProfessionalRegister)
                .HasName("uk_lawyer_01_register");

            // FK
            modelBuilder.Entity<Lawyer>()
                .HasOne(l => l.User)
                .WithOne(u => u.Lawyer)
                .HasForeignKey<Lawyer>(l => l.ID)
                .HasConstraintName("fk_lawyer_user_01")
                .OnDelete(DeleteBehavior.Cascade);

            // **************** PHONE **************** //
            modelBuilder.Entity<Phone>()
                .HasKey(p => p.ID)
                // PK
                .HasName("pk_phone");

            modelBuilder.Entity<Phone>()
                .Property(p => p.ID)
                .HasColumnName("phone_id");

            modelBuilder.Entity<Phone>()
                .Property(p => p.UserID)
                .HasColumnName("fk_user_id")
                .IsRequired();

            modelBuilder.Entity<Phone>()
                .Property(p => p.CountryCode)
                .HasColumnName("phone_country_code")
                .HasDefaultValue((short)351)
                .IsRequired();

            modelBuilder.Entity<Phone>()
                .Property(p => p.Number)
                .HasColumnName("phone_number")
                .HasMaxLength(20)
                .IsRequired();

            modelBuilder.Entity<Phone>()
                .Property(p => p.IsMain)
                .HasColumnName("phone_is_main")
                .HasDefaultValue(true)
                .IsRequired();

            // UK
            modelBuilder.Entity<Phone>()
                .HasIndex(p => new { p.UserID, p.CountryCode, p.Number })
                .IsUnique()
                .HasDatabaseName("uk_phone_01_user_number");

            // FK
            modelBuilder.Entity<Phone>()
                .HasOne(p => p.User)
                .WithMany(u => u.Phones)
                .HasForeignKey(p => p.UserID)
                .HasConstraintName("fk_phone_user_01")
                .OnDelete(DeleteBehavior.Restrict);

            // **************** USER **************** //
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("user", "core");

                // PK
                entity.HasKey(u => u.ID).HasName("pk_user");

                // COLUMNS
                entity.Property(u => u.ID)
                    .HasColumnName("user_id")
                    .HasColumnType("uuid");

                entity.Property(u => u.Name)
                    .HasColumnName("user_name")
                    .HasColumnType("character varying(254)")
                    .HasMaxLength(254)
                    .IsRequired();

                entity.Property(u => u.NIF)
                    .HasColumnName("user_nif")
                    .HasColumnType("char(9)")
                    .HasMaxLength(9)
                    .IsRequired();

                entity.Property(u => u.Email)
                    .HasColumnName("user_email")
                    .HasColumnType("character varying(254)")
                    .HasMaxLength(254)
                    .IsRequired();

                entity.Property(u => u.PasswordHash)
                    .HasColumnName("user_password_hash")
                    .HasColumnType("text")
                    .IsRequired();

                entity.Property(u => u.IsActive)
                    .HasColumnName("user_is_active")
                    .HasColumnType("boolean")
                    .HasDefaultValue(true)
                    .IsRequired();

                entity.Property(u => u.CreatedAt)
                    .HasColumnName("user_created_at")
                    .HasColumnType("timestamp with time zone")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .IsRequired();

                // UK
                entity.HasIndex(u => u.NIF)
                    .IsUnique()
                    .HasDatabaseName("uk_user_01_nif");

                entity.HasIndex(u => u.Email)
                    .IsUnique()
                    .HasDatabaseName("uk_user_02_email");
            });


            /*************************************************************************
            ********************** END OF CORE SCHEMA ENTITIES ***********************
            *************************************************************************/

            /*************************************************************************
            ************************* LEGAL SCHEMA ENTITIES *************************
            *************************************************************************/

            // **************** DOCUMENT **************** //
            modelBuilder.Entity<Document>()
                .HasKey(d => d.Id)
                // PK
                .HasName("pk_document");

            modelBuilder.Entity<Document>()
                .Property(d => d.Id)
                .HasColumnName("document_id");

            modelBuilder.Entity<Document>()
                .Property(d => d.ProcessId)
                .HasColumnName("process_id")
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.File)
                .HasColumnName("file")
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.FileMimeType)
                .HasColumnName("file_mimetype")
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.FileSize)
                .HasColumnName("file_size")
                .IsRequired();

            modelBuilder.Entity<Document>()
                .Property(d => d.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            // FK
            modelBuilder.Entity<Document>()
                .HasOne(d => d.Process)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProcessId)
                .HasConstraintName("fk_document_process_01")
                .OnDelete(DeleteBehavior.Cascade);

            // **************** PROCESS **************** //
            modelBuilder.Entity<Process>()
                .HasKey(p => p.Id)
                // PK
                .HasName("pk_process");

            modelBuilder.Entity<Process>()
                .Property(p => p.Id)
                .HasColumnName("process_id");

            modelBuilder.Entity<Process>()
                .Property(p => p.Name)
                .HasColumnName("process_name")
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.Number)
                .HasColumnName("process_number")
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.ClientId)
                .HasColumnName("client_id")
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.LawyerId)
                .HasColumnName("lawyer_id")
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.AdversePartName)
                .HasColumnName("process_adverse_part_name")
                .HasMaxLength(255);

            modelBuilder.Entity<Process>()
                .Property(p => p.OpposingCounselName)
                .HasColumnName("process_opposing_counsel_name")
                .HasMaxLength(255);

            modelBuilder.Entity<Process>()
                .Property(p => p.CreatedAt)
                .HasColumnName("process_created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd()
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.CreatedAt)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            modelBuilder.Entity<Process>()
                .Property(p => p.ClosedAt)
                .HasColumnName("process_closed_at");

            modelBuilder.Entity<Process>()
                .Property(p => p.Description)
                .HasColumnName("process_dsc");

            modelBuilder.Entity<Process>()
                .Property(p => p.NextHearingDate)
                .HasColumnName("process_next_hearing_date");

            modelBuilder.Entity<Process>()
                .Property(p => p.Priority)
                .HasColumnName("process_priority")
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.CourtInfo)
                .HasColumnName("process_court_info")
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.ProcessTypePhaseId)
                .HasColumnName("process_type_phase_id")
                .IsRequired();

            modelBuilder.Entity<Process>()
                .Property(p => p.ProcessStatusId)
                .HasColumnName("process_status_id")
                .IsRequired();

            // UK
            modelBuilder.Entity<Process>()
                .HasIndex(p => new { p.Number, p.ClientId, p.LawyerId })
                .IsUnique()
                .HasDatabaseName("uk_process_01");

            // FK
            modelBuilder.Entity<Process>()
                .HasOne(p => p.Client)
                .WithMany()
                .HasForeignKey(p => p.ClientId)
                .HasConstraintName("fk_process_client_01")
                .OnDelete(DeleteBehavior.Restrict);

            // FK
            modelBuilder.Entity<Process>()
                .HasOne(p => p.Lawyer)
                .WithMany()
                .HasForeignKey(p => p.LawyerId)
                .HasConstraintName("fk_process_lawyer_02")
                .OnDelete(DeleteBehavior.Restrict);

            // FK
            modelBuilder.Entity<Process>()
                .HasOne(p => p.Status)
                .WithMany()
                .HasForeignKey(p => p.ProcessStatusId)
                .HasConstraintName("fk_process_status_03")
                .OnDelete(DeleteBehavior.Restrict);

            // FK
            modelBuilder.Entity<Process>()
                .HasOne(p => p.ProcessTypePhase)
                .WithMany()
                .HasForeignKey(p => p.ProcessTypePhaseId)
                .HasConstraintName("fk_process_type_phase_04")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
