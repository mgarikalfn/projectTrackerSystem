using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Aggregates;
using ProjectTracker.Infrastructure.Data;
using projectTracker.Infrastructure.Risk.Evaluators;
using projectTracker.Domain.ValueObjects;
using projectTracker.Infrastructure.Risk;
using projectTracker.Domain.Entities;

namespace projectTracker.Infrastructure.Sync
{

    public class SyncManager : ISyncManager
    {
        private readonly AppDbContext _dbContext;
        private readonly IProjectManegementAdapter _adapter;
        private readonly ILogger<SyncManager> _logger;
        private readonly IRiskCalculatorService _riskCalculator;

        public SyncManager(
            AppDbContext dbContext,
            IProjectManegementAdapter adapter,
            ILogger<SyncManager> logger,
            IRiskCalculatorService riskCalculator)
        {
            _dbContext = dbContext;
            _adapter = adapter;
            _logger = logger;
            _riskCalculator = riskCalculator;
        }

        public async Task SyncAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting Jira sync...");

            try
            {
                // Sync users first (they might be referenced by projects)
                await SyncUsersAsync(ct);

                // Then sync projects
                await SyncProjectsAsync(ct);

                _logger.LogInformation("Sync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete sync");
                throw;
            }
        }

        private async Task SyncUsersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting user sync...");

            try
            {
                var jiraUsers = await _adapter.GetAppUsersAsync(ct);
                _logger.LogDebug("Retrieved {UserCount} users from Jira", jiraUsers.Count);

                foreach (var jiraUser in jiraUsers)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(jiraUser.Email))
                        {
                            _logger.LogWarning("Skipping user {AccountId} with empty email", jiraUser.AccountId);
                            continue;
                        }

                        // Find existing user by email or Jira account ID
                        var user = await _dbContext.Users
                            .FirstOrDefaultAsync(u =>
                                u.Email == jiraUser.Email ||
                                u.AccountId == jiraUser.AccountId, ct);

                        if (user == null)
                        {
                            // Create new user
                            user = new AppUser
                            {
                                UserName = jiraUser.Email,
                                Email = jiraUser.Email,
                                EmailConfirmed = true,
                                AccountId = jiraUser.AccountId,
                                DisplayName = jiraUser.DisplayName,
                                AvatarUrl = jiraUser.AvatarUrl,
                                IsActive = jiraUser.Active
                            };

                            _dbContext.Users.Add(user);
                            _logger.LogDebug("Added new user {Email}", jiraUser.Email);
                        }
                        else
                        {
                            // Update existing user
                            user.AccountId = jiraUser.AccountId;
                            user.DisplayName = jiraUser.DisplayName;
                            user.AvatarUrl = jiraUser.AvatarUrl;
                            user.IsActive = jiraUser.Active;

                            _logger.LogDebug("Updated existing user {Email}", jiraUser.Email);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing user {AccountId}", jiraUser.AccountId);
                    }
                }

                var userChanges = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("User sync completed. {ChangesCount} changes saved", userChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete user sync");
                throw;
            }
        }

        private async Task SyncProjectsAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting project sync...");

            try
            {
                var projects = await _adapter.GetProjectsAsync(ct);
                _logger.LogDebug("Retrieved {ProjectCount} projects from Jira", projects.Count);

                foreach (var projectDto in projects)
                {
                    try
                    {
                        // Find or create project
                        var project = await _dbContext.Projects
                            .FirstOrDefaultAsync(p => p.Key == projectDto.Key, ct)
                            ?? Project.Create(
                                Id: Guid.NewGuid().ToString(),
                                key: projectDto.Key,
                                name: projectDto.Name,
                                Lead: projectDto.LeadName,
                                Description: projectDto.Description);

                        // Get and update metrics
                        var metricsDto = await _adapter.GetProjectMetricsAsync(projectDto.Key, ct);
                        var metrics = new ProgressMetrics(
                            totalTasks: metricsDto.OpenIssues,
                            completedTasks: metricsDto.CompletedTasks,
                            storyPointsCompleted: metricsDto.CompletedStoryPoints,
                            storyPointsTotal: metricsDto.TotalStoryPoints,
                            activeBlockers: metricsDto.ActiveBlockers,
                            recentUpdates: metricsDto.RecentUpdates);

                        project.UpdateProgressMetrics(metrics);

                        // Calculate and update health
                        var health = _riskCalculator.Calculate(metricsDto);
                        project.UpdateHealthMetrics(health);

                        // Update database
                        if (_dbContext.Entry(project).State == EntityState.Detached)
                        {
                            _dbContext.Projects.Add(project);
                            _logger.LogDebug("Added new project {ProjectKey}", project.Key);
                        }
                        else
                        {
                            _logger.LogDebug("Updated existing project {ProjectKey}", project.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing project {ProjectKey}", projectDto.Key);
                    }
                }

                var projectChanges = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Project sync completed. {ChangesCount} changes saved", projectChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete project sync");
                throw;
            }
        }
    }
}
