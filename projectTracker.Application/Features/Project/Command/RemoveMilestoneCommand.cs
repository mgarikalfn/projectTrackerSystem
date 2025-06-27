
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces; 
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Project.Command
{
    public class RemoveMilestoneCommand : IRequest<Result>
    {
        public string MilestoneId { get; set; } = default!; 
    }

    public class RemoveMilestoneCommandHandler : IRequestHandler<RemoveMilestoneCommand, Result>
    {
        private readonly IRepository<Milestone> _milestoneRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveMilestoneCommandHandler(IRepository<Milestone> milestoneRepository, IUnitOfWork unitOfWork)
        {
            _milestoneRepository = milestoneRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(RemoveMilestoneCommand request, CancellationToken cancellationToken)
        {
            var milestoneToRemove = await _milestoneRepository.GetByIdAsync(request.MilestoneId);

            if (milestoneToRemove == null)
            {
                
                return Result.Fail($"Milestone with ID '{request.MilestoneId}' not found.");
            }

            _milestoneRepository.DeleteAsync(milestoneToRemove);

            await _unitOfWork.SaveChangesAsync(); 

            return Result.Ok(); 
        }
    }
}