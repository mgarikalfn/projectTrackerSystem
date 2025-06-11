using projectTracker.Application.Dto.Project;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.ValueObjects;

namespace projectTracker.Application.Mapping;

/// <summary>
/// Central place for converting domain entities → DTOs.
/// Keeps controllers thin and avoids accidental duplication.
/// </summary>
public static class ProjectMappings
{
    /// <summary>
    /// Maps a populated <see cref="Project"/> aggregate into a
    /// wire-friendly <see cref="ProjectResponseDto"/>.
    /// </summary>
    public static ProjectResponseDto ToDto(this Project project)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));

        // ――― Map nested value objects first
        ProjectHealthDto MapHealth(ProjectHealth h) => new()
        {
            Level = (int)h.Level,   // enum → int
            Reason = h.Reason,
            Score = h.Score,
            Confidence = h.Confidence
        };

        ProgressMetricsDto MapProgress(ProgressMetrics p) => new()
        {
            TotalTasks = p.TotalTasks,
            CompletedTasks = p.CompletedTasks,
            StoryPointsCompleted = p.StoryPointsCompleted,
            StoryPointsTotal = p.StoryPointsTotal,
            ActiveBlockers = p.ActiveBlockers,
            RecentUpdates = p.RecentUpdates
        };

        // ――― Final DTO
        return new ProjectResponseDto
        {
            Id = project.Id,
            Key = project.Key,
            Name = project.Name,
            Description = project.Description,
            Lead = project.Lead,

            Health = MapHealth(project.Health),
            Progress = MapProgress(project.Progress),

            Critical = project.Critical
        };
    }
}
