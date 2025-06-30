using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using ProjectTracker.Infrastructure.Data;

namespace projectTracker.Infrastructure.Sync
{
    public class SyncBoardsAndSprints
    {
        private readonly AppDbContext _dbContext;
        private readonly IProjectManegementAdapter _adapter;
        private readonly ILogger<SyncBoardsAndSprints> _logger;

        public SyncBoardsAndSprints(
            AppDbContext dbContext,
            IProjectManegementAdapter adapter,
            ILogger<SyncBoardsAndSprints> logger)
        {
            _dbContext = dbContext;
            _adapter = adapter;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct)
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
    }
}
