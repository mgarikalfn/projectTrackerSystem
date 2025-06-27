using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto.Project.MileStone;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Project.Query
{
    public class GetRiskByIdQuery : IRequest<Result<RiskDto>>
    {
        public string RiskId { get; set; } = default!;
    }

    public class GetRiskByIdQueryHandler : IRequestHandler<GetRiskByIdQuery, Result<RiskDto>>
    {
        private readonly IRepository<Risk> _riskRepository;

        public GetRiskByIdQueryHandler(IRepository<Risk> riskRepository)
        {
            _riskRepository = riskRepository;
        }

        public async Task<Result<RiskDto>> Handle(GetRiskByIdQuery request, CancellationToken cancellationToken)
        {
            var risk = await _riskRepository.GetByIdAsync(request.RiskId);

            if (risk == null)
            {
                return Result.Fail($"Risk with ID '{request.RiskId}' not found.");
            }

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
