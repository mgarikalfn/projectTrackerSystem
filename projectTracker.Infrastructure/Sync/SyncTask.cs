using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Domain.Entities;
using projectTracker.Infrastructure.Adapter;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Sync
{
    public class SyncTasks
    {
        private readonly ILogger<SyncTasks> _logger;
        private readonly AppDbContext _dbContext;
        private readonly JiraAdapter _adapter;

        public SyncTasks(
            ILogger<SyncTasks> logger,
            AppDbContext dbContext,
            JiraAdapter adapter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        public async Task<(int Created, int Updated)> ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting task sync...");
            int createdCount = 0;
            int updatedCount = 0;

            try
            {
                // Load all necessary data upfront to minimize DB queries
                var projects = await _dbContext.Projects.AsNoTracking().ToListAsync(ct);
                var sprintsInDb = await _dbContext.Sprints.AsNoTracking().ToListAsync(ct);
                var sprintMap = sprintsInDb.ToDictionary(s => s.JiraId);

                var usersByAccountId = await _dbContext.Users
                    .Where(u => u.AccountId != null && u.Source == UserSource.Jira)
                    .AsNoTracking()
                    .ToDictionaryAsync(u => u.AccountId!, ct);

                if (!projects.Any())
                {
                    _logger.LogWarning("No projects found in local DB. Skipping task sync.");
                    return (0, 0);
                }

                foreach (var project in projects)
                {
                    try
                    {
                        _logger.LogDebug("Syncing tasks for project {ProjectKey}", project.Key);
                        var jiraTasks = await _adapter.GetProjectTasksAsync(project.Key, ct);
                        _logger.LogDebug("Retrieved {TaskCount} tasks for project {ProjectKey}", jiraTasks.Count, project.Key);

                        if (!jiraTasks.Any())
                        {
                            _logger.LogInformation("No tasks found for project {ProjectKey}.", project.Key);
                            continue;
                        }

                        // Get existing tasks for this project
                        var existingTasks = await _dbContext.Tasks
                            .Where(t => t.ProjectId == project.Id)
                            .ToDictionaryAsync(t => t.Key, ct);

                        foreach (var jiraTask in jiraTasks)
                        {
                            try
                            {
                                // Try to find existing task
                                var task = existingTasks.TryGetValue(jiraTask.Key, out var existingTask)
                                    ? existingTask
                                    : null;

                                // Resolve assignee
                                AppUser? resolvedAssignee = null;
                                if (!string.IsNullOrEmpty(jiraTask.AssigneeId))
                                {
                                    if (usersByAccountId.TryGetValue(jiraTask.AssigneeId, out var existingUser))
                                    {
                                        resolvedAssignee = existingUser;
                                    }
                                    else
                                    {
                                        // Create stub user if not found
                                        resolvedAssignee = new AppUser
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            UserName = jiraTask.AssigneeId,
                                            Email = $"{jiraTask.AssigneeId}@jira-temp.invalid",
                                            AccountId = jiraTask.AssigneeId,
                                            DisplayName = jiraTask.AssigneeName ?? jiraTask.AssigneeId,
                                            Source = UserSource.Jira,
                                            IsActive = true,
                                            EmailConfirmed = true
                                        };
                                        _dbContext.Users.Add(resolvedAssignee);
                                        usersByAccountId.Add(resolvedAssignee.AccountId!, resolvedAssignee);
                                        _logger.LogDebug("Created temporary user for assignee {AssigneeId}", jiraTask.AssigneeId);
                                    }
                                }

                                // Resolve sprint
                                Guid? localSprintId = null;
                                if (jiraTask.CurrentSprintJiraId.HasValue &&
                                    sprintMap.TryGetValue(jiraTask.CurrentSprintJiraId.Value, out var sprint))
                                {
                                    localSprintId = sprint.Id;
                                }

                                if (task == null)
                                {
                                    // Create new task
                                    task = ProjectTask.Create(
                                        taskKey: jiraTask.Key,
                                        title: jiraTask.Title,
                                        projectId: project.Id,
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        jiraCreatedDate: jiraTask.CreatedDate,
                                        jiraUpdatedDate: jiraTask.UpdatedDate,
                                        priority: jiraTask.Priority,
                                        sprintId: localSprintId
                                    );

                                    task.UpdateFromJira(
                                        title: jiraTask.Title,
                                        description: jiraTask.Description,
                                        jiraStatusName: jiraTask.Status ?? "Unknown",
                                        assigneeAccountId: jiraTask.AssigneeId,
                                        assigneeDisplayName: jiraTask.AssigneeName,
                                        dueDate: jiraTask.DueDate,
                                        storyPoints: jiraTask.StoryPoints,
                                        timeEstimateMinutes: jiraTask.TimeEstimateMinutes,
                                        jiraUpdatedDate: jiraTask.UpdatedDate,
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        epicKey: jiraTask.EpicKey,
                                        parentKey: jiraTask.ParentKey,
                                        priority: jiraTask.Priority,
                                        jiraSprintId: jiraTask.CurrentSprintJiraId,
                                        localSprintId: localSprintId
                                    );

                                    task.SetAssigneeUser(resolvedAssignee?.Id, resolvedAssignee?.DisplayName);
                                    _dbContext.Tasks.Add(task);
                                    createdCount++;
                                    _logger.LogDebug("Created new task {TaskKey}", jiraTask.Key);
                                }
                                else
                                {
                                    // Update existing task
                                    task.UpdateFromJira(
                                        title: jiraTask.Title,
                                        description: jiraTask.Description,
                                        jiraStatusName: jiraTask.Status ?? "Unknown",
                                        assigneeAccountId: jiraTask.AssigneeId,
                                        assigneeDisplayName: jiraTask.AssigneeName,
                                        dueDate: jiraTask.DueDate,
                                        storyPoints: jiraTask.StoryPoints,
                                        timeEstimateMinutes: jiraTask.TimeEstimateMinutes,
                                        jiraUpdatedDate: jiraTask.UpdatedDate,
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        epicKey: jiraTask.EpicKey,
                                        parentKey: jiraTask.ParentKey,
                                        priority: jiraTask.Priority,
                                        jiraSprintId: jiraTask.CurrentSprintJiraId,
                                        localSprintId: localSprintId
                                    );

                                    task.SetAssigneeUser(resolvedAssignee?.Id, resolvedAssignee?.DisplayName);
                                    updatedCount++;
                                    _logger.LogDebug("Updated existing task {TaskKey}", jiraTask.Key);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing task {TaskKey} for project {ProjectKey}", jiraTask.Key, project.Key);
                            }
                        }

                        // Remove tasks that no longer exist in Jira
                        var jiraKeys = jiraTasks.Select(t => t.Key).ToHashSet();
                        var tasksToRemove = existingTasks.Values
                            .Where(t => !jiraKeys.Contains(t.Key))
                            .ToList();

                        if (tasksToRemove.Any())
                        {
                            _dbContext.Tasks.RemoveRange(tasksToRemove);
                            _logger.LogInformation("Removing {Count} obsolete tasks for project {ProjectKey}", tasksToRemove.Count, project.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing tasks for project {ProjectKey}", project.Key);
                    }
                }

                // Save all changes at once
                var changes = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Task sync completed. Created: {Created}, Updated: {Updated}, Total changes: {TotalChanges}",
                    createdCount, updatedCount, changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task sync failed.");
                throw;
            }

            return (createdCount, updatedCount);
        }
    }
}