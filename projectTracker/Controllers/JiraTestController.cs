using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Interfaces;
using projectTracker.Application.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace projectTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class JiraTestController : ControllerBase
    {
        private readonly IProjectManegementAdapter _adapter;
        private readonly ILogger<JiraTestController> _logger;

        public JiraTestController(IProjectManegementAdapter adapter, ILogger<JiraTestController> logger)
        {
            _adapter = adapter;
            _logger = logger;
        }

        [HttpGet("projects")]
        public async Task<IActionResult> GetProjects(CancellationToken ct)
        {
            _logger.LogInformation("Received request to get all projects from JiraAdapter.");
            try
            {
                var projects = await _adapter.GetProjectsAsync(ct);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching projects from JiraAdapter.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("projects/{projectKey}/metrics")]
        public async Task<IActionResult> GetProjectMetrics(string projectKey, CancellationToken ct)
        {
            _logger.LogInformation("Received request to get metrics for project {ProjectKey}.", projectKey);
            try
            {
                var metrics = await _adapter.GetProjectMetricsAsync(projectKey, ct);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching metrics for project {ProjectKey}.", projectKey);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("projects/{projectKey}/tasks")]
        public async Task<IActionResult> GetProjectTasks(string projectKey, CancellationToken ct)
        {
            _logger.LogInformation("Received request to get tasks for project {ProjectKey}.", projectKey);
            try
            {
                var tasks = await _adapter.GetProjectTasksAsync(projectKey, ct);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tasks for project {ProjectKey}.", projectKey);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("tasksByJql")]
        public async Task<IActionResult> GetTasksByJql([FromQuery] string jql, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(jql))
            {
                return BadRequest("JQL query parameter is required.");
            }

            _logger.LogInformation("Received request to get tasks by JQL: {Jql}", jql);
            try
            {
                var tasks = await _adapter.GetProjectTasksByJqlAsync(jql, ct);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tasks by JQL: {Jql}", jql);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(CancellationToken ct)
        {
            _logger.LogInformation("Received request to get all users.");
            try
            {
                var users = await _adapter.GetAppUsersAsync(ct);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("boards")]
        public async Task<IActionResult> GetBoards(CancellationToken ct)
        {
            _logger.LogInformation("Received request to get all boards.");
            try
            {
                var boards = await _adapter.GetBoardsAsync(ct);
                return Ok(boards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching boards.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("boards/{boardId}/sprints")]
        public async Task<IActionResult> GetSprintsForBoard(int boardId, CancellationToken ct)
        {
            _logger.LogInformation("Received request to get sprints for board {BoardId}.", boardId);
            try
            {
                var sprints = await _adapter.GetSprintsForBoardAsync(boardId, ct);
                return Ok(sprints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sprints for board {BoardId}.", boardId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("sprints/{sprintId}/issues")]
        public async Task<IActionResult> GetIssuesInSprint(int sprintId, CancellationToken ct)
        {
            _logger.LogInformation("Received request to get issues for sprint {SprintId}.", sprintId);
            try
            {
                var tasks = await _adapter.GetIssuesInSprintAsync(sprintId, ct);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching issues for sprint {SprintId}.", sprintId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("issues/{issueKeyOrId}/changelog")]
        public async Task<IActionResult> GetIssueChangelog(string issueKeyOrId, CancellationToken ct)
        {
            _logger.LogInformation("Received request to get changelog for issue {IssueKeyOrId}.", issueKeyOrId);
            try
            {
                var changelog = await _adapter.GetIssueChangelogAsync(issueKeyOrId, ct);
                return changelog == null ? NotFound() : Ok(changelog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching changelog for issue {IssueKeyOrId}.", issueKeyOrId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("recentTasks")]
        public async Task<IActionResult> GetRecentTasks([FromQuery] DateTime lastSyncTime, CancellationToken ct)
        {
            if (lastSyncTime == default)
            {
                return BadRequest("lastSyncTime parameter is required.");
            }

            _logger.LogInformation("Received request to get recent tasks updated after {LastSyncTime}.", lastSyncTime);
            try
            {
                var tasks = await _adapter.GetRecentTasksAsync(lastSyncTime.ToUniversalTime(), ct);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent tasks.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}