using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Features.Project.Command
{
    public class UpdateRiskCommand : IRequest<Result>
    {
        public string RiskId { get; set; } = default!;
        public string Description { get; set; } = default!;
        public RiskImpact Impact { get; set; }
        public RiskLikelihood Likelihood { get; set; }
        public string? MitigationPlan { get; set; }
        public RiskStatus Status { get; set; }
        // ProjectId is generally not updated for an existing child entity
        // public string? ProjectId { get; set; }
    }

    public class UpdateRiskCommandHandler : IRequestHandler<UpdateRiskCommand, Result>
    {
        private readonly IRepository<Risk> _riskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateRiskCommandHandler(IRepository<Risk> riskRepository, IUnitOfWork unitOfWork)
        {
            _riskRepository = riskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateRiskCommand request, CancellationToken cancellationToken)
        {
            var risk = await _riskRepository.GetByIdAsync(request.RiskId);

            if (risk == null)
            {
                return Result.Fail($"Risk with ID '{request.RiskId}' not found.");
            }

            // Basic validation
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return Result.Fail("Risk description cannot be empty.");
            }

            // Call the domain entity's update method
            risk.Update(
                description: request.Description,
                impact: request.Impact,
                likelihood: request.Likelihood,
                mitigationPlan: request.MitigationPlan,
                status: request.Status
            );

            // No explicit _riskRepository.UpdateAsync needed if EF Core tracks the retrieved entity
            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
