using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;

namespace projectTracker.Infrastructure.Adapter
{
    public class JiraAdapter : IProjectManegementAdapter
    {
        private const double V = 0.0;
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

        async Task<List<UsersDto>> IProjectManegementAdapter.GetAppUsersAsync(CancellationToken ct)
        {
            
            var users = new List<UsersDto>();
            var startAt = 0;
            const int maxResults = 50; // Jira's default page size
            const int maxRequests = 20; // Safety limit to prevent infinite loops
            var requestCount = 0;

            while (requestCount < maxRequests)
            {
                requestCount++;

                var response = await _httpClient.GetAsync(
                    $"users/search?startAt={startAt}&maxResults={maxResults}", ct);

                response.EnsureSuccessStatusCode();

                var page = await response.Content.ReadFromJsonAsync<List<JiraUserResponse>>(cancellationToken: ct);
                if (page == null || page.Count == 0) break;

                users.AddRange(page.Select(u => new UsersDto
                {
                    AccountId = u.AccountId,
                    Email = u.EmailAddress,
                    DisplayName = u.DisplayName,
                    AvatarUrl = u.AvatarUrls?.GetValueOrDefault("48x48"),
                    Active = u.Active,
                    Source = "Jira"
                }));

                startAt += maxResults;

                // Break if we've gotten all users
                if (page.Count < maxResults) break;

                // Respect rate limits
                await Task.Delay(200, ct);
            }

            return users;
        }

        public async Task<List<TaskDto>> GetProjectTasksAsync(string projectKey, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync(
                $"search?jql=project={projectKey}&fields=key,summary,description,status,assignee,created,updated,duedate,customfield_10035&maxResults=1000",
                ct);

            response.EnsureSuccessStatusCode();

            var options = new JsonSerializerOptions
            {
                Converters = { new JiraDateTimeConverter() },
                PropertyNameCaseInsensitive = true
            };

            var jiraData = await response.Content.ReadFromJsonAsync<JiraSearchResult>(options, ct);

            return jiraData.Issues.Select(issue => new TaskDto
            {
                Key = issue.Key,
                Title = issue.Fields.Summary,
                Description = issue.Fields.Description,
                Status = issue.Fields.Status.Name,
                AssigneeId = issue.Fields.Assignee?.AccountId,
                AssigneeName = issue.Fields.Assignee?.DisplayName ?? "Unassigned",
                CreatedDate = issue.Fields.CreatedDate,
                UpdatedDate = issue.Fields.UpdatedDate,
                DueDate = issue.Fields.DueDate, // Add this to your JiraIssueFieldsDto
                StoryPoints = (int)(issue.Fields.StoryPoints ?? 0)
            }).ToList();
        }
    }
    
}
