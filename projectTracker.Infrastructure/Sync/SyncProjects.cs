using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.ValueObjects;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Sync
{
    public class SyncProjects
    {
        private readonly AppDbContext _dbContext;
        private readonly IProjectManegementAdapter _adapter;
        private readonly IRiskCalculatorService _riskCalculator;
        private readonly ILogger<SyncProjects> _logger;

        public SyncProjects(
            AppDbContext dbContext,
            IProjectManegementAdapter adapter,
            IRiskCalculatorService riskCalculator,
            ILogger<SyncProjects> logger)
        {
            _dbContext = dbContext;
            _adapter = adapter;
            _riskCalculator = riskCalculator;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting project sync...");
            try
            {
                var jiraProjects = await _adapter.GetProjectsAsync(ct);
                _logger.LogDebug("Retrieved {ProjectCount} projects from Jira", jiraProjects.Count);

                if (!jiraProjects.Any())
                {
                    _logger.LogInformation("No projects found from Jira. Skipping project sync.");
                    return;
                }

                foreach (var projectDto in jiraProjects)
                {
                    try
                    {
                        var project = await _dbContext.Projects
                            .FirstOrDefaultAsync(p => p.Key == projectDto.Key, ct);

                        if (project == null)
                        {
                            project = Project.Create(
                                Id: Guid.NewGuid().ToString(),
                                key: projectDto.Key,
                                name: projectDto.Name,
                                Lead: projectDto.LeadName,
                                Description: projectDto.Description
                            // The new strategic fields will be initialized to their defaults (e.g., NotStarted, null)
                            );
                            _dbContext.Projects.Add(project);
                            _logger.LogDebug("Added new project {ProjectKey}", project.Key);
                        }
                        else
                        {
                            // --- FIX APPLIED HERE: Call the new UpdateJiraSyncedDetails method ---
                            project.UpdateJiraSyncedDetails(
                                name: projectDto.Name,
                                description: projectDto.Description,
                                leadName: projectDto.LeadName
                            );
                            _logger.LogDebug("Updated existing project {ProjectKey} with Jira synced details", project.Key);
                        }

                        // Fetch and update project metrics/health (assuming your Project has these fields)
                        var metricsDto = await _adapter.GetProjectMetricsAsync(projectDto.Key, ct);
                        if (metricsDto != null)
                        {
                            var metrics = new ProgressMetrics(
                                totalTasks: metricsDto.TotalTasks,
                                completedTasks: metricsDto.CompletedTasks,
                                storyPointsCompleted: metricsDto.CompletedStoryPoints,
                                storyPointsTotal: metricsDto.TotalStoryPoints,
                                activeBlockers: metricsDto.ActiveBlockers,
                                recentUpdates: metricsDto.RecentUpdates);

                            project.UpdateProgressMetrics(metrics);

                            var health = _riskCalculator.Calculate(metricsDto); // Ensure health calculation uses metricsDto
                            project.UpdateHealthMetrics(health);
                            _logger.LogDebug("Updated metrics and health for project {ProjectKey}", project.Key);
                        }
                        else
                        {
                            _logger.LogWarning("No metrics found for project {ProjectKey}. Skipping metrics/health update.", project.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing project {ProjectKey} during sync. Skipping this project.", projectDto.Key);
                    }
                }

                var projectChanges = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Project sync completed. {ChangesCount} changes saved", projectChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete project sync.");
                throw; // Re-throw if it's a critical error that should stop the sync process
            }
        }
    }

}
