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
   
        public record CreatePrivilegeCommand(CreatePrivilegeDto PrivilegeDto)
            : IRequest<Result<int>>;

        public class CreatePrivilegeCommandHandler : IRequestHandler<CreatePrivilegeCommand, Result<int>>
        {
            private readonly IRepository<Domain.Entities.Privilege> _repository;

            public CreatePrivilegeCommandHandler(IRepository<Domain.Entities.Privilege> repository)
            {
                _repository = repository;
            }

            public async Task<Result<int>> Handle(CreatePrivilegeCommand request, CancellationToken cancellationToken)
            {
                var privilege = new Domain.Entities.Privilege
                {
                    PrivilageName = request.PrivilegeDto.PrivilegeName,
                    Description = request.PrivilegeDto.Description,
                    Action = request.PrivilegeDto.Action,
                    CreatedAt = DateTime.UtcNow
                };

                await _repository.AddAsync(privilege);
                return Result.Ok(privilege.Id);
            }
        }
    
}
