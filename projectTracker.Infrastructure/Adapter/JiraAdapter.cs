using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;

namespace projectTracker.Infrastructure.Adapter
{
    public class JiraAdapter : IProjectManegementAdapter
    {
        private readonly HttpClient _httpClient;
        public JiraAdapter(HttpClient httpClient, IConfiguration config )
        {
            _httpClient = httpClient;
            var email = config["Jira:Email"];
            var token = config["Jira:ApiToken"];
            var base64Creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{token}"));

            _httpClient.BaseAddress = new Uri($"{config["Jira:BaseUrl"]}/rest/api/3/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Creds);
        }

        public async Task<ProgressMetricsDto> GetProjectMetricsAsync(string projectKey, CancellationToken ct)
        {
            // 1. Total tasks
            var totalResponse = await _httpClient.GetAsync(
                $"search?jql=project={projectKey}&maxResults=0", ct);
            totalResponse.EnsureSuccessStatusCode();
            var totalData = await totalResponse.Content.ReadFromJsonAsync<JiraCountResult>(cancellationToken: ct);

            // 2. Completed tasks
            var completedResponse = await _httpClient.GetAsync(
                $"search?jql=project={projectKey} AND statusCategory = Done&maxResults=0", ct);
            completedResponse.EnsureSuccessStatusCode();
            var completedData = await completedResponse.Content.ReadFromJsonAsync<JiraCountResult>(cancellationToken: ct);

            // 3. Story points
            var spResponse = await _httpClient.GetAsync(
                $"search?jql=project={projectKey} AND cf[10035] IS NOT EMPTY&fields=customfield_10035,status&maxResults=1000", ct);
            spResponse.EnsureSuccessStatusCode();
            var spData = await spResponse.Content.ReadFromJsonAsync<JiraSearchResult>(cancellationToken: ct);

            // 4. Blocked tasks
            var blockedResponse = await _httpClient.GetAsync(
                $"search?jql=project={projectKey} AND (status = Blocked OR labels = Blocked)&maxResults=0", ct);
            blockedResponse.EnsureSuccessStatusCode();
            var blockedData = await blockedResponse.Content.ReadFromJsonAsync<JiraCountResult>(cancellationToken: ct);

            // 5. Recent activity (last 3 days)
            var recentResponse = await _httpClient.GetAsync(
                $"search?jql=project={projectKey} AND updated >= -3d&maxResults=0", ct);
            recentResponse.EnsureSuccessStatusCode();
            var recentData = await recentResponse.Content.ReadFromJsonAsync<JiraCountResult>(cancellationToken: ct);

            return new ProgressMetricsDto
            {
                TotalTasks = totalData.Total,
                CompletedTasks = completedData.Total,
                OpenIssues = totalData.Total - completedData.Total,
                TotalStoryPoints = (int)spData.Issues.Sum(i => i.Fields.StoryPoints ?? 0),
                CompletedStoryPoints = (int)spData.Issues
                    .Where(i => i.Fields.Status.StatusCategory.Key == "done")
                    .Sum(i => i.Fields.StoryPoints ?? 0),
                ActiveBlockers = blockedData.Total,
                RecentUpdates = recentData.Total,
                LastCalculated = DateTime.UtcNow
            };
        }

        public async Task<List<ProjectDto>> GetProjectsAsync(CancellationToken ct)
        {
            var response = await _httpClient.GetAsync("project?expand=lead,description", ct);
            response.EnsureSuccessStatusCode();

            var jiraProjects = await response.Content.ReadFromJsonAsync<List<JiraProjectDto>>(cancellationToken: ct);

            return jiraProjects.Select(p => new ProjectDto
            {
                Key = p.Key,
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                LeadName = p.Lead?.DisplayName
            }).ToList();
        }

        public Task<List<TaskDto>> GetRecentTasksAsync(DateTime lastSyncTime, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        
    }
}
