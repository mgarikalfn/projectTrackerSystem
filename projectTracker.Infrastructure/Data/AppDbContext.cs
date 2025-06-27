using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
        public DbSet<Board> Boards { get; set; }
        public DbSet<Sprint> Sprints { get; set; }

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

            modelBuilder.Entity<Project>().OwnsOne(p => p.Owner);
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

            // AppUser Configuration:
            modelBuilder.Entity<AppUser>(entity =>
            {
                // AccountId: No longer a principal key. It's just a regular column.
                // Make it nullable in DB, and set length. No unique index needed for FK.
                entity.Property(u => u.AccountId)
                      .IsRequired(false) // Allow NULL in DB
                      .HasMaxLength(450);

                // Add unique index on AccountId if you still want to enforce uniqueness
                // for lookups (e.g., preventing duplicate Jira Account IDs).
                // If you *do* want it unique for non-nulls:
                entity.HasIndex(u => u.AccountId)
                      .IsUnique()
                      .HasFilter("[AccountId] IS NOT NULL"); // Allows multiple NULLs, unique for non-NULLs

                // Ensure other AppUser properties' nullability/length matches entity
                entity.Property(u => u.DisplayName).IsRequired(); // Required in AppUser.cs
                entity.Property(u => u.AvatarUrl).IsRequired(false); // Nullable in AppUser.cs
                entity.Property(u => u.TimeZone).IsRequired(false); // Nullable in AppUser.cs
                entity.Property(u => u.CurrentWorkload).IsRequired(false); // Nullable in AppUser.cs
                entity.Property(u => u.Location).IsRequired(false); // Nullable in AppUser.cs

                // HasMany for AssignedTasks: FK now points to AppUser.Id (PK)
                entity.HasMany(u => u.AssignedTasks)
                      .WithOne(pt => pt.Assignee)
                      .HasForeignKey(pt => pt.AssigneeId)
                      .HasPrincipalKey(u => u.Id) // <--- CRITICAL CHANGE: Now points to AppUser.Id (PK)
                      .IsRequired(false)          // Tasks can be unassigned
                      .OnDelete(DeleteBehavior.SetNull); // Tasks become unassigned if user deleted
            });
            // ProjectTask Configuration:
            modelBuilder.Entity<ProjectTask>(entity =>
            {
                entity.HasKey(t => t.Id);




                // Project relationship (unchanged)
                entity.HasOne(t => t.Project)
                      .WithMany(p => p.Tasks)
                      .HasForeignKey(t => t.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(t => t.Status)
                   .HasConversion<string>()
                   .HasColumnType("nvarchar(50)");


            });



        }
    }
}
        