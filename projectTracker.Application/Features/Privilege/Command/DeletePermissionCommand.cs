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

    public record DeletePermissionCommand(string Id) : IRequest<Result>;


    public class DeletePermissionCommandHandler : IRequestHandler<DeletePermissionCommand, Result>
    {
        private readonly IRepository<Domain.Entities.Permission> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeletePermissionCommandHandler(
            IRepository<Domain.Entities.Permission> repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(
            DeletePermissionCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Check if privilege exists
            var privilege = await _repository.GetByIdAsync(request.Id.ToString());
            if (privilege == null)
                return Result.Fail("Privilege not found");

            // 2. Check role assignments using UnitOfWork
            var rolePrivileges = await _unitOfWork.RolePermissionRepository
                .GetWhereAsync(rp => rp.PermissionId == request.Id); // Convert request.Id to string

            if (rolePrivileges?.Count > 0)
                return Result.Fail("Cannot delete privilege assigned to roles");

            // 3. Proceed with deletion
            await _repository.DeleteAsync(privilege);
            return Result.Ok();
        }
    }

}


