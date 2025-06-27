using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Entities;
using projectTracker.Domain.ValueObjects;
using ProjectTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Identity; // REQUIRED for UserManager and RoleManager

namespace projectTracker.Infrastructure.Sync
{
    public class SyncManager : ISyncManager
    {
        private readonly AppDbContext _dbContext;
        private readonly IProjectManegementAdapter _adapter;
        private readonly ILogger<SyncManager> _logger;
        private readonly IRiskCalculatorService _riskCalculator;
        private readonly UserManager<AppUser> _userManager; // REQUIRED
        private readonly RoleManager<UserRole> _roleManager; // REQUIRED

        public SyncManager(
            AppDbContext dbContext,
            IProjectManegementAdapter adapter,
            ILogger<SyncManager> logger,
            IRiskCalculatorService riskCalculator,
            UserManager<AppUser> userManager, // REQUIRED
            RoleManager<UserRole> roleManager) // REQUIRED
        {
            _dbContext = dbContext;
            _adapter = adapter;
            _logger = logger;
            _riskCalculator = riskCalculator;
            _userManager = userManager; // Assign
            _roleManager = roleManager; // Assign
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
            await _dbContext.SaveChangesAsync(ct); // Save sync history record immediately

            int totalTasksProcessed = 0;
            int totalTasksCreated = 0;
            int totalTasksUpdated = 0;

            try
            {
                // CRITICAL ORDERING FOR DEPENDENCIES:
                // 1. Users (mostly independent, but assignees for tasks)
                // 2. Projects (top-level, needed by Boards)
                // 3. Boards & Sprints (Boards need Projects; Sprints need Boards)
                // 4. Tasks (need Projects, Sprints, Assignees)

                _logger.LogInformation("Executing User Synchronization...");
                await SyncUsersAsync(ct);

                _logger.LogInformation("Executing Project Synchronization...");
                await SyncProjectsAsync(ct);

                _logger.LogInformation("Executing Board and Sprint Synchronization...");
                await SyncBoardsAndSprintsAsync(ct);

                _logger.LogInformation("Executing Task Synchronization...");
                var taskSyncCounts = await SyncTasksAsync(ct);
                totalTasksCreated = taskSyncCounts.Created;
                totalTasksUpdated = taskSyncCounts.Updated;
                totalTasksProcessed = totalTasksCreated + totalTasksUpdated;

                syncHistory.Complete(totalTasksCreated, totalTasksUpdated);

                // Final save for any remaining changes and sync history updates
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Full sync completed successfully. Created: {Created}, Updated: {Updated}", totalTasksCreated, totalTasksUpdated);
            }
            catch (Exception ex)
            {
                syncHistory.Fail(ex.Message);
                await _dbContext.SaveChangesAsync(ct); // Save failure status
                _logger.LogError(ex, "Failed to complete full sync");
                throw; // Re-throw to propagate the error up
            }
        }

        private async Task SyncUsersAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting user sync...");
            try
            {
                var jiraUsers = await _adapter.GetAppUsersAsync(ct);
                _logger.LogDebug("Retrieved {UserCount} users from Jira", jiraUsers.Count);

                // Fetch existing users only once for efficiency
                var existingUsers = await _dbContext.Users.AsNoTracking().ToListAsync(ct);
                var existingUserMapByEmail = existingUsers.ToDictionary(u => u.Email, StringComparer.OrdinalIgnoreCase);
                var existingUserMapByAccountId = existingUsers
                    .Where(u => !string.IsNullOrEmpty(u.AccountId))
                    .ToDictionary(u => u.AccountId, StringComparer.OrdinalIgnoreCase);

                foreach (var jiraUser in jiraUsers)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(jiraUser.Email))
                        {
                            _logger.LogWarning("Skipping Jira user {AccountId} with empty email. Cannot sync without a valid email address.", jiraUser.AccountId);
                            continue;
                        }

                        AppUser? user = null; // Use nullable reference type
                        bool isNewUser = false;

                        // Try to find by email first (more reliable for Identity)
                        if (existingUserMapByEmail.TryGetValue(jiraUser.Email, out var foundByEmail))
                        {
                            user = foundByEmail;
                            _logger.LogDebug("Found existing user by email: {Email}", jiraUser.Email);
                        }
                        // Then try by AccountId if email didn't yield a match
                        else if (!string.IsNullOrEmpty(jiraUser.AccountId) && existingUserMapByAccountId.TryGetValue(jiraUser.AccountId, out var foundByAccountId))
                        {
                            user = foundByAccountId;
                            _logger.LogDebug("Found existing user by AccountId: {AccountId}", jiraUser.AccountId);
                        }

                        if (user == null)
                        {
                            // Create new AppUser
                            user = new AppUser
                            {
                                UserName = jiraUser.Email, // Identity prefers UserName to be email for login
                                Email = jiraUser.Email,
                                EmailConfirmed = true, // Assume confirmed if coming from Jira
                                AccountId = jiraUser.AccountId,
                                DisplayName = jiraUser.DisplayName,
                                AvatarUrl = jiraUser.AvatarUrl,
                                IsActive = jiraUser.Active,
                                Source = UserSource.Jira, // Mark as Jira-synced
                                FirstName = string.Empty, // Jira doesn't always provide first/last
                                LastName = string.Empty,
                                //TimeZone = jiraUser.TimeZone ?? string.Empty,
                                //Location = jiraUser.Location ?? string.Empty
                            };

                            // Use UserManager to create the user, which handles password hashing etc.
                            // Pass a dummy password as Jira users authenticate via Jira/SSO.
                            // Identity still requires a password for CreateAsync.
                            var createResult = await _userManager.CreateAsync(user, "TempPass!123"); // Dummy password
                            if (!createResult.Succeeded)
                            {
                                _logger.LogError("Failed to create new Jira user {Email}: {Errors}", jiraUser.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                                continue; // Skip to next user if creation failed
                            }
                            isNewUser = true;
                            _logger.LogDebug("Added new Jira user: {Email}", jiraUser.Email);
                        }
                        else
                        {
                            // If user was found by AccountId but has a different email, and the Jira email is new.
                            // This might indicate an identity clash or email change. For now, we trust Jira's email.
                            if (user.Email != jiraUser.Email)
                            {
                                _logger.LogWarning("Existing user {ExistingEmail} (ID: {UserId}) has a different email than Jira user {JiraEmail} (Account ID: {JiraAccountId}). Updating local email to match Jira.",
                                    user.Email, user.Id, jiraUser.Email, jiraUser.AccountId);
                                user.Email = jiraUser.Email;
                                user.UserName = jiraUser.Email; // Update UserName as well
                            }

                            // If user was previously Local but is now found in Jira, update their Source.
                            if (user.Source == UserSource.Local)
                            {
                                _logger.LogInformation("User {Email} (ID: {UserId}) was locally created, but is now found in Jira. Changing source to Jira.", user.Email, user.Id);
                            }

                            // Update Jira-managed fields. Local-only fields (FirstName, LastName) are preserved.
                            user.AccountId = jiraUser.AccountId;
                            user.DisplayName = jiraUser.DisplayName;
                            user.AvatarUrl = jiraUser.AvatarUrl;
                            user.IsActive = jiraUser.Active;
                            user.EmailConfirmed = true; // Ensure confirmed if synced from Jira
                            user.Source = UserSource.Jira; // IMPORTANT: Mark as Jira source now
                            //user.TimeZone = jiraUser.TimeZone ?? user.TimeZone;
                            //user.Location = jiraUser.Location ?? user.Location;

                            // Use UserManager to update the user
                            var updateResult = await _userManager.UpdateAsync(user);
                            if (!updateResult.Succeeded)
                            {
                                _logger.LogError("Failed to update user {Email} (ID: {UserId}): {Errors}", user.Email, user.Id, string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                                continue;
                            }
                            _logger.LogDebug("Updated existing user: {Email} (now marked as Jira source)", jiraUser.Email);
                        }

                        // Assign default role only if it's a NEW Jira-synced user (or if they have no roles yet)
                        if (isNewUser)
                        {
                            var defaultRole = "Team Member";
                            if (await _roleManager.RoleExistsAsync(defaultRole))
                            {
                                if (!await _userManager.IsInRoleAsync(user, defaultRole))
                                {
                                    await _userManager.AddToRoleAsync(user, defaultRole);
                                    _logger.LogDebug("Assigned '{DefaultRole}' role to new Jira user {Email}", defaultRole, jiraUser.Email);
                                }
                            }
                            else
                            {
                                _logger.LogWarning("Default role '{DefaultRole}' does not exist. Please create it in your Identity system. User {Email} was not assigned this role.", defaultRole, jiraUser.Email);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Jira user {AccountId} ({Email}) during sync. Skipping this user.", jiraUser.AccountId, jiraUser.Email);
                    }
                }
                // No final SaveChangesAsync here for users, as UserManager.CreateAsync/UpdateAsync handles saves
                _logger.LogInformation("User sync completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete user sync.");
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

                if (!jiraProjects.Any())
                {
                    _logger.LogInformation("No projects found from Jira. Skipping project sync.");
                    return;
                }

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
                            // The new strategic fields will be initialized to their defaults (e.g., NotStarted, null)
                            );
                            _dbContext.Projects.Add(project);
                            _logger.LogDebug("Added new project {ProjectKey}", project.Key);
                        }
                        else
                        {
                            // --- FIX APPLIED HERE: Call the new UpdateJiraSyncedDetails method ---
                            project.UpdateJiraSyncedDetails(
                                name: projectDto.Name,
                                description: projectDto.Description,
                                leadName: projectDto.LeadName
                            );
                            _logger.LogDebug("Updated existing project {ProjectKey} with Jira synced details", project.Key);
                        }

                        // Fetch and update project metrics/health (assuming your Project has these fields)
                        var metricsDto = await _adapter.GetProjectMetricsAsync(projectDto.Key, ct);
                        if (metricsDto != null)
                        {
                            var metrics = new ProgressMetrics(
                                totalTasks: metricsDto.TotalTasks,
                                completedTasks: metricsDto.CompletedTasks,
                                storyPointsCompleted: metricsDto.CompletedStoryPoints,
                                storyPointsTotal: metricsDto.TotalStoryPoints,
                                activeBlockers: metricsDto.ActiveBlockers,
                                recentUpdates: metricsDto.RecentUpdates);

                            project.UpdateProgressMetrics(metrics);

                            var health = _riskCalculator.Calculate(metricsDto); // Ensure health calculation uses metricsDto
                            project.UpdateHealthMetrics(health);
                            _logger.LogDebug("Updated metrics and health for project {ProjectKey}", project.Key);
                        }
                        else
                        {
                            _logger.LogWarning("No metrics found for project {ProjectKey}. Skipping metrics/health update.", project.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing project {ProjectKey} during sync. Skipping this project.", projectDto.Key);
                    }
                }

                var projectChanges = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Project sync completed. {ChangesCount} changes saved", projectChanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete project sync.");
                throw; // Re-throw if it's a critical error that should stop the sync process
            }
        }

        private async Task SyncBoardsAndSprintsAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting board and sprint sync...");
            try
            {
                // Pre-fetch all projects to quickly get their local IDs by Jira Key
                var projectsInDb = await _dbContext.Projects.ToDictionaryAsync(p => p.Key, ct);
                _logger.LogDebug("Found {ProjectCount} projects in local DB for board linking.", projectsInDb.Count);

                if (!projectsInDb.Any())
                {
                    _logger.LogWarning("No projects found in local DB. Boards and Sprints cannot be linked without projects. Skipping board and sprint sync.");
                    return; // Cannot link boards without projects
                }

                var jiraBoards = await _adapter.GetBoardsAsync(ct);
                _logger.LogDebug("Retrieved {BoardCount} boards from Jira for sync.", jiraBoards.Count);

                if (!jiraBoards.Any())
                {
                    _logger.LogInformation("No boards found from Jira. Skipping board and sprint sync.");
                    return; // Exit early if no boards to process
                }

                foreach (var boardDto in jiraBoards)
                {
                    _logger.LogDebug("Processing Jira Board: Name={BoardName}, JiraId={JiraId}, Type={BoardType}, ProjectKey={ProjectKey}",
                        boardDto.Name, boardDto.Id, boardDto.Type, boardDto.Location?.ProjectKey ?? "N/A_No_Location"); // <-- UPDATED LOGGING

                    // Resolve the local ProjectId from the Jira ProjectKey in the BoardDto
                    // CRITICAL: Access the ProjectKey from the Location property
                    if (!projectsInDb.TryGetValue(boardDto.Location?.ProjectKey ?? string.Empty, out var associatedProject)) // <-- UPDATED ACCESS
                    {
                        _logger.LogWarning("Skipping board '{BoardName}' (Jira ID: {JiraId}) because its associated project '{ProjectKey}' was not found locally. Ensure projects are synced first and ProjectKey is correctly mapped in JiraBoardDto's Location property.",
                            boardDto.Name, boardDto.Id, boardDto.Location?.ProjectKey ?? string.Empty); // <-- UPDATED LOGGING
                        continue;
                    }

                    try
                    {
                        var board = await _dbContext.Boards
                            .FirstOrDefaultAsync(b => b.JiraId == boardDto.Id, ct);

                        if (board == null)
                        {
                            // Pass the resolved ProjectId when creating the Board
                            board = new Board(boardDto.Id, boardDto.Name, boardDto.Type, associatedProject.Id);
                            _dbContext.Boards.Add(board);
                            _logger.LogDebug("Added NEW board {BoardName} (Jira ID: {JiraId}) for project {ProjectKey}", board.Name, board.JiraId, associatedProject.Key);
                        }
                        else
                        {
                            board.UpdateDetails(boardDto.Name, boardDto.Type);
                            // It's generally not recommended to change ProjectId of an existing board directly
                            // unless Jira API indicates a board can change projects.
                            // If board.ProjectId differs, it means the board changed projects in Jira.
                            // if (board.ProjectId != associatedProject.Id) { board.ProjectId = associatedProject.Id; _logger.LogInformation("Board {BoardName} (Jira ID: {JiraId}) moved to new project {NewProjectKey}", board.Name, board.JiraId, associatedProject.Key); }
                            _logger.LogDebug("Updated EXISTING board {BoardName} (Jira ID: {JiraId}) for project {ProjectKey}", board.Name, board.JiraId, associatedProject.Key);
                        }

                        // Save board changes IMMEDIATELY to ensure board.Id is in DB for sprint FKs
                        var boardChanges = await _dbContext.SaveChangesAsync(ct);
                        _logger.LogDebug("Board {BoardName} (Jira ID: {JiraId}) saved to DB. Changes: {Changes}", board.Name, board.JiraId, boardChanges);

                        if (board.Id == Guid.Empty) // Sanity check, should not happen after SaveChanges
                        {
                            _logger.LogError("Board {BoardName} (Jira ID: {JiraId}) has an empty local ID after saving. Cannot sync sprints.", board.Name, board.JiraId);
                            continue; // Skip sprints if board couldn't be saved properly
                        }

                        // Now, sync sprints *only if* it's a scrum board
                        if (boardDto.Type.Equals("scrum", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogDebug("Fetching sprints for scrum board '{BoardName}' (Jira ID: {JiraId}).", board.Name, board.JiraId);
                            var jiraSprints = await _adapter.GetSprintsForBoardAsync(boardDto.Id, ct);
                            _logger.LogDebug("Retrieved {SprintCount} sprints for board {BoardName}", jiraSprints.Count, boardDto.Name);

                            if (!jiraSprints.Any())
                            {
                                _logger.LogInformation("No sprints found from Jira for board '{BoardName}' (Jira ID: {JiraId}).", board.Name, board.JiraId);
                            }

                            foreach (var sprintDto in jiraSprints)
                            {
                                try
                                {
                                    _logger.LogDebug("Processing Jira Sprint: Name={SprintName}, JiraId={JiraId}, State={State}",
                                        sprintDto.Name, sprintDto.Id, sprintDto.State);

                                    var sprint = await _dbContext.Sprints
                                        .FirstOrDefaultAsync(s => s.JiraId == sprintDto.Id, ct);

                                    if (sprint == null)
                                    {
                                        sprint = new Sprint(
                                            sprintDto.Id, board.Id, sprintDto.Name, sprintDto.State, // Use board.Id here
                                            sprintDto.StartDate, sprintDto.EndDate, sprintDto.CompleteDate, sprintDto.Goal);
                                        _dbContext.Sprints.Add(sprint);
                                        _logger.LogDebug("Added NEW sprint {SprintName} (Jira ID: {JiraId}) for board {BoardName}", sprint.Name, sprint.JiraId, board.Name);
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
                                        _logger.LogDebug("Updated EXISTING sprint {SprintName} (Jira ID: {JiraId}) for board {BoardName}", sprint.Name, sprint.JiraId, board.Name);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error processing sprint {SprintId} for board {BoardId}. Skipping this sprint.", sprintDto.Id, board.Id);
                                }
                            }
                            // Save sprint changes after processing all sprints for this board
                            var sprintChanges = await _dbContext.SaveChangesAsync(ct);
                            _logger.LogDebug("Sprints for board {BoardName} (Jira ID: {JiraId}) saved to DB. Changes: {Changes}", board.Name, board.JiraId, sprintChanges);
                        }
                        else
                        {
                            _logger.LogDebug("Board '{BoardName}' (Jira ID: {JiraId}) is not a scrum board. Skipping sprint fetching.", board.Name, board.JiraId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing board {BoardId} during sync. Board or its sprints might not be retrievable. Skipping this board.", boardDto.Id);
                    }
                }
                _logger.LogInformation("Board and sprint sync completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete board and sprint sync due to an unhandled error.");
                throw; // Re-throw to be caught by the main SyncAsync try-catch
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

                // Pre-fetch all sprints for efficient lookup
                var sprintsInDb = await _dbContext.Sprints.AsNoTracking().ToListAsync(ct);
                var sprintMap = sprintsInDb.ToDictionary(s => s.JiraId);
                _logger.LogDebug("Found {SprintCount} sprints in local DB for task linking.", sprintMap.Count);

                // --- NEW/MODIFIED: Pre-fetch all AppUsers by AccountId for efficient lookup ---
                // Only fetch users that have an AccountId (Jira-sourced)
                var usersByAccountId = await _dbContext.Users
                                                    .Where(u => u.AccountId != null && u.Source == UserSource.Jira)
                                                    .AsNoTracking()
                                                    .ToDictionaryAsync(u => u.AccountId!, ct); // Use AccountId as key
                _logger.LogDebug("Found {UserCount} Jira-sourced users in local DB for assignee linking.", usersByAccountId.Count);
                // --- END NEW/MODIFIED ---


                if (!projects.Any())
                {
                    _logger.LogWarning("No projects found in local DB. Skipping task sync.");
                    return (0, 0); // No projects, no tasks to sync
                }

                foreach (var project in projects)
                {
                    _logger.LogDebug("Syncing tasks for project {ProjectKey}", project.Key);
                    try
                    {
                        var jiraTasks = await _adapter.GetProjectTasksAsync(project.Key, ct);
                        _logger.LogDebug("Retrieved {TaskCount} tasks for project {ProjectKey} from Jira", jiraTasks.Count, project.Key);

                        if (!jiraTasks.Any())
                        {
                            _logger.LogInformation("No tasks found from Jira for project {ProjectKey}. Skipping task sync for this project.", project.Key);
                            continue;
                        }

                        // Fetch existing tasks for this specific project only (AsNoTracking is good for read)
                        var existingTasksForProject = await _dbContext.Tasks
                            .Where(t => t.ProjectId == project.Id)
                            .AsNoTracking()
                            .ToDictionaryAsync(t => t.Key, ct);

                        foreach (var jiraTask in jiraTasks)
                        {
                            _logger.LogDebug("Processing Jira Task: Key={TaskKey}, Project={ProjectKey}", jiraTask.Key, project.Key);
                            try
                            {
                                // IMPORTANT: Use LoadByIdAsync or another method to get a TRACKED entity for updates
                                // If existingTasksForProject was AsNoTracking, you need to re-attach or re-fetch.
                                // For simplicity, I'll fetch the tracked entity here if it exists.
                                ProjectTask? task = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Key == jiraTask.Key && t.ProjectId == project.Id, ct);

                                // --- NEW: Resolve the local AppUser ID for the assignee ---
                                AppUser? resolvedAssignee = null;
                                if (!string.IsNullOrEmpty(jiraTask.AssigneeId))
                                {
                                    if (usersByAccountId.TryGetValue(jiraTask.AssigneeId, out var existingUser))
                                    {
                                        resolvedAssignee = existingUser;
                                        _logger.LogDebug("Resolved assignee {JiraAccountId} to local user ID {LocalUserId}", jiraTask.AssigneeId, resolvedAssignee.Id);
                                    }
                                    else
                                    {
                                        // User not found by AccountId, create a new Jira-sourced user
                                        _logger.LogWarning("Jira assignee {JiraAccountId} not found locally. Creating new user.", jiraTask.AssigneeId);
                                        resolvedAssignee = new AppUser
                                        {
                                            Id = Guid.NewGuid().ToString(), // Generate a new local ID
                                            AccountId = jiraTask.AssigneeId,
                                            DisplayName = jiraTask.AssigneeName ?? jiraTask.AssigneeId,
                                            FirstName = "", // Populate as much as possible, defaults if not available
                                            LastName = "",
                                           // Email = jiraTask., // Assuming JiraTaskData has Email
                                           // UserName = jiraTask.AssigneeEmail ?? jiraTask.AssigneeAccountId,
                                            Source = UserSource.Jira, // Mark as Jira-sourced
                                            IsActive = true,
                                            TimeZone = "Etc/UTC", // Sensible default
                                            Location = "Unknown", // Sensible default
                                            // Other properties...
                                        };
                                        _dbContext.Users.Add(resolvedAssignee);
                                        // Add to map so it's available for subsequent tasks in this sync run
                                        usersByAccountId.Add(resolvedAssignee.AccountId!, resolvedAssignee);
                                    }
                                }
                                // --- END NEW ---

                                // Resolve the local Sprint ID (Guid) from the Jira Sprint ID (int)
                                Guid? localSprintId = null;
                                if (jiraTask.CurrentSprintJiraId.HasValue)
                                {
                                    if (sprintMap.TryGetValue(jiraTask.CurrentSprintJiraId.Value, out var localSprint))
                                    {
                                        localSprintId = localSprint.Id;
                                        _logger.LogDebug("Resolved local Sprint ID {LocalSprintId} for Jira Sprint ID {JiraSprintId}", localSprintId, jiraTask.CurrentSprintJiraId.Value);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Local sprint not found for Jira Sprint ID {JiraSprintId} (Task: {TaskKey}). Local SprintId will be null.", jiraTask.CurrentSprintJiraId.Value, jiraTask.Key);
                                    }
                                }
                                else
                                {
                                    _logger.LogDebug("Jira task {TaskKey} has no associated Jira Sprint ID.", jiraTask.Key);
                                }


                                if (task == null)
                                {
                                    // Create a new ProjectTask instance
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

                                    // Ensure all fields are updated using UpdateFromJira (signature unchanged)
                                    task.UpdateFromJira(
                                        title: jiraTask.Title,
                                        description: jiraTask.Description,
                                        jiraStatusName: jiraTask.Status ?? "Unknown",
                                        assigneeAccountId: jiraTask.AssigneeId, // Still passing Jira Account ID here
                                        assigneeDisplayName: jiraTask.AssigneeName,
                                        dueDate: jiraTask.DueDate,
                                        storyPoints: jiraTask.StoryPoints,
                                        timeEstimateMinutes: jiraTask.TimeEstimateMinutes,
                                        jiraUpdatedDate: jiraTask.UpdatedDate,
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        epicKey: jiraTask.EpicKey,
                                        parentKey: jiraTask.ParentKey,
                                        priority: jiraTask.Priority,
                                        //labels: jiraTask.Labels, // Pass labels
                                        jiraSprintId: jiraTask.CurrentSprintJiraId,
                                        localSprintId: localSprintId
                                        //currentSprintName: jiraTask.CurrentSprintName, // Pass current sprint name
                                       // currentSprintState: jiraTask.CurrentSprintState // Pass current sprint state
                                    );

                                    // --- NEW: Call SetAssigneeUser after UpdateFromJira ---
                                    task.SetAssigneeUser(resolvedAssignee?.Id, resolvedAssignee?.DisplayName);
                                    // --- END NEW ---

                                    _dbContext.Tasks.Add(task);
                                    createdCount++;
                                    _logger.LogDebug("Added new task {TaskKey} for project {ProjectKey}. SprintId: {SprintId}, AssigneeId: {AssigneeId}", jiraTask.Key, project.Key, localSprintId, resolvedAssignee?.Id);
                                }
                                else
                                {
                                    // Update existing task
                                    // Make sure it's a tracked entity (if it was from existingTasksForProject.AsNoTracking(), it won't be)
                                    // The FirstOrDefaultAsync above handles this.

                                    task.UpdateFromJira(
                                        title: jiraTask.Title,
                                        description: jiraTask.Description,
                                        jiraStatusName: jiraTask.Status ?? "Unknown",
                                        assigneeAccountId: jiraTask.AssigneeId, // Still passing Jira Account ID here
                                        assigneeDisplayName: jiraTask.AssigneeName,
                                        dueDate: jiraTask.DueDate,
                                        storyPoints: jiraTask.StoryPoints,
                                        timeEstimateMinutes: jiraTask.TimeEstimateMinutes,
                                        jiraUpdatedDate: jiraTask.UpdatedDate,
                                        issueType: jiraTask.IssueType ?? "Unknown",
                                        epicKey: jiraTask.EpicKey,
                                        parentKey: jiraTask.ParentKey,
                                        priority: jiraTask.Priority,
                                      //  labels: jiraTask.Labels, // Pass labels
                                        jiraSprintId: jiraTask.CurrentSprintJiraId,
                                        localSprintId: localSprintId
                                       // currentSprintName: jiraTask.CurrentSprintName,
                                       // currentSprintState: jiraTask.CurrentSprintState
                                    );

                                    // --- NEW: Call SetAssigneeUser after UpdateFromJira ---
                                    task.SetAssigneeUser(resolvedAssignee?.Id, resolvedAssignee?.DisplayName);
                                    // --- END NEW ---

                                    // _dbContext.Tasks.Update(task); // Not strictly necessary if 'task' is already tracked
                                    updatedCount++;
                                    _logger.LogDebug("Updated existing task {TaskKey} for project {ProjectKey}. SprintId: {SprintId}, AssigneeId: {AssigneeId}", jiraTask.Key, project.Key, localSprintId, resolvedAssignee?.Id);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing task {TaskKey} for project {ProjectKey}. Skipping this task.", jiraTask.Key, project.Key);
                            }
                        }

                        // Remove tasks that are in local DB but no longer in Jira for this project
                        var jiraTaskKeys = jiraTasks.Select(jt => jt.Key).ToHashSet();
                        var tasksToRemove = existingTasksForProject.Keys.Except(jiraTaskKeys).ToList();

                        foreach (var keyToRemove in tasksToRemove)
                        {
                            // Need to load the tracked entity to remove it
                            var taskToDelete = await _dbContext.Tasks.FirstOrDefaultAsync(t => t.Key == keyToRemove && t.ProjectId == project.Id, ct);
                            if (taskToDelete != null)
                            {
                                _dbContext.Tasks.Remove(taskToDelete);
                                _logger.LogDebug("Removed task {TaskKey} from DB (no longer in Jira for project {ProjectKey})", keyToRemove, project.Key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error syncing tasks for project {ProjectKey}. Skipping this project's tasks.", project.Key);
                    }
                }

                var changes = await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Task sync completed. {ChangesCount} changes saved", changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete task sync.");
                throw;
            }

            return (createdCount, updatedCount);
        }
    }
}