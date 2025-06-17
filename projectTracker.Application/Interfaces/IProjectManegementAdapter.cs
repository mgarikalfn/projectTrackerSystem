
using projectTracker.Application.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace projectTracker.Application.Interfaces
{
    public interface IProjectManegementAdapter
    {
        Task<List<ProjectDto>> GetProjectsAsync(CancellationToken ct);
        Task<List<UsersDto>> GetAppUsersAsync(CancellationToken ct);
        Task<ProgressMetricsDto> GetProjectMetricsAsync(string projectKey, CancellationToken ct);

        // Enhanced task fetching (now returns more comprehensive TaskDto)
        Task<List<TaskDto>> GetProjectTasksAsync(string projectKey, CancellationToken ct);

        // New: Get tasks by specific JQL (for advanced filtering like backlog, bugs only etc.)
        Task<List<TaskDto>> GetProjectTasksByJqlAsync(string jql, CancellationToken ct ,string? fields = null);

        // New: Methods for Jira Boards and Sprints
        Task<List<JiraBoardDto>> GetBoardsAsync(CancellationToken ct);
        Task<List<JiraSprintDto>> GetSprintsForBoardAsync(int boardId, CancellationToken ct);
        Task<List<TaskDto>> GetIssuesInSprintAsync(int sprintId, CancellationToken ct);

        // New: Get changelog for a specific issue
        Task<JiraChangelog?> GetIssueChangelogAsync(string issueKeyOrId, CancellationToken ct);

        // For recent tasks, perhaps an incremental sync based on updated date
        Task<List<TaskDto>> GetRecentTasksAsync(DateTime lastSyncTime, CancellationToken ct);
    }
}