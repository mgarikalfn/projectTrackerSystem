using Microsoft.EntityFrameworkCore;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;


namespace ProjectTracker.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> Tasks { get; set; }
        public DbSet<SyncHistory> SyncHistory { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Project Aggregate Configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.Id);

                // Value Objects (Owned Entities)
                entity.OwnsOne(p => p.Health, h =>
                {
                    h.Property(h => h.Level)
                        .HasConversion(
                            v => v.ToString(),
                            v => (HealthLevel)Enum.Parse(typeof(HealthLevel), v))
                        .HasColumnName("HealthLevel");

                    h.Property(h => h.Reason)
                        .HasColumnName("HealthReason");
                });

                entity.OwnsOne(p => p.Progress, p =>
                {
                    p.Property(p => p.TotalTasks)
                        .HasColumnName("TotalTasks");

                    p.Property(p => p.CompletedTasks)
                        .HasColumnName("CompletedTasks");

                    p.Property(p => p.StoryPointsTotal)
                        .HasColumnName("StoryPointsTotal");

                    p.Property(p => p.StoryPointsCompleted)
                        .HasColumnName("StoryPointsCompleted");

                    p.Property(p => p.OnTrackPercentage)
                        .HasColumnName("ProgressPercentage");
                });

                // Indexes
                entity.HasIndex(p => p.Key).IsUnique();
                entity.Property(p => p.Key).IsRequired();
                entity.Property(p => p.Name).IsRequired();
            });

            // ProjectTask Entity Configuration
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.HasKey(t => t.Id);

                // Status Conversion
                entity.Property(t => t.Status)
                    .HasConversion(
                        v => v.ToString(),
                        v => (projectTracker.Domain.Enums.TaskStatus)Enum.Parse(typeof(projectTracker.Domain.Enums.TaskStatus), v))
                    .IsRequired();

                // Relationships
                entity.HasOne(t => t.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(t => t.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Value Constraints
                entity.Property(t => t.Key).IsRequired();
                entity.Property(t => t.Summary).IsRequired();
                entity.Property(t => t.Updated).IsRequired();

                // Indexes
                entity.HasIndex(t => t.Key).IsUnique();
                entity.HasIndex(t => t.ProjectId);
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.AssigneeId);
                entity.HasIndex(t => t.Updated);
            });

            // Additional Configurations
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<SyncHistory>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.SyncTime);
                entity.HasIndex(s => s.ProjectId);
            });


            modelBuilder.Entity<SyncHistory>(entity =>
            {
                entity.HasKey(s => s.Id);

                // Indexes for query performance
                entity.HasIndex(s => s.SyncTime);
                entity.HasIndex(s => s.ProjectId);
                entity.HasIndex(s => s.Status);
                entity.HasIndex(s => new { s.ProjectId, s.SyncTime });

                // Conversions
                entity.Property(s => s.Type)
                    .HasConversion<string>();

                entity.Property(s => s.Status)
                    .HasConversion<string>();

                // Relationships
                entity.HasOne(s => s.Project)
                    .WithMany()
                    .HasForeignKey(s => s.ProjectId);
            });
        }
    }
}