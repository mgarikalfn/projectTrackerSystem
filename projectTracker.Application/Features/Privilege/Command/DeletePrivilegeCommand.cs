using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;

namespace projectTracker.Application.Features.Privilege.Command
{
    
        public record DeletePrivilegeCommand(int Id) : IRequest<Result>;

        
        public class DeletePrivilegeCommandHandler: IRequestHandler<DeletePrivilegeCommand, Result>
        {
            private readonly IRepository<Domain.Entities.Privilege> _repository;
            private readonly IUnitOfWork _unitOfWork;

            public DeletePrivilegeCommandHandler(
                IRepository<Domain.Entities.Privilege> repository,
                IUnitOfWork unitOfWork)
            {
                _repository = repository;
                _unitOfWork = unitOfWork;
            }

            public async Task<Result> Handle(
                DeletePrivilegeCommand request,
                CancellationToken cancellationToken)
            {
                // 1. Check if privilege exists
                var privilege = await _repository.GetByIdAsync(request.Id.ToString());
                if (privilege == null)
                    return Result.Fail("Privilege not found");

                // 2. Check role assignments using UnitOfWork
                var rolePrivileges = await _unitOfWork.RolePrivilegeRepository
                    .GetWhereAsync(rp => rp.PrivilageId == request.Id);

                if (rolePrivileges?.Count > 0)
                    return Result.Fail("Cannot delete privilege assigned to roles");

                // 3. Proceed with deletion
                await _repository.DeleteAsync(privilege);
                return Result.Ok();
            }
        }
    }
    

