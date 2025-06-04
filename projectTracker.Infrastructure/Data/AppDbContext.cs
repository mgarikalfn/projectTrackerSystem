using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;

namespace ProjectTracker.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, UserRole, string,
                                              IdentityUserClaim<string>, UserRoleMapping,
                                              IdentityUserLogin<string>,
                                              IdentityRoleClaim<string>,
                                              IdentityUserToken<string>>
    {
        // Essential DbSets
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectTask> Tasks { get; set; }
        public DbSet<SyncHistory> SyncHistory { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Rename Identity tables
            modelBuilder.Entity<AppUser>().ToTable("Users");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<UserRoleMapping>().ToTable("UserRoleMappings");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");


            // Project Aggregate Configuration
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.Id);

                // Health Value Object Configuration
                entity.OwnsOne(p => p.Health, h =>
                {
                    h.Property(h => h.Level)
                        .HasConversion(
                            v => v.ToString(),
                            v => (HealthLevel)Enum.Parse(typeof(HealthLevel), v))
                        .HasColumnName("HealthLevel");

                    h.Property(h => h.Reason)
                        .HasColumnName("HealthReason");

                    h.Property(h => h.Score)
                        .HasColumnName("HealthScore");

                    h.Property(h => h.Confidence)
                        .HasColumnName("HealthConfidence");
                });

                // Progress Value Object Configuration
                entity.OwnsOne(p => p.Progress, p =>
                {
                    p.Property(p => p.TotalTasks)
                        .HasColumnName("TotalTasks")
                        .IsRequired();

                    p.Property(p => p.CompletedTasks)
                        .HasColumnName("CompletedTasks")
                        .IsRequired();

                    p.Property(p => p.StoryPointsTotal)
                        .HasColumnName("StoryPointsTotal")
                        .IsRequired();

                    p.Property(p => p.StoryPointsCompleted)
                        .HasColumnName("StoryPointsCompleted")
                        .IsRequired();

                    p.Property(p => p.ActiveBlockers)
                        .HasColumnName("ActiveBlockers")
                        .IsRequired();

                    p.Property(p => p.RecentUpdates)
                        .HasColumnName("RecentUpdates")
                        .IsRequired();
                });

                // Core Project properties
                entity.Property(p => p.Key)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(p => p.Description)
                    .HasMaxLength(500);

                entity.Property(p => p.Lead)
                    .HasMaxLength(100);

                // Indexes
                entity.HasIndex(p => p.Key)
                    .IsUnique();
            });


            // Configure RolePermission with length limits
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.Property(p => p.Id)
                    .HasMaxLength(450); // Max length for SQL Server index compatibility
            });

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);
        }
    }
}