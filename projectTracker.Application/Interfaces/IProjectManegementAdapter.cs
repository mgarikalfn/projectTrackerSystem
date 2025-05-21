using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Dto;

namespace projectTracker.Application.Interfaces
{
    public interface IProjectManegementAdapter
    {
        Task<List<ProjectDto>> GetProjectsAsync(CancellationToken ct);
        Task<List<TaskDto>> GetRecentTasksAsync(DateTime lastSyncTime, CancellationToken ct);
        Task<ProgressMetricsDto> GetProjectMetricsAsync(string projectKey, CancellationToken ct);
    }
}
