
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces; 
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Features.Project.Command
{
    public class UpdateMilestoneCommand : IRequest<Result>
    {
        public string Id { get; set; } = default!; 
        public string Name { get; set; } = default!; 
        public DateTime DueDate { get; set; }
        public MilestoneStatus Status { get; set; }
        public string? Description { get; set; }
        
    }

    public class UpdateMileStoneCommandHandler : IRequestHandler<UpdateMilestoneCommand, Result>
    {
        private readonly IRepository<Milestone> _milestoneRepository; 
        private readonly IUnitOfWork _unitOfWork; // Inject Unit of Work

        public UpdateMileStoneCommandHandler(IRepository<Milestone> milestoneRepository, IUnitOfWork unitOfWork)
        {
            _milestoneRepository = milestoneRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateMilestoneCommand request, CancellationToken cancellationToken)
        {
            var milestone = await _milestoneRepository.GetByIdAsync(request.Id); 

            if (milestone == null)
            {
                return Result.Fail("Milestone not found.");
            }

            
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Result.Fail("Milestone name cannot be empty.");
            }
            if (request.DueDate == default)
            {
                return Result.Fail("Milestone due date is required.");
            }

           
            milestone.Update(
                name: request.Name,
                dueDate: request.DueDate,
                status: request.Status,
                description: request.Description
            );

            // The repository's UpdateAsync method (or just calling Update on DbContext's DbSet)
            // will track changes. No need to call it if EF Core tracks the retrieved entity.
            // If your IRepository has an explicit UpdateAsync that updates context, call it:
            await _milestoneRepository.UpdateAsync(milestone);

           
            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }
    }
}