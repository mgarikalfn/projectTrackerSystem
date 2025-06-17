// ProjectTracker.Infrastructure/Adapter/JiraAdapter.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Dto; // Reference to your DTOs
using projectTracker.Application.Interfaces;

namespace projectTracker.Infrastructure.Adapter
{
    public class JiraAdapter : IProjectManegementAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly string _jiraAgileBaseUrl;
        private readonly ILogger<JiraAdapter> _logger;
        private readonly string _storyPointsCustomFieldId; // Custom field ID for Story Points
        private readonly string _epicLinkCustomFieldId;
        private readonly string _sprintCustomFieldId;
        public JiraAdapter(HttpClient httpClient, IConfiguration config, ILogger<JiraAdapter> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var email = config["Jira:Email"];
            var token = config["Jira:ApiToken"];
            var baseUrl = config["Jira:BaseUrl"];
            // Get custom field ID from configuration, default to 10035 if not found
            _storyPointsCustomFieldId = config["Jira:CustomFieldIds:StoryPoints"] ?? "customfield_10035";
            _epicLinkCustomFieldId = config["Jira:CustomFieldIds:EpicLink"];
            _sprintCustomFieldId = config["Jira:CustomFieldIds:Sprint"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogError("Jira configuration (Email, ApiToken, BaseUrl) is incomplete.");
                throw new InvalidOperationException("Jira configuration is incomplete. Check appsettings.json.");
            }

            var base64Creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{email}:{token}"));

            _httpClient.BaseAddress = new Uri($"{baseUrl}/rest/api/3/"); // Default to core API v3
            _jiraAgileBaseUrl = $"{baseUrl}/rest/agile/1.0/"; // Agile API v1.0
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Creds);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Set up JSON serializer options for consistent parsing
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                Converters = { new JiraDateTimeConverter() },
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Jira often uses camelCase for properties
                PropertyNameCaseInsensitive = true // Essential for matching Jira's JSON to C# properties
            };
        }

        private readonly JsonSerializerOptions _jsonSerializerOptions;

        // Helper for common search queries returning only total count
        private async Task<JiraCountResult> GetJiraCount(string jql, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"search?jql={Uri.EscapeDataString(jql)}&maxResults=0", ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<JiraCountResult>(_jsonSerializerOptions, ct) ?? new JiraCountResult();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for JQL count: {Jql}", jql);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization failed for JQL count: {Jql}", jql);
                throw;
            }
        }

        public async Task<ProgressMetricsDto> GetProjectMetricsAsync(string projectKey, CancellationToken ct)
        {
            // Fetch ALL tasks for a project, then filter/aggregate locally.
            // This is more robust as it uses the same task fetching logic.
            var allProjectTasks = await GetProjectTasksAsync(projectKey, ct);

            // Aggregate metrics from the fetched tasks
            var totalTasks = allProjectTasks.Count;
            var completedTasks = allProjectTasks.Count(t => t.StatusCategory == "Done");
            var openIssues = totalTasks - completedTasks;
            var totalStoryPoints = allProjectTasks
                .Where(t => t.IssueType == "Story" && t.StoryPoints.HasValue) // Only count Story points for 'Story' issue type
                .Sum(t => t.StoryPoints ?? 0);
            var completedStoryPoints = allProjectTasks
                .Where(t => t.IssueType == "Story" && t.StatusCategory == "Done" && t.StoryPoints.HasValue)
                .Sum(t => t.StoryPoints ?? 0);
            var activeBlockers = allProjectTasks.Count(t => t.StatusCategory == "Blocked" || (t.Labels != null && t.Labels.Contains("Blocked", StringComparer.OrdinalIgnoreCase))); // Assuming 'Blocked' label or status category
            var recentUpdates = allProjectTasks.Count(t => (DateTime.UtcNow - t.UpdatedDate).TotalDays <= 3); // Tasks updated in last 3 days

            return new ProgressMetricsDto
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                OpenIssues = openIssues,
                TotalStoryPoints = totalStoryPoints,
                CompletedStoryPoints = completedStoryPoints,
                ActiveBlockers = activeBlockers,
                RecentUpdates = recentUpdates,
                LastCalculated = DateTime.UtcNow
            };
        }

        public async Task<List<ProjectDto>> GetProjectsAsync(CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync("project?expand=lead,description", ct);
                response.EnsureSuccessStatusCode();
                var jiraProjects = await response.Content.ReadFromJsonAsync<List<JiraProjectDto>>(_jsonSerializerOptions, ct);
                return jiraProjects?.Select(p => new ProjectDto
                {
                    Key = p.Key,
                    Name = p.Name,
                    Description = p.Description ?? string.Empty,
                    LeadName = p.Lead?.DisplayName
                }).ToList() ?? new List<ProjectDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for GetProjectsAsync.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization failed for GetProjectsAsync.");
                throw;
            }
        }

        public async Task<List<UsersDto>> GetAppUsersAsync(CancellationToken ct)
        {
            var users = new List<UsersDto>();
            var startAt = 0;
            const int maxResults = 50; // Jira's default page size for users search
            const int maxRequests = 20; // Safety limit to prevent infinite loops (adjust based on expected user count)

            for (int requestCount = 0; requestCount < maxRequests; requestCount++)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"users/search?startAt={startAt}&maxResults={maxResults}", ct);
                    response.EnsureSuccessStatusCode();

                    var page = await response.Content.ReadFromJsonAsync<List<JiraUserResponse>>(_jsonSerializerOptions, ct);
                    if (page == null || page.Count == 0) break; // No more users

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

                    // If the number of users returned is less than maxResults, it's the last page
                    if (page.Count < maxResults) break;

                    await Task.Delay(200, ct); // Basic rate limit adherence
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP request failed during GetAppUsersAsync (page {StartAt}).", startAt);
                    throw;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON deserialization failed during GetAppUsersAsync (page {StartAt}).", startAt);
                    throw;
                }
            }
            return users;
        }

        public async Task<List<TaskDto>> GetProjectTasksAsync(string projectKey, CancellationToken ct)
        {
            // Request all fields that might be useful for detailed task tracking
            var fields = $"key,summary,description,status,assignee,created,updated,duedate," +
                         $"{_storyPointsCustomFieldId},issuetype,parent,{_epicLinkCustomFieldId},timetracking,labels,priority,{_sprintCustomFieldId}";

            // Use GetProjectTasksByJqlAsync internally for consistent logic

            var jql = $"project=\"{projectKey}\"";
            return await GetProjectTasksByJqlAsync(jql,ct, fields);
        }

        public async Task<List<TaskDto>> GetProjectTasksByJqlAsync(string jql,  CancellationToken ct = default, string? fields = null)
        {
            // Default fields if not provided
            if (string.IsNullOrEmpty(fields))
            {
                fields = $"key,summary,description,status,assignee,created,updated,duedate," +
                        $"{_storyPointsCustomFieldId},issuetype,parent, {_epicLinkCustomFieldId},timetracking,labels,priority, {_sprintCustomFieldId}t" +
                        $"nt";
            }

            var tasks = new List<TaskDto>();
            var startAt = 0;
            const int maxResults = 100; // Jira's maxResults for search is typically 100
            const int maxRequests = 100; // Safety limit to prevent excessive calls for very large projects

            for (int i = 0; i < maxRequests; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(
                        $"search?jql={Uri.EscapeDataString(jql)}&fields={Uri.EscapeDataString(fields)}&startAt={startAt}&maxResults={maxResults}",
                        ct);

                    response.EnsureSuccessStatusCode();

                    var jiraData = await response.Content.ReadFromJsonAsync<JiraSearchResult>(_jsonSerializerOptions, ct);
                    if (jiraData == null || jiraData.Issues == null || jiraData.Issues.Count == 0) break;

                    tasks.AddRange(jiraData.Issues.Select(issue =>
                    {
                        var taskDto = new TaskDto
                        {
                            Key = issue.Key,
                            Title = issue.Fields.Summary,
                            Description = issue.Fields.Description?.GetRawText(), // Handle JsonElement for description
                            Status = issue.Fields.Status?.Name,
                            StatusCategory = issue.Fields.Status?.StatusCategory?.Name,
                            AssigneeId = issue.Fields.Assignee?.AccountId,
                            AssigneeName = issue.Fields.Assignee?.DisplayName ?? "Unassigned",
                            CreatedDate = issue.Fields.Created,
                            UpdatedDate = issue.Fields.Updated,
                            DueDate = issue.Fields.DueDate,
                            StoryPoints = issue.Fields.StoryPoints,
                            TimeEstimateMinutes = issue.Fields.TimeTracking?.OriginalEstimateSeconds != null
                                                  ? (int?)(issue.Fields.TimeTracking.OriginalEstimateSeconds / 60)
                                                  : null,
                            IssueType = issue.Fields.IssueType?.Name,
                            EpicKey = issue.Fields.Epic, // This assumes 'epic' is directly available if linked
                            ParentKey = issue.Fields.Parent?.Key,
                            Labels = issue.Fields.Labels,
                            Priority = issue.Fields.Priority?.Name,
                        };

                        // Handle sprint field (it's often an array)
                        var currentSprint = issue.Fields.Sprints?.FirstOrDefault(s => s.State == "active");
                        if (currentSprint != null)
                        {
                            taskDto.CurrentSprintJiraId = currentSprint.Id;
                            taskDto.CurrentSprintName = currentSprint.Name;
                            taskDto.CurrentSprintState = currentSprint.State;
                        }

                        return taskDto;
                    }));

                    startAt += jiraData.Issues.Count;
                    if (startAt >= jiraData.Total) break; // All issues fetched
                    await Task.Delay(200, ct); // Basic rate limit adherence
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP request failed for JQL search: {Jql} at startAt {StartAt}", jql, startAt);
                    throw;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON deserialization failed for JQL search: {Jql} at startAt {StartAt}", jql, startAt);
                    throw;
                }
            }
            return tasks;
        }

        // --- New Methods for Jira Agile API ---

        public async Task<List<JiraBoardDto>> GetBoardsAsync(CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_jiraAgileBaseUrl}board?type=scrum,kanban", ct); // Filter to common board types
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<JiraBoardsResponse>(_jsonSerializerOptions, ct);
                return result?.Values ?? new List<JiraBoardDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for GetBoardsAsync.");
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization failed for GetBoardsAsync.");
                throw;
            }
        }

        public async Task<List<JiraSprintDto>> GetSprintsForBoardAsync(int boardId, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_jiraAgileBaseUrl}board/{boardId}/sprint?state=active,future,closed", ct);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<JiraSprintsResponse>(_jsonSerializerOptions, ct);
                return result?.Values ?? new List<JiraSprintDto>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for GetSprintsForBoardAsync (Board ID: {BoardId}).", boardId);
                throw;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization failed for GetSprintsForBoardAsync (Board ID: {BoardId}).", boardId);
                throw;
            }
        }

        public async Task<List<TaskDto>> GetIssuesInSprintAsync(int sprintId, CancellationToken ct)
        {
            // Agile API's sprint issues endpoint accepts 'jql' parameter which is useful for filtering.
            // However, the standard way is to get all issues in sprint.
            // Reusing GetProjectTasksByJqlAsync with specific JQL to allow for full field set
            var jql = $"sprint = {sprintId}";
            return await GetProjectTasksByJqlAsync(jql, ct, null); // Use default fields
        }

        public async Task<JiraChangelog?> GetIssueChangelogAsync(string issueKeyOrId, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync($"issue/{issueKeyOrId}?expand=changelog", ct);
                response.EnsureSuccessStatusCode();
                var issueWithChangelog = await response.Content.ReadFromJsonAsync<JiraIssueWithChangelog>(_jsonSerializerOptions, ct);
                return issueWithChangelog?.Changelog;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for GetIssueChangelogAsync (Issue: {IssueKey}).", issueKeyOrId);
                return null; // Return null if changelog couldn't be fetched
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization failed for GetIssueChangelogAsync (Issue: {IssueKey}).", issueKeyOrId);
                return null;
            }
        }

        public async Task<List<TaskDto>> GetRecentTasksAsync(DateTime lastSyncTime, CancellationToken ct)
        {
            // Format DateTime to Jira's expected JQL format (e.g., "YYYY-MM-DD HH:MM")
            var lastSyncJiraFormat = lastSyncTime.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
            // This JQL gets all issues updated AFTER the last sync time
            // For production, you might want to limit to specific projects or a reasonable date range
            var jql = $"updated >= \"{lastSyncJiraFormat}\" ORDER BY updated ASC";
            return await GetProjectTasksByJqlAsync(jql, ct, null);
        }

       
    }
}