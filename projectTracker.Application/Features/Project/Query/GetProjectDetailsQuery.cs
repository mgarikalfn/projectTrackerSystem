using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using projectTracker.Application.Dto.Project.MileStone;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Interfaces;
using FluentResults;
using projectTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace projectTracker.Application.Features.Project.Query
{
    public class GetProjectDetailsQuery : IRequest<Result<ProjectDetailDto>>
    {
        public string ProjectId { get; set; } = default!;
    }

    public class GetProjectDetailsQueryHandler : IRequestHandler<GetProjectDetailsQuery, Result<ProjectDetailDto>>
    {
        private readonly IRepository<Domain.Aggregates.Project> _projectRepository;

        public GetProjectDetailsQueryHandler(IRepository<Domain.Aggregates.Project> projectRepository)
        {
            _projectRepository = projectRepository;
        }

        public async Task<Result<ProjectDetailDto>> Handle(GetProjectDetailsQuery request, CancellationToken cancellationToken)
        {
            var project = await _projectRepository.GetQueryable()
                .Include(m => m.Milestones)
                .Include(m => m.Risks)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (project == null)
            {
                throw new ApplicationException($"Project with ID '{request.ProjectId}' not found.");
            }

            return new ProjectDetailDto
            {
                Id = project.Id,
                Key = project.Key,
                Name = project.Name,
                Description = project.Description,
                Lead = project.Lead,
                HealthLevel = project.Health.Level.ToString(),
                HealthReason = project.Health.Reason,
                TotalTasks = project.Progress.TotalTasks,
                CompletedTasks = project.Progress.CompletedTasks,
                StoryPointsTotal = project.Progress.StoryPointsTotal,
                StoryPointsCompleted = project.Progress.StoryPointsCompleted,
                ActiveBlockers = project.Progress.ActiveBlockers,
                OverallProjectStatus = project.OverallProjectStatus.ToString(),
                ExecutiveSummary = project.ExecutiveSummary,
                OwnerName = project.Owner?.Name,
                OwnerContactInfo = project.Owner?.ContactInfo,
                ProjectStartDate = project.ProjectStartDate,
                TargetEndDate = project.TargetEndDate,
                Milestones = project.Milestones.Select(milestone => new MilestoneDto
                {
                    id = milestone.Id,
                    Name = milestone.Name,
                    Description = milestone.Description,
                    DueDate = milestone.DueDate,
                    status = milestone.Status,
                    ProjectId = milestone.ProjectId
                }).ToList(),
                Risks = project.Risks.Select(r => new RiskDto
                (
                    id: r.Id,
                    description: r.Description,
                    impact: r.Impact.ToString(),
                    likelihood: r.Likelihood.ToString(),
                    mitigationPlan: r.MitigationPlan,
                    status: r.Status.ToString(),
                    projectId: r.ProjectId
                ).ToResult().Value).ToList()
            };
        }
    }
}

