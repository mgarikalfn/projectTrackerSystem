using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Infrastructure.BackgroundService
{
    using System.Net.Http.Headers;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using projectTracker.Domain.Aggregates;
    using projectTracker.Domain.Entities;
    using ProjectTracker.Infrastructure.Data;

    public class JiraSyncService : BackgroundService
    {
        private readonly ILogger<JiraSyncService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly JiraSettings _jiraSettings;
        private readonly HttpClient _httpClient;
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(15);

        public JiraSyncService(
            ILogger<JiraSyncService> logger,
            IServiceProvider serviceProvider,
            IOptions<JiraSettings> jiraSettings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _jiraSettings = jiraSettings.Value;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes(
                            $"{_jiraSettings.Username}:{_jiraSettings.ApiToken}")));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Jira Sync Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    await SyncProjects(dbContext, stoppingToken);
                    await SyncRecentTasks(dbContext, stoppingToken);

                    _logger.LogInformation("Sync completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Jira sync");
                }

                await Task.Delay(_syncInterval, stoppingToken);
            }
        }

        private async Task SyncProjects(AppDbContext dbContext, CancellationToken ct)
        {
            _logger.LogInformation("Syncing projects...");

            // Get projects from Jira
            var response = await _httpClient.GetAsync(
                $"{_jiraSettings.BaseUrl}/rest/api/3/project?expand=lead,description", ct);

            response.EnsureSuccessStatusCode();

            var jiraProjects = await response.Content.ReadFromJsonAsync<List<JiraProjectDto>>(cancellationToken: ct);

            foreach (var jiraProject in jiraProjects)
            {
                var project = await dbContext.Projects
                    .FirstOrDefaultAsync(p => p.Key == jiraProject.Key, ct)
                    ?? new Project(jiraProject.Key, jiraProject.Name);

                // Update core properties
                project.UpdateDetails(
                    jiraProject.Name,
                    jiraProject.Description?.Content ?? string.Empty,
                    jiraProject.Lead?.DisplayName);

                // Calculate and update metrics
                var metrics = await CalculateProjectMetrics(jiraProject.Key, ct);
                project.UpdateMetrics(metrics);

                // Upsert project
                if (dbContext.Entry(project).State == EntityState.Detached)
                    dbContext.Projects.Add(project);

                await dbContext.SaveChangesAsync(ct);
            }
        }

        private async Task SyncRecentTasks(AppDbContext dbContext, CancellationToken ct)
        {
            _logger.LogInformation("Syncing recent tasks...");

            var lastSync = await dbContext.SyncHistory
                .OrderByDescending(s => s.SyncTime)
                .Select(s => s.SyncTime)
                .FirstOrDefaultAsync(ct) ?? DateTime.UtcNow.AddHours(-1);

            var jql = $"updated >= '{lastSync:yyyy-MM-dd HH:mm}'";
            var encodedJql = Uri.EscapeDataString(jql);

            var response = await _httpClient.GetAsync(
                $"{_jiraSettings.BaseUrl}/rest/api/3/search?jql={encodedJql}&fields=summary,status,assignee,updated", ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JiraSearchResult>(cancellationToken: ct);

            foreach (var issue in result.Issues)
            {
                var task = await dbContext.Tasks
                    .FirstOrDefaultAsync(t => t.Key == issue.Key, ct)
                    ?? new ProjectTask(issue.Key, issue.Fields.Summary);

                task.UpdateDetails(
                    issue.Fields.Summary,
                    issue.Fields.Status.Name,
                    issue.Fields.Assignee?.AccountId,
                    issue.Fields.Updated);

                if (dbContext.Entry(task).State == EntityState.Detached)
                    dbContext.Tasks.Add(task);
            }

            await dbContext.SaveChangesAsync(ct);

            // Record sync
            dbContext.SyncHistory.Add(new SyncRecord
            {
                SyncTime = DateTime.UtcNow,
                TasksUpdated = result.Issues.Count
            });
            await dbContext.SaveChangesAsync(ct);
        }

        private async Task<ProjectMetrics> CalculateProjectMetrics(string projectKey, CancellationToken ct)
        {
            // Get open issues count
            var openIssuesResponse = await _httpClient.GetAsync(
                $"{_jiraSettings.BaseUrl}/rest/api/3/search?jql=project={projectKey} AND statusCategory!=Done&maxResults=0", ct);

            openIssuesResponse.EnsureSuccessStatusCode();
            var openData = await openIssuesResponse.Content.ReadFromJsonAsync<JiraCountResult>(cancellationToken: ct);

            // Get story points
            var storyPointsResponse = await _httpClient.GetAsync(
                $"{_jiraSettings.BaseUrl}/rest/api/3/search?jql=project={projectKey} AND storyPoints is not empty&fields=storyPoints,status", ct);

            storyPointsResponse.EnsureSuccessStatusCode();
            var spData = await storyPointsResponse.Content.ReadFromJsonAsync<JiraSearchResult>(cancellationToken: ct);

            return new ProjectMetrics
            {
                OpenIssues = openData.Total,
                TotalStoryPoints = spData.Issues.Sum(i => i.Fields.StoryPoints ?? 0),
                CompletedStoryPoints = spData.Issues
                    .Where(i => i.Fields.Status.StatusCategory.Key == "done")
                    .Sum(i => i.Fields.StoryPoints ?? 0),
                LastCalculated = DateTime.UtcNow
            };
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Jira Sync Service stopping");
            _httpClient.Dispose();
            await base.StopAsync(cancellationToken);
        }
    }
}
