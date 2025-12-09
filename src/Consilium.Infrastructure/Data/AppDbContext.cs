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

        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Lawyer> Lawyers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Phone> Phones { get; set; }
        public DbSet<Process> Processes { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<ProcessType> ProcessTypes { get; set; }
        public DbSet<ProcessPhase> ProcessPhases { get; set; }
        public DbSet<ProcessTypePhase> ProcessTypePhases { get; set; }
        public DbSet<ProcessStatus> ProcessStatuses { get; set; }
        public DbSet<ActionLogType> ActionLogTypes { get; set; }
        public DbSet<UserLog> UserLogs { get; set; }
        public DbSet<ProcessLog> ProcessLogs { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Set default schema to core (lowercase)
            modelBuilder.HasDefaultSchema("core");

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasKey(u => u.ID);

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            // Configure Client entity with 1:1 relationship to User
            modelBuilder.Entity<Client>()
                .HasKey(c => c.ID);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.User)
                .WithOne(u => u.Client)
                .HasForeignKey<Client>(c => c.ID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Lawyer entity with 1:1 relationship to User
            modelBuilder.Entity<Lawyer>()
                .HasKey(l => l.ID);

            modelBuilder.Entity<Lawyer>()
                .HasOne(l => l.User)
                .WithOne(u => u.Lawyer)
                .HasForeignKey<Lawyer>(l => l.ID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Admin entity with 1:1 relationship to User
            modelBuilder.Entity<Admin>()
                .HasKey(a => a.ID);

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.ID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Admin>()
                .Property(a => a.StartedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configure Phone entity with 1:N relationship to User
            modelBuilder.Entity<Phone>()
                .HasKey(p => p.ID);

            modelBuilder.Entity<Phone>()
                .HasOne(p => p.User)
                .WithMany(u => u.Phones)
                .HasForeignKey(p => p.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Phone>()
                .Property(p => p.CountryCode)
                .HasDefaultValue((short)351);

            // Configure Document entity in LEGAL schema and cascade delete when a Process is deleted
            modelBuilder.Entity<Document>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Process)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Process entity - CreatedAt should never be updated after creation
            modelBuilder.Entity<Process>()
                .Property(p => p.CreatedAt)
                .ValueGeneratedOnAdd();
            
            modelBuilder.Entity<Process>()
                .Property(p => p.CreatedAt)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            // Configure Message entity
            modelBuilder.Entity<Message>()
                .HasKey(m => m.Id);
            
            modelBuilder.Entity<Message>()
                .Property(m => m.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Process)
                .WithMany() // Assuming Process doesn't need a collection of Messages for now, or we can add it later if needed
                .HasForeignKey(m => m.ProcessId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
