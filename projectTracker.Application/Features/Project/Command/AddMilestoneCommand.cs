using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Dto.Project.MileStone;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;
using projectTracker.Domain.Aggregates;

namespace projectTracker.Application.Features.Project.Command
{
    public class AddMilestoneCommand : IRequest<Result<MilestoneDto>>
    {
        public string Name { get; set; }
        public DateTime DueDate { get;  set; }
        public MilestoneStatus Status { get; set; }
        public string? Description { get; set; }
        public string ProjectId { get; set; }
    }


    public class AddMiltestoneCommandHandler : IRequestHandler<AddMilestoneCommand, Result<MilestoneDto>>
    {
        private readonly IRepository<Milestone> _mileStoneRepository;
        private readonly IRepository<Domain.Aggregates.Project> _projectRepository;
        public AddMiltestoneCommandHandler(IRepository<Milestone> mileStoneRepository , IRepository<Domain.Aggregates.Project> projectRepository)
        {
            _mileStoneRepository = mileStoneRepository;
            _projectRepository = projectRepository;
        }

        public async Task<Result<MilestoneDto>> Handle(AddMilestoneCommand request, CancellationToken cancellationToken)
        {
            var existingMileStone = await _mileStoneRepository.GetQueryable()
                .AnyAsync(p => p.ProjectId == request.ProjectId && p.Name == request.Name, cancellationToken);

            if (existingMileStone)
            {
                return Result.Fail<MilestoneDto>("Milestone with this name already exists for this project.");
            }

            var validProjectId = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (validProjectId == null)
            {
                return Result.Fail<MilestoneDto>("Project ID not found");
            }

            var milestone = Milestone.Create(
                 name: request.Name,
                 dueDate: request.DueDate,
                 projectId: request.ProjectId,
                 description: request.Description,
                 status: request.Status
                );

            await _mileStoneRepository.AddAsync(milestone);
            var mileStoneDto = new MilestoneDto
            {
                id = milestone.Id,
                Name = milestone.Name,
                Description = milestone.Description,
                DueDate = milestone.DueDate,
                status = milestone.Status,
                ProjectId = milestone.ProjectId
            };

            return Result.Ok(mileStoneDto);
        }
    }
}
