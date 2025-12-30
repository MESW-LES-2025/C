using Consilium.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Consilium.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    #region DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Lawyer> Lawyers { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Phone> Phones { get; set; }
    public DbSet<Process> Processes { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentLog> DocumentLogs { get; set; }
    public DbSet<ProcessType> ProcessTypes { get; set; }
    public DbSet<ProcessPhase> ProcessPhases { get; set; }
    public DbSet<ProcessTypePhase> ProcessTypePhases { get; set; }
    public DbSet<ProcessStatus> ProcessStatuses { get; set; }
    public DbSet<ActionLogType> ActionLogTypes { get; set; }
    public DbSet<UserLog> UserLogs { get; set; }
    public DbSet<ProcessLog> ProcessLogs { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageLog> MessageLogs { get; set; }
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        #region CORE Schema Configuration
        // Define the default schema for security and organizational purposes
        modelBuilder.HasDefaultSchema("core");

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("user", "core");
            entity.HasKey(u => u.ID);
            entity.Property(u => u.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("client", "core");
            entity.HasKey(c => c.ID);
            // One-to-One relationship sharing the Primary Key with the User table
            entity.HasOne(c => c.User).WithOne(u => u.Client).HasForeignKey<Client>(c => c.ID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lawyer>(entity =>
        {
            entity.ToTable("lawyer", "core");
            entity.HasKey(l => l.ID);
            // Lawyer entity acts as a specialized extension of the User entity
            entity.HasOne(l => l.User).WithOne(u => u.Lawyer).HasForeignKey<Lawyer>(l => l.ID).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Admin>(entity =>
        {
            entity.ToTable("admin", "core");
            entity.HasKey(a => a.ID);
            entity.HasOne(a => a.User).WithOne(u => u.Admin).HasForeignKey<Admin>(a => a.ID).OnDelete(DeleteBehavior.Cascade);
            entity.Property(a => a.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Phone>(entity =>
        {
            entity.ToTable("phone", "core");
            entity.HasKey(p => p.ID);
            entity.HasOne(p => p.User).WithMany(u => u.Phones).HasForeignKey(p => p.UserID).OnDelete(DeleteBehavior.Cascade);
            entity.Property(p => p.CountryCode).HasDefaultValue((short)351); // Defaulting to Portugal
        });

        modelBuilder.Entity<ActionLogType>(entity =>
        {
            entity.ToTable("action_log_type", "core");
            entity.HasKey(alt => alt.ID);
        });

        modelBuilder.Entity<UserLog>(entity =>
        {
            entity.ToTable("user_log", "core");
            entity.HasKey(e => e.ID);
            // Storing audit data as JSONB for flexibility and better indexing in PG
            entity.Property(e => e.OldValue).HasColumnType("jsonb");
            entity.Property(e => e.NewValue).HasColumnType("jsonb");
            entity.HasOne(e => e.ActionLogType).WithMany(alt => alt.UserLogs).HasForeignKey(e => e.ActionLogTypeID).OnDelete(DeleteBehavior.Restrict);
        });
        #endregion

        #region LEGAL Schema Configuration
        // Legal schema handles business-specific processes and case management

        modelBuilder.Entity<Process>(entity =>
        {
            entity.ToTable("process", "legal");

            // Primary Key handled by PostgreSQL using pgcrypto gen_random_uuid()
            entity.HasKey(p => p.Id).HasName("pk_process");
            entity.Property(p => p.Id)
                  .HasColumnName("process_id")
                  .HasDefaultValueSql("gen_random_uuid()")
                  .ValueGeneratedOnAdd();

            // Mapping mandatory fields with explicit column naming
            entity.Property(p => p.Name).HasColumnName("process_name").IsRequired();
            entity.Property(p => p.Number).HasColumnName("process_number").IsRequired();
            entity.Property(p => p.ClientId).HasColumnName("client_id").IsRequired();
            entity.Property(p => p.LawyerId).HasColumnName("lawyer_id").IsRequired();
            entity.Property(p => p.CourtInfo).HasColumnName("process_court_info").IsRequired();
            entity.Property(p => p.Priority).HasColumnName("process_priority").IsRequired();

            // Mapping optional fields
            entity.Property(p => p.Description).HasColumnName("process_dsc");
            entity.Property(p => p.NextHearingDate).HasColumnName("process_next_hearing_date");
            entity.Property(p => p.ClosedAt).HasColumnName("process_closed_at");
            entity.Property(p => p.ProcessTypePhaseId).HasColumnName("process_type_phase_id").IsRequired();
            entity.Property(p => p.ProcessStatusId).HasColumnName("process_status_id").IsRequired();
            entity.Property(p => p.AdversePartName).HasColumnName("process_adverse_part_name");
            entity.Property(p => p.OpposingCounselName).HasColumnName("process_opposing_counsel_name");

            // Timestamp handling: DB provides the value and it cannot be modified after insertion
            entity.Property(p => p.CreatedAt)
                  .HasColumnName("process_created_at")
                  .HasDefaultValueSql("CURRENT_TIMESTAMP")
                  .ValueGeneratedOnAdd();

            entity.Property(p => p.CreatedAt)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        });

        modelBuilder.Entity<ProcessLog>(entity =>
        {
            entity.ToTable("process_log", "legal");
            entity.HasKey(e => e.ID).HasName("pk_process_log");
            entity.Property(e => e.ID).HasColumnName("process_log_id").ValueGeneratedOnAdd();
            entity.Property(e => e.ProcessID).HasColumnName("process_id");
            entity.Property(e => e.UpdatedByID).HasColumnName("updated_by"); // Auditing user reference
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.OldValue).HasColumnName("old_value").HasColumnType("jsonb");
            entity.Property(e => e.NewValue).HasColumnName("new_value").HasColumnType("jsonb");
            entity.Property(e => e.ActionLogTypeID).HasColumnName("action_log_type_id");

            entity.HasOne(e => e.Process).WithMany().HasForeignKey(e => e.ProcessID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UpdatedBy).WithMany().HasForeignKey(e => e.UpdatedByID).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("document", "legal");
            entity.HasKey(d => d.Id).HasName("pk_document");
            entity.Property(e => e.Id).HasColumnName("document_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ProcessId).HasColumnName("process_id");
            entity.Property(e => e.File).HasColumnName("file"); // Reference to binary or storage path

            entity.HasIndex(e => e.ProcessId).HasDatabaseName("idx_document_01");
            entity.HasOne(d => d.Process).WithMany(p => p.Documents).HasForeignKey(d => d.ProcessId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentLog>(entity =>
        {
            entity.ToTable("document_log", "legal");
            entity.HasKey(e => e.ID).HasName("pk_document_log");
            entity.Property(e => e.ID).HasColumnName("document_log_id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DocumentID).HasColumnName("document_id");
            entity.Property(e => e.UpdatedByID).HasColumnName("updated_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.OldValue).HasColumnName("old_value").HasColumnType("jsonb");
            entity.Property(e => e.NewValue).HasColumnName("new_value").HasColumnType("jsonb");

            entity.HasOne(e => e.Document).WithMany().HasForeignKey(e => e.DocumentID).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UpdatedBy).WithMany().HasForeignKey(e => e.UpdatedByID).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProcessType>(entity => entity.ToTable("process_type", "legal"));
        modelBuilder.Entity<ProcessPhase>(entity => entity.ToTable("process_phase", "legal"));
        modelBuilder.Entity<ProcessStatus>(entity => entity.ToTable("process_status", "legal"));
        modelBuilder.Entity<ProcessTypePhase>(entity => entity.ToTable("process_type_phase", "legal"));
        #endregion

        #region COMMUNICATION Schema Configuration
        // Handles interaction between users regarding specific legal processes

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("message", "communication");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(m => m.Process).WithMany().HasForeignKey(m => m.ProcessId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.Recipient).WithMany().HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MessageLog>(entity =>
        {
            entity.ToTable("message_log", "communication");
            entity.HasKey(e => e.ID).HasName("pk_message_log");
            entity.Property(e => e.ID).HasColumnName("message_log_id").ValueGeneratedOnAdd();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.NewValue).HasColumnType("jsonb");
            entity.Property(e => e.OldValue).HasColumnType("jsonb");

            entity.HasOne(e => e.Message).WithMany().HasForeignKey(e => e.MessageID).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.UpdatedBy).WithMany().HasForeignKey(e => e.UpdatedByID).OnDelete(DeleteBehavior.Restrict);
        });
        #endregion
    }
}