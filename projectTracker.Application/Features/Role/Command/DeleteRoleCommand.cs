using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Command
{
    public class DeleteRoleCommand : IRequest<Result>
    {
        public string Id { get; set; }
    }

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
    {
        private readonly IRepository<UserRole> _roleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteRoleCommandHandler(
            IRepository<UserRole> roleRepository,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            // 1. Check if role exists
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
                return Result.Fail("Role not found");

            // 2. Check user role assignments using UnitOfWork
            var userRoles = await _unitOfWork.UserRoleMappingRepository
                .GetWhereAsync(ur => ur.RoleId == request.Id);

            if (userRoles?.Count > 0)
                return Result.Fail("Cannot delete role assigned to users");

            // 3. Check role permission assignments using UnitOfWork
            var rolePrivileges = await _unitOfWork.RolePermissionRepository
                .GetWhereAsync(rp => rp.RoleId == request.Id);

            if (rolePrivileges?.Count > 0)
                return Result.Fail("Cannot delete role with assigned permissions");

            // 4. Proceed with deletion
            try
            {
                var result = await _roleRepository.DeleteAsync(role);
                return result ? Result.Ok() : Result.Fail("Couldn't delete role");
            }
            catch (Exception ex)
            {
                return Result.Fail("exception");
            }
        }
    }
}