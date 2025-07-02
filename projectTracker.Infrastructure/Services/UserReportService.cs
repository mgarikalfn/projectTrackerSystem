using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Dto.Report;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Services
{
    public class UserReportService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UserReportService> _logger;

        public UserReportService(AppDbContext dbContext, ILogger<UserReportService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets a summary of all projects a specific user has been involved in (assigned tasks).
        /// </summary>
        /// <param name="userId">The ID of the user (AppUser.Id).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of UserProjectSummaryDto.</returns>
        public async Task<List<UserProjectSummaryDto>> GetUserProjectsSummaryAsync(string userId, CancellationToken ct)
        {
            _logger.LogInformation("Fetching project summary for user ID: {UserId}", userId);

            // Fetch tasks where the user is the assignee
            var userTasks = await _dbContext.Tasks
                .AsNoTracking()
                .Include(t => t.Project) // Include Project to get ProjectKey and ProjectName
                .Include(t => t.Assignee) // Include Assignee to ensure user details are available
                .Where(t => t.AssigneeId == userId)
                .ToListAsync(ct);

            if (!userTasks.Any())
            {
                _logger.LogInformation("No tasks found for user ID: {UserId}. Returning empty project summary.", userId);
                return new List<UserProjectSummaryDto>();
            }

            // Group tasks by Project and calculate aggregate metrics
            var projectSummaries = userTasks
                .GroupBy(t => t.ProjectId)
                .Select(g => new UserProjectSummaryDto
                {
                    ProjectId = g.Key,
                    ProjectKey = g.First().Project?.Key ?? "N/A", // Get from the first task's project
                    ProjectName = g.First().Project?.Name ?? "Unknown Project",

                    TotalTasksAssigned = g.Count(),
                    CompletedTasks = g.Count(t => t.Status == Domain.Enums.TaskStatus.Done),
                    TotalStoryPointsAssigned = g.Sum(t => t.StoryPoints ?? 0),
                    CompletedStoryPoints = g.Where(t => t.Status == Domain.Enums.TaskStatus.Done)
                                            .Sum(t => t.StoryPoints ?? 0)
                })
                .OrderBy(s => s.ProjectName)
                .ToList();

            // Fix for CS1503 errors: Ensure correct argument types for LogInformation
            _logger.LogInformation("Found {Count} projects for user ID: {UserId}", projectSummaries.Count.ToString(), userId);
            return projectSummaries;
        } 

      public async Task<UserProjectContributionDetailDto?> GetUserProjectContributionDetailAsync(string userId, string projectId, CancellationToken ct)
        {
            _logger.LogInformation("Fetching detailed contribution report for user ID: {UserId} in project ID: {ProjectId}", userId, projectId);

            // Fetch user and project details  
            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for detailed project contribution report.", userId);
                return null;
            }

            var project = await _dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == projectId, ct);
            if (project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found for user {UserId}'s detailed contribution report.", projectId, userId);
                return null;
            }

            // Fetch all tasks assigned to this user in this project  
            var userTasksInProject = await _dbContext.Tasks
                .AsNoTracking()
                .Where(t => t.AssigneeId == userId && t.ProjectId == projectId)
                .ToListAsync(ct);

            if (!userTasksInProject.Any())
            {
                _logger.LogInformation("No tasks found for user {UserId} in project {ProjectId}. Returning empty detail report.", userId, projectId);
                return new UserProjectContributionDetailDto
                {
                    UserId = userId,
                    UserName = user.DisplayName ?? user.Email,
                    ProjectId = projectId,
                    ProjectKey = project.Key,
                    ProjectName = project.Name,
                    UserTasksInProject = new List<TaskDto>(), // Initialize empty lists  
                    TaskStatusCounts = new Dictionary<string, int>(),
                    IssueTypeCounts = new Dictionary<string, int>(),
                    PriorityCounts = new Dictionary<string, int>(),
                    SprintsInvolvedIn = new List<SprintListItemDto>()
                };
            }

            // Aggregate metrics  
            int totalTasks = userTasksInProject.Count;
            int completedTasks = userTasksInProject.Count(t => t.Status == Domain.Enums.TaskStatus.Done);
            decimal totalStoryPoints = userTasksInProject.Sum(t => t.StoryPoints ?? 0);
            decimal completedStoryPoints = userTasksInProject.Where(t => t.Status == Domain.Enums.TaskStatus.Done)
                                                            .Sum(t => t.StoryPoints ?? 0);
            int overdueTasks = userTasksInProject.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Today && t.Status != Domain.Enums.TaskStatus.Done);
            int activeBlockers = userTasksInProject.Count(t => t.Status == Domain.Enums.TaskStatus.Blocked); // Assuming 'blocked' label  

            // Calculate breakdowns  
            var taskStatusCounts = userTasksInProject
                .GroupBy(t => t.Status.ToString()) // Convert TaskStatus enum to string  
                .ToDictionary(g => g.Key, g => g.Count());

            var issueTypeCounts = userTasksInProject
                .GroupBy(t => t.IssueType ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            var priorityCounts = userTasksInProject
                .GroupBy(t => t.Priority ?? "N/A")
                .ToDictionary(g => g.Key, g => g.Count());

            var involvedSprintJiraIds = userTasksInProject
               .Where(t => t.JiraSprintId.HasValue) // Correct property name from type signature  
               .Select(t => t.JiraSprintId!.Value)
               .Distinct()
               .ToList();

            var sprintsInvolvedIn = await _dbContext.Sprints
                .AsNoTracking()
                .Where(s => involvedSprintJiraIds.Contains(s.JiraId))
                .Select(s => new SprintListItemDto { Id = s.Id, JiraId = s.JiraId, Name = s.Name, State = s.State.ToString() })
                .ToListAsync(ct);

            var detailReport = new UserProjectContributionDetailDto
            {
                UserId = userId,
                UserName = user.DisplayName ?? user.Email, // Prefer display name  
                ProjectId = projectId,
                ProjectKey = project.Key,
                ProjectName = project.Name,

                TotalTasksAssigned = totalTasks,
                CompletedTasks = completedTasks,
                TotalStoryPointsAssigned = totalStoryPoints,
                CompletedStoryPoints = completedStoryPoints,
                OverdueTasks = overdueTasks,
                ActiveBlockers = activeBlockers,
                UserTasksInProject = userTasksInProject.Select(t => new TaskDto
                {
                    Key = t.Key,
                    Title = t.Summary, // Correct property name based on ProjectTask signature  
                    Description = t.Description,
                    Status = t.Status.ToString(),
                    AssigneeId = t.AssigneeId,
                    AssigneeName = t.AssigneeName,
                    CreatedDate = t.CreatedDate,
                    UpdatedDate = t.UpdatedDate,
                    DueDate = t.DueDate,
                    StoryPoints = t.StoryPoints,
                    TimeEstimateMinutes = t.TimeEstimateMinutes,
                    IssueType = t.IssueType,
                    EpicKey = t.EpicKey,
                    ParentKey = t.ParentKey,
                    Priority = t.Priority,
                    CurrentSprintJiraId = t.JiraSprintId,
                    CurrentSprintName = sprintsInvolvedIn.FirstOrDefault(x => x.JiraId == t.JiraSprintId)?.Name, // Fix for CS0029 and CS1662
                    CurrentSprintState = sprintsInvolvedIn.FirstOrDefault(x => x.JiraId == t.JiraSprintId)?.State
                }).ToList(),
                TaskStatusCounts = taskStatusCounts,
                IssueTypeCounts = issueTypeCounts,
                PriorityCounts = priorityCounts,
                SprintsInvolvedIn = sprintsInvolvedIn
            };

            _logger.LogInformation("Successfully generated detailed contribution report for user {UserId} in project {ProjectId}.", userId, projectId);
            return detailReport;
        }
    }
    
}
