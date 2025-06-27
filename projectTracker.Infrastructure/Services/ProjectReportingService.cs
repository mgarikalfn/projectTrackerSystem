

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities; 
using ProjectTracker.Infrastructure.Data;
using TaskStatus = projectTracker.Domain.Enums.TaskStatus; 

namespace projectTracker.Infrastructure.Services
{
    public class ProjectReportingService : IProjectReportingService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ProjectReportingService> _logger;

        public ProjectReportingService(AppDbContext dbContext, ILogger<ProjectReportingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ProjectSprintOverviewDto?> GetProjectSprintOverviewAsync(string projectKey, CancellationToken ct)
        {
            var project = await _dbContext.Projects
                                          .FirstOrDefaultAsync(p => p.Key == projectKey, ct); // Removed .Include(p => p.Tasks) as it's not needed for this overview

            if (project == null)
            {
                _logger.LogWarning("Project {ProjectKey} not found for overview report.", projectKey);
                return null;
            }

            // Fetch only the necessary sprint details (Id, Name, State) for the overview
            // Filter by the project's internal ID to ensure correct association
            var sprintsList = await _dbContext.Sprints
                .Where(s => s.Board!.ProjectId == project.Id) // Filter sprints by the resolved project ID
                .OrderByDescending(s => s.StartDate) // Order to easily find the most recent/active
                .Select(s => new SprintListItemDto // Project to the simpler DTO
                {
                    Id = s.Id,
                    Name = s.Name,
                    State = s.State.ToString() // Convert enum to string for the DTO
                })
                .ToListAsync(ct);

            var overview = new ProjectSprintOverviewDto
            {
                ProjectKey = project.Key,
                ProjectName = project.Name,
                Sprints = sprintsList // Assign the list of lighter sprint DTOs
            };

            _logger.LogInformation("Generated project sprint overview for project {ProjectKey} with {SprintCount} sprints.", projectKey, sprintsList.Count);
            return overview;
        }
    

        public async Task<List<SprintReportDto>> GetAllSprintsForProjectAsync(string projectKey, CancellationToken ct)
        {
            // Step 1: Get the Project ID
            var projectId = await _dbContext.Projects
                                            .Where(p => p.Key == projectKey)
                                            .Select(p => p.Id)
                                            .FirstOrDefaultAsync(ct);

            if (projectId == null)
            {
                _logger.LogWarning("Project {ProjectKey} not found, cannot get sprints.", projectKey);
                return new List<SprintReportDto>();
            }

            // Step 2: Get all tasks for this project, including their Sprint and Assignee details
            var projectTasks = await _dbContext.Tasks
                                               .Where(t => t.ProjectId == projectId)
                                               .Include(t => t.Sprint) // Include Sprint navigation property
                                               .ToListAsync(ct);

            // Step 3: Extract unique sprints associated with these tasks
            var relevantSprints = projectTasks
                                    .Where(t => t.Sprint != null)
                                    .Select(t => t.Sprint!)
                                    .DistinctBy(s => s.Id) // Use DistinctBy from System.Linq if C# 10+, otherwise manually
                                    .OrderByDescending(s => s.StartDate ?? DateTime.MinValue) // Order by start date
                                    .ToList();

            var sprintReports = new List<SprintReportDto>();

            foreach (var sprint in relevantSprints)
            {
                sprintReports.Add(await BuildSprintReportDto(sprint, projectTasks.Where(t => t.SprintId == sprint.Id).ToList(), ct));
            }

            return sprintReports;
        }


        public async Task<SprintReportDto?> GetSprintReportAsync(Guid sprintId, CancellationToken ct)
        {
            var sprint = await _dbContext.Sprints
                                         .Include(s => s.Board) // Include board for board name
                                         .FirstOrDefaultAsync(s => s.Id == sprintId, ct);

            if (sprint == null)
            {
                _logger.LogWarning("Sprint with ID {SprintId} not found for report.", sprintId);
                return null;
            }

            // Get all tasks associated with this sprint
            var tasksInSprint = await _dbContext.Tasks
                                                .Where(t => t.SprintId == sprintId)
                                                .ToListAsync(ct);

            return await BuildSprintReportDto(sprint, tasksInSprint, ct);
        }

        
        private async Task<SprintReportDto> BuildSprintReportDto(Sprint sprint, List<ProjectTask> tasksInSprint, CancellationToken ct)
        {
            // Metrics Calculations
            var totalStoryPoints = tasksInSprint
                .Where(t => t.IssueType == "Story" && t.StoryPoints.HasValue) // Only stories for SP
                .Sum(t => t.StoryPoints!.Value);

            var completedStoryPoints = tasksInSprint
                .Where(t => t.IssueType == "Story" && t.Status == TaskStatus.Done && t.StoryPoints.HasValue)
                .Sum(t => t.StoryPoints!.Value);

            var totalTasks = tasksInSprint.Count;
            var completedTasks = tasksInSprint.Count(t => t.Status == TaskStatus.Done);

            var activeBlockers = tasksInSprint.Count(t => t.Status == TaskStatus.Blocked); // Using your Enum
            var overdueTasks = tasksInSprint.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.Today && t.Status != TaskStatus.Done);
            var bugsCreatedThisSprint = tasksInSprint.Count(t => t.IssueType == "Bug" && t.CreatedDate >= sprint.StartDate && t.CreatedDate <= sprint.EndDate);

            // Task Status Breakdown
            var taskStatusCounts = tasksInSprint
                .GroupBy(t => t.Status.ToString()) // Group by Enum name
                .ToDictionary(g => g.Key, g => g.Count());

            // Issue Type Counts
            var issueTypeCounts = tasksInSprint
                .GroupBy(t => t.IssueType)
                .ToDictionary(g => g.Key, g => g.Count());

            // Team Workload
            var developerWorkloads = tasksInSprint
                .Where(t => t.AssigneeId != null) // Only assigned tasks
                .GroupBy(t => new { t.AssigneeId, t.AssigneeName })
                .Select(g => new DeveloperWorkloadDto
                {
                    AssigneeId = g.Key.AssigneeId!,
                    AssigneeName = g.Key.AssigneeName!,
                    EstimatedWork = g.Sum(t => t.StoryPoints ?? (decimal)(t.TimeEstimateMinutes ?? 0) / 60), // Sum SP or convert minutes to hours
                    CompletedWork = g.Where(t => t.Status == TaskStatus.Done)
                                     .Sum(t => t.StoryPoints ?? (decimal)(t.TimeEstimateMinutes ?? 0) / 60),
                    TaskStatusBreakdown = g.GroupBy(t => t.Status.ToString())
                                           .ToDictionary(sg => sg.Key, sg => sg.Count())
                }).ToList();

            // Example in your ProjectReportingService (or similar report generation logic)
            // Make sure tasksInSprint is fetched including the UpdatedDate property.
            var recentActivities = tasksInSprint
                .OrderByDescending(t => t.UpdatedDate) // Use ProjectTask.UpdatedDate for Jira's last update
                .Take(5)
                .Select(t => new RecentActivityItemDto
                {
                    TaskKey = t.Key,
                    Description = $"{t.IssueType} {t.Key} updated (Status: {t.Status}, Assignee: {t.AssigneeName})",
                    ChangedBy = t.AssigneeName, // This is an approximation; true changelog requires more data
                    Timestamp = t.UpdatedDate // This will be Jira's updated timestamp
                }).ToList();


            // Calculate TasksMovedFromPreviousSprint - This is hard without historical data
            // To implement this accurately, you'd need:
            // 1. To store historical sprint assignments for tasks in your DB (e.g., in a TaskSprintHistory table)
            // 2. Or, fetch changelog from Jira for *every* task in the current sprint during sync,
            //    parse it, and then store that specific "moved from sprint X to sprint Y" event.
            // For this report, we'll leave it as 0 unless you implement the historical tracking.
            var tasksMovedFromPreviousSprint = 0;


            var tasksInSprintDtos = tasksInSprint.Select(t => new TaskDto
            {
                Key = t.Key,
                Title = t.Summary,
                Description = t.Description,
                Status = t.Status.ToString(),
                StatusCategory = GetStatusCategory(t.Status),
                Priority= t.Priority,
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
                CurrentSprintJiraId = t.JiraSprintId,
                CurrentSprintName = t.Sprint?.Name // Use the included Sprint navigation property
            }).ToList();



            return new SprintReportDto
            {
                Id = sprint.Id,
                JiraId = sprint.JiraId,
                Name = sprint.Name,
                State = sprint.State,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                CompleteDate = sprint.CompleteDate,
                Goal = sprint.Goal,
                BoardName = sprint.Board?.Name ?? "N/A", // From navigation property

                TotalStoryPoints = totalStoryPoints,
                CompletedStoryPoints = completedStoryPoints,
                StoryPointCompletionPercentage = totalStoryPoints > 0 ? (completedStoryPoints / totalStoryPoints) * 100 : 0,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                TaskCompletionPercentage = totalTasks > 0 ? (decimal)completedTasks / totalTasks * 100 : 0,
                ActiveBlockers = activeBlockers,
                OverdueTasks = overdueTasks,
                BugsCreatedThisSprint = bugsCreatedThisSprint,
                TasksMovedFromPreviousSprint = tasksMovedFromPreviousSprint,

                TaskStatusCounts = taskStatusCounts,
                IssueTypeCounts = issueTypeCounts,
                DeveloperWorkloads = developerWorkloads,
                RecentActivities = recentActivities,
                TasksInSprint = tasksInSprintDtos
            };
        }
       
        private static string GetStatusCategory(TaskStatus status)
        {
            
            return status switch
            {
                TaskStatus.Done => "Done",
                TaskStatus.InProgress => "In Progress",
                TaskStatus.ToDo => "To Do",
                TaskStatus.Blocked => "Blocked",
                // Add other TaskStatus values as needed
                _ => "Other"
            };
        }
    }


}