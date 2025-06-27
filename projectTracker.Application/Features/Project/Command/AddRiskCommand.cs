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

namespace projectTracker.Application.Features.Project.Command
{
    public class AddRiskCommand : IRequest<Result<RiskDto>>
    {
        public string ProjectId { get; set; } = default!;
        public string Description { get; set; } = default!;
        public RiskImpact Impact { get; set; } = RiskImpact.Medium; // Default value
        public RiskLikelihood Likelihood { get; set; } = RiskLikelihood.Medium; // Default value
        public string? MitigationPlan { get; set; }
        public RiskStatus Status { get; set; } = RiskStatus.Open; // Default value
    }

    public class AddRiskCommandHandler : IRequestHandler<AddRiskCommand, Result<RiskDto>>
    {
        private readonly IRepository<Risk> _riskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddRiskCommandHandler(IRepository<Risk> riskRepository, IUnitOfWork unitOfWork)
        {
            _riskRepository = riskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RiskDto>> Handle(AddRiskCommand request, CancellationToken cancellationToken)
        {
            // Check for duplicate risk description within the same project (optional, but good practice)
            var riskExists = await _riskRepository.GetQueryable()
                .AnyAsync(r => r.ProjectId == request.ProjectId && r.Description == request.Description, cancellationToken);

            if (riskExists)
            {
                return Result.Fail<RiskDto>("A risk with this description already exists for this project.");
            }

            // Basic validation for required fields
            if (string.IsNullOrWhiteSpace(request.ProjectId))
            {
                return Result.Fail<RiskDto>("Project ID is required to add a risk.");
            }
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return Result.Fail<RiskDto>("Risk description is required.");
            }

            var risk = Risk.Create(
                description: request.Description,
                impact: request.Impact,
                likelihood: request.Likelihood,
                mitigationPlan: request.MitigationPlan,
                status: request.Status,
                projectId: request.ProjectId
            );

            await _riskRepository.AddAsync(risk);
            await _unitOfWork.SaveChangesAsync();

            var riskDto = new RiskDto(
                risk.Id,
                risk.Description,
                risk.Impact.ToString(),
                risk.Likelihood.ToString(),
                risk.MitigationPlan,
                risk.Status.ToString(),
                risk.ProjectId
            );

            return Result.Ok(riskDto);
        }
    }
}
