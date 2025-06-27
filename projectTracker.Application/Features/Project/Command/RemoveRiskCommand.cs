using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Project.Command
{
    public class RemoveRiskCommand : IRequest<Result>
    {
        public string RiskId { get; set; } = default!; // The ID of the risk to remove
    }

    public class RemoveRiskCommandHandler : IRequestHandler<RemoveRiskCommand, Result>
    {
        private readonly IRepository<Risk> _riskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveRiskCommandHandler(IRepository<Risk> riskRepository, IUnitOfWork unitOfWork)
        {
            _riskRepository = riskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(RemoveRiskCommand request, CancellationToken cancellationToken)
        {
            var riskToRemove = await _riskRepository.GetByIdAsync(request.RiskId);

            if (riskToRemove == null)
            {
                return Result.Fail($"Risk with ID '{request.RiskId}' not found.");
            }

            _riskRepository.DeleteAsync(riskToRemove); // Assuming your IRepository has a Remove method

            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
