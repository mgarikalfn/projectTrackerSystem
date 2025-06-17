using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Dto.Project;

namespace projectTracker.Application.Interfaces
{
    public interface IProjectReportingService
    {
        Task<ProjectSprintOverviewDto?> GetProjectSprintOverviewAsync(string projectKey, CancellationToken ct);
        Task<SprintReportDto?> GetSprintReportAsync(Guid sprintId, CancellationToken ct);
        Task<List<SprintReportDto>> GetAllSprintsForProjectAsync(string projectKey, CancellationToken ct);
    }
}
