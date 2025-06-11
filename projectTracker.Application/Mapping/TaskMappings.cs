using projectTracker.Application.Dto;
using projectTracker.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace projectTracker.Application.Mapping;

/// <summary>
/// Converts <see cref="ProjectTask"/> entities → <see cref="TaskDto"/>s.
/// Keeps mapping logic out of controllers / handlers.
/// </summary>
public static class TaskMappings
{
    /// <summary>
    /// Single-item projection.
    /// </summary>
    public static TaskDto ToDto(this ProjectTask task)
    {
        if (task is null) throw new ArgumentNullException(nameof(task));

        return new TaskDto
        {
            Key = task.Key,
            Title = task.Summary,                // Domain “Summary” → DTO “Title”
            Description = task.Description,
            Status = task.Status.ToString(),      // Enum → string
            AssigneeId = task.AssigneeId,
            AssigneeName = task.AssigneeName,
            CreatedDate = task.CreatedDate,
            UpdatedDate = task.UpdatedDate,
            DueDate = task.DueDate,
            StoryPoints = (int?)task.StoryPoints ?? 0, // null-safe cast decimal? → int
            Priority = null                         // Not in domain entity (reserve for later)
        };
    }

    /// <summary>
    /// Convenience for `IEnumerable<ProjectTask>`.
    /// Lets you write `tasks.ToDtoList()` in handlers.
    /// </summary>
    public static IReadOnlyList<TaskDto> ToDtoList(this IEnumerable<ProjectTask> tasks) =>
        tasks?.Select(ToDto).ToList() ?? new List<TaskDto>();
}
