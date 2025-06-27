
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Features.Projects.Command
{
    public class UpdateProjectStrategicDetailsCommand : IRequest<Result>
    {
       public string Id { get; set; }
        public OverallProjectStatus OverallStatus { get; set; }
        public string? ExecutiveSummary { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerContact { get; set; }
        public DateTime? ProjectStartDate { get; set; }
        public DateTime? TargetEndDate { get; set; }
    }

    public class UpdateProjectStrategicDetailsCommandHandler : IRequestHandler<UpdateProjectStrategicDetailsCommand, Result>
    {
      
        private readonly IUnitOfWork _unitOfWork; // Assuming you have a Unit of Work

        public UpdateProjectStrategicDetailsCommandHandler(IUnitOfWork unitOfWork)
        {
            
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateProjectStrategicDetailsCommand request, CancellationToken cancellationToken)
        {
            var project = await _unitOfWork.ProjectRepository.GetByIdAsync(request.Id);

            if (project == null)
            {
                throw new ApplicationException($"Project with ID '{request.Id}' not found.");
            }

            // Call the aggregate method to update strategic details
            project.UpdateDetails(
               
                request.OverallStatus,
                request.ExecutiveSummary,
                request.OwnerName,
                request.OwnerContact,
                request.ProjectStartDate,
                request.TargetEndDate
            );

            await _unitOfWork.ProjectRepository.UpdateAsync(project);
            await _unitOfWork.SaveChangesAsync(); // Save changes to the database

            return Result.Ok();
        }
    }
}