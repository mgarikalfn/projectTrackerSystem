using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;


namespace projectTracker.Application.Features.Privilege.Command
{
   
    public record UpdatePrivilegeCommand(UpdatePrivilegeDto PrivilegeDto)
        : IRequest<Result>;

    public class UpdatePrivilegeCommandHandler : IRequestHandler<UpdatePrivilegeCommand, Result>
    {
        private readonly IRepository<Domain.Entities.Privilege> _repository;

        public UpdatePrivilegeCommandHandler(IRepository<Domain.Entities.Privilege> repository)
        {
            _repository = repository;
        }

        public async Task<Result> Handle(UpdatePrivilegeCommand request, CancellationToken cancellationToken)
        {
            var privilege = await _repository.GetByIdAsync(request.PrivilegeDto.Id.ToString());
            if (privilege == null) return Result.Fail("Privilege not found");

            privilege.PrivilageName = request.PrivilegeDto.PrivilegeName;
            privilege.Description = request.PrivilegeDto.Description;
            privilege.Action = request.PrivilegeDto.Action;

            await _repository.UpdateAsync(privilege);
            return Result.Ok();
        }
    }
}
