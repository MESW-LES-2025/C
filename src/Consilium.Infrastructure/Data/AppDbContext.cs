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
            // All tables listed here belong to the CORE schema.
            // - ACTION_LOG_TYPE
            // - ADMIN
            // - CLIENT
            // - LAWYER
            // - PHONE
            // - USER
            // - USER_LOG

            modelBuilder.HasDefaultSchema("core");

            // ADMIN
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

            // CLIENT
            modelBuilder.Entity<Client>()
                .HasKey(c => c.ID);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.User)
                .WithOne(u => u.Client)
                .HasForeignKey<Client>(c => c.ID)
                .OnDelete(DeleteBehavior.Cascade);

            // LAWYER
            modelBuilder.Entity<Lawyer>()
                .HasKey(l => l.ID);

            modelBuilder.Entity<Lawyer>()
                .HasOne(l => l.User)
                .WithOne(u => u.Lawyer)
                .HasForeignKey<Lawyer>(l => l.ID)
                .OnDelete(DeleteBehavior.Cascade);

            // PHONE
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

            // USER
            modelBuilder.Entity<User>()
                .HasKey(u => u.ID);

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);


            /*************************************************************************
            ********************** END OF CORE SCHEMA ENTITIES ***********************
            *************************************************************************/

            /*************************************************************************
            ************************* LEGAL SCHEMA ENTITIES *************************
            *************************************************************************/

            // DOCUMENT
            modelBuilder.Entity<Document>()
                .HasKey(d => d.Id);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Process)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProcessId)
                .OnDelete(DeleteBehavior.Cascade);

            // PROCESS
            modelBuilder.Entity<Process>()
                .Property(p => p.CreatedAt)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Process>()
                .Property(p => p.CreatedAt)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        }
    }
}
