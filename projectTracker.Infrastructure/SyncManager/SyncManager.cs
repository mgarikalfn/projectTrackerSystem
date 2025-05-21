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

namespace projectTracker.Infrastructure.SyncManager
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
            _logger.LogInformation("Starting Jira project sync...");

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
                        // Continue with next project even if one fails
                    }
                }

                var changes = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Sync completed. {ChangesCount} changes saved to database", changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete project sync");
                throw; // Re-throw to let caller handle
            }
        }
    }
}
