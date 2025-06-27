// projectTracker.Application.Features.Project.Queries/GetMilestoneByIdQuery.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto.Project.MileStone; // Assuming MilestoneDto is here
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums; // For MilestoneStatus conversion

namespace projectTracker.Application.Features.Project.Queries
{
    public class GetMilestoneByIdQuery : IRequest<Result<MilestoneDto>>
    {
        public string MilestoneId { get; set; } = default!;
    }

    public class GetMilestoneByIdQueryHandler : IRequestHandler<GetMilestoneByIdQuery, Result<MilestoneDto>>
    {
        private readonly IRepository<Milestone> _milestoneRepository;

        public GetMilestoneByIdQueryHandler(IRepository<Milestone> milestoneRepository)
        {
            _milestoneRepository = milestoneRepository;
        }

        public async Task<Result<MilestoneDto>> Handle(GetMilestoneByIdQuery request, CancellationToken cancellationToken)
        {
            var milestone = await _milestoneRepository.GetByIdAsync(request.MilestoneId);

            if (milestone == null)
            {
                return Result.Fail($"Milestone with ID '{request.MilestoneId}' not found.");
            }

            var milestoneDto = new MilestoneDto
            {
                id = milestone.Id,
                Name = milestone.Name,
                Description = milestone.Description,
                DueDate = milestone.DueDate,
                status = milestone.Status,
                ProjectId = milestone.ProjectId
            };


            return Result.Ok(milestoneDto);
        }
    }
}