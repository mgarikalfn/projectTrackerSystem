using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;
using projectTracker.Domain.ValueObjects;
using ProjectTracker.Infrastructure.Data;
using TaskStatus = projectTracker.Domain.Enums.TaskStatus;

namespace projectTracker.Infrastructure.Sync
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
            _logger.LogInformation("Starting full sync...");

            var syncStartTime = DateTime.UtcNow;
            var syncHistory = SyncHistory.Start(
                type: SyncType.Full,
                projectId: null,
                trigger: "Manual/Scheduled"
            );
            _dbContext.Set<SyncHistory>().Add(syncHistory);
            await _dbContext.SaveChangesAsync(ct);

            int totalTasksProcessed = 0;
            int totalTasksCreated = 0;
            int totalTasksUpdated = 0;

            try
            {
                // Initial sync steps (users, boards, projects, tasks)
                await SyncUsersAsync(ct);
                await SyncBoardsAndSprintsAsync(ct);
                await SyncProjectsAsync(ct);

                var taskSyncCounts = await SyncTasksAsync(ct);
                totalTasksCreated = taskSyncCounts.Created;
                totalTasksUpdated = taskSyncCounts.Updated;
                totalTasksProcessed = totalTasksCreated + totalTasksUpdated;

                // Use the Complete domain method, which should set Duration internally
                syncHistory.Complete(totalTasksCreated, totalTasksUpdated);


                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Full sync completed successfully. Created: {Created}, Updated: {Updated}", totalTasksCreated, totalTasksUpdated);
            }
            catch (Exception ex)
            {
                // Use the Fail domain method, which should set Duration internally
                syncHistory.Fail(ex.Message);

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogError(ex, "Failed to complete full sync");
                throw;
            }
        }

        private async Task SyncUsersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting user sync...");
            try
            {
                var jiraUsers = await _adapter.GetAppUsersAsync(ct);
                _logger.LogDebug("Retrieved {UserCount} users from Jira", jiraUsers.Count);

                foreach (var jiraUser in jiraUsers)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(jiraUser.Email))
                        {
                            _logger.LogWarning("Skipping user {AccountId} with empty email", jiraUser.AccountId);
                            continue;
                        }

                        var user = await _dbContext.Users
                            .FirstOrDefaultAsync(u =>
                                u.Email == jiraUser.Email ||
                                u.AccountId == jiraUser.AccountId, ct);

                        if (user == null)
                        {
                            user = new AppUser
                            {
                                Id = Guid.NewGuid().ToString(),
                                UserName = jiraUser.Email,
                                Email = jiraUser.Email,
                                EmailConfirmed = true,
                                AccountId = jiraUser.AccountId,
                                DisplayName = jiraUser.DisplayName,
                                AvatarUrl = jiraUser.AvatarUrl,
                                IsActive = jiraUser.Active,
                                Source = UserSource.Jira // Mark as Jira-synced
                            };
                            _dbContext.Users.Add(user);
                            _logger.LogDebug("Added new user {Email}", jiraUser.Email);
                        }
                        else
                        {
                            user.AccountId = jiraUser.AccountId;
                            user.DisplayName = jiraUser.DisplayName;
                            user.AvatarUrl = jiraUser.AvatarUrl;
                            user.IsActive = jiraUser.Active;
                            user.Source = UserSource.Jira; // Ensure source is updated if it was manually changed or initially wrong
                            _logger.LogDebug("Updated existing user {Email}", jiraUser.Email);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing user {AccountId} during sync", jiraUser.AccountId);
                    }
                }
                var userChanges = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("User sync completed. {ChangesCount} changes saved", userChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete user sync");
                throw;
            }
        }

        private async Task SyncBoardsAndSprintsAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting board and sprint sync...");
            try
            {
                var jiraBoards = await _adapter.GetBoardsAsync(ct);
                _logger.LogDebug("Retrieved {BoardCount} boards from Jira", jiraBoards.Count);

                foreach (var boardDto in jiraBoards)
                {
                    if (boardDto.Type != "scrum")
                    {
                        _logger.LogInformation("Skipping sprint sync for board '{BoardName}' (Jira ID: {JiraId}) because its type '{BoardType}' does not support sprints.", boardDto.Name, boardDto.Id, boardDto.Type);
                        continue;
                    }

                    try
                    {
                        var board = await _dbContext.Boards
                            .FirstOrDefaultAsync(b => b.JiraId == boardDto.Id, ct);

                        if (board == null)
                        {
                            board = new Board(boardDto.Id, boardDto.Name, boardDto.Type);
                            _dbContext.Boards.Add(board);
                            _logger.LogDebug("Added new board {BoardName} (Jira ID: {JiraId})", board.Name, board.JiraId);
                        }
                        else
                        {
                            board.UpdateDetails(boardDto.Name, boardDto.Type);
                            _logger.LogDebug("Updated existing board {BoardName} (Jira ID: {JiraId})", board.Name, board.JiraId);
                        }

                        var jiraSprints = await _adapter.GetSprintsForBoardAsync(boardDto.Id, ct);
                        _logger.LogDebug("Retrieved {SprintCount} sprints for board {BoardName}", jiraSprints.Count, boardDto.Name);

                        foreach (var sprintDto in jiraSprints)
                        {
                            try
                            {
                                var sprint = await _dbContext.Sprints
                                    .FirstOrDefaultAsync(s => s.JiraId == sprintDto.Id, ct);

                                if (sprint == null)
                                {
                                    sprint = new Sprint(
                                        sprintDto.Id, board.Id, sprintDto.Name, sprintDto.State,
                                        sprintDto.StartDate, sprintDto.EndDate, sprintDto.CompleteDate, sprintDto.Goal);
                                    _dbContext.Sprints.Add(sprint);
                                    _logger.LogDebug("Added new sprint {SprintName} (Jira ID: {JiraId}) for board {BoardName}", sprint.Name, sprint.JiraId, board.Name);
                                }
                                else
                                {
                                    sprint.UpdateDetails(
                                        name: sprintDto.Name,
                                        state: sprintDto.State,
                                        startDate: sprintDto.StartDate,
                                        endDate: sprintDto.EndDate,
                                        completeDate: sprintDto.CompleteDate,
                                        goal: sprintDto.Goal
                                    );
                                    _logger.LogDebug("Updated existing sprint {SprintName} (Jira ID: {JiraId}) for board {BoardName}", sprint.Name, sprint.JiraId, board.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing sprint {SprintId} for board {BoardId}", sprintDto.Id, boardDto.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing board {BoardId} during sync. This board's sprints might not be retrievable.", boardDto.Id);
                    }
                }
                var changes = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Board and sprint sync completed. {ChangesCount} changes saved", changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete board and sprint sync due to an unhandled error.");
                throw;
            }
        }

        private async Task SyncProjectsAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting project sync...");
            try
            {
                var jiraProjects = await _adapter.GetProjectsAsync(ct);
                _logger.LogDebug("Retrieved {ProjectCount} projects from Jira", jiraProjects.Count);

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
                            );
                            _dbContext.Projects.Add(project);
                            _logger.LogDebug("Added new project {ProjectKey}", project.Key);
                        }
                        else
                        {
                            project.UpdateDetails(
                                name: projectDto.Name,
                                description: projectDto.Description,
                                leadName: projectDto.LeadName
                            );
                            _logger.LogDebug("Updated existing project {ProjectKey}", project.Key);
                        }

                        // Get and update metrics
                        var metricsDto = await _adapter.GetProjectMetricsAsync(projectDto.Key, ct);
                        var metrics = new ProgressMetrics(
                            totalTasks: metricsDto.TotalTasks,
                            completedTasks: metricsDto.CompletedTasks,
                            storyPointsCompleted: metricsDto.CompletedStoryPoints,
                            storyPointsTotal: metricsDto.TotalStoryPoints,
                            activeBlockers: metricsDto.ActiveBlockers,
                            recentUpdates: metricsDto.RecentUpdates);

                        project.UpdateProgressMetrics(metrics);

                        // Calculate and update health
                        var health = _riskCalculator.Calculate(metricsDto);
                        project.UpdateHealthMetrics(health);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing project {ProjectKey} during sync", projectDto.Key);
                    }
                }

                var projectChanges = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Project sync completed. {ChangesCount} changes saved", projectChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete project sync");
                throw;
            }
        }

        private async Task<(int Created, int Updated)> SyncTasksAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting task sync...");
            int createdCount = 0;
            int updatedCount = 0;

            try
            {
                var projects = await _dbContext.Projects.ToListAsync(ct);
                var sprintsInDb = await _dbContext.Sprints.ToListAsync(ct);
                // Create a map from JiraId to the local Sprint object for efficient lookup
                var sprintMap = sprintsInDb.ToDictionary(s => s.JiraId);

                foreach (var project in projects)
                {
                    try
                    {
                        _logger.LogDebug("Syncing tasks for project {ProjectKey}", project.Key);
                        var jiraTasks = await _adapter.GetProjectTasksAsync(project.Key, ct);
                        _logger.LogDebug("Retrieved {TaskCount} tasks for project {ProjectKey} from Jira", jiraTasks.Count, project.Key);

                        var existingTasksForProject = await _dbContext.Tasks
                            .Where(t => t.ProjectId == project.Id)
                            .ToDictionaryAsync(t => t.Key, ct);

                        foreach (var jiraTask in jiraTasks)
                        {
                            try
                            {
                                existingTasksForProject.TryGetValue(jiraTask.Key, out var task);

                                // Resolve the local Sprint ID (Guid) from the Jira Sprint ID (int)
                                Guid? localSprintId = null;
                                if (jiraTask.CurrentSprintJiraId.HasValue && sprintMap.TryGetValue(jiraTask.CurrentSprintJiraId.Value, out var localSprint))
                                {
                                    localSprintId = localSprint.Id; // This is the local GUID ID
                                }

                                if (task == null)
                                {
                                    // Create a new ProjectTask instance
                                    task = ProjectTask.Create(
                                        taskKey: jiraTask.Key,
                                        title: jiraTask.Title,
                                        projectId: project.Id,
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        sprintId: localSprintId // Pass the local GUID Sprint ID here
                                    );

                                    // Update other details from Jira
                                    task.UpdateFromJira(
                                        title: jiraTask.Title,
                                        description: jiraTask.Description,
                                        jiraStatusName: jiraTask.Status ?? "Unknown",
                                        assigneeAccountId: jiraTask.AssigneeId,
                                        assigneeDisplayName: jiraTask.AssigneeName,
                                        dueDate: jiraTask.DueDate,
                                        storyPoints: jiraTask.StoryPoints,
                                        timeEstimateMinutes: jiraTask.TimeEstimateMinutes,
                                        jiraUpdatedDate: jiraTask.UpdatedDate, // Pass Jira's updated date
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        epicKey: jiraTask.EpicKey,
                                        parentKey: jiraTask.ParentKey,
                                        jiraSprintId: jiraTask.CurrentSprintJiraId,
                                        localSprintId: localSprintId
                                    );

                                    _dbContext.Tasks.Add(task);
                                    createdCount++;
                                    _logger.LogDebug("Added new task {TaskKey}", jiraTask.Key);
                                }
                                else
                                {
                                    // The `ProjectTask.UpdateDetails` method in your entity is designed for internal changes.
                                    // When syncing from Jira, you should *always* use `UpdateFromJira`
                                    // to ensure all Jira-provided fields (including `UpdatedDate`) are refreshed.
                                    // The check `if (task.Status != newStatus)` should ideally be inside `UpdateFromJira`
                                    // if you want to track `StatusChangedDate` from Jira's perspective.
                                    // For simplicity here, we ensure `UpdateFromJira` is called.

                                    task.UpdateFromJira(
                                        title: jiraTask.Title,
                                        description: jiraTask.Description,
                                        jiraStatusName: jiraTask.Status ?? "Unknown",
                                        assigneeAccountId: jiraTask.AssigneeId,
                                        assigneeDisplayName: jiraTask.AssigneeName,
                                        dueDate: jiraTask.DueDate,
                                        storyPoints: jiraTask.StoryPoints,
                                        timeEstimateMinutes: jiraTask.TimeEstimateMinutes,
                                        jiraUpdatedDate: jiraTask.UpdatedDate, // Pass Jira's updated date
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        epicKey: jiraTask.EpicKey,
                                        parentKey: jiraTask.ParentKey,
                                        jiraSprintId: jiraTask.CurrentSprintJiraId,
                                        localSprintId: localSprintId
                                    );

                                    updatedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing task {TaskKey} for project {ProjectKey}", jiraTask.Key, project.Key);
                            }
                        }

                        var jiraTaskKeys = jiraTasks.Select(jt => jt.Key).ToHashSet();
                        var tasksToRemove = existingTasksForProject.Keys.Except(jiraTaskKeys).ToList();

                        foreach (var keyToRemove in tasksToRemove)
                        {
                            if (existingTasksForProject.TryGetValue(keyToRemove, out var taskToDelete))
                            {
                                _dbContext.Tasks.Remove(taskToDelete);
                                _logger.LogDebug("Removed task {TaskKey} from DB (no longer in Jira for project {ProjectKey})", keyToRemove, project.Key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing tasks for project {ProjectKey}", project.Key);
                    }
                }

                var changes = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Task sync completed. {ChangesCount} changes saved", changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete task sync");
                throw;
            }

            return (createdCount, updatedCount);
        }
    }
}