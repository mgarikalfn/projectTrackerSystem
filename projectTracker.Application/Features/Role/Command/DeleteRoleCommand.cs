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
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Check if role exists
                var role = await _roleRepository.GetByIdAsync(request.Id);
                if (role == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Role not found");
                }

                // 2. Check user role assignments using UnitOfWork
                var userRoles = await _unitOfWork.UserRoleMappingRepository
                    .GetWhereAsync(ur => ur.RoleId == request.Id);

                if (userRoles != null && userRoles.Any())
                {
                    foreach (var mapping in userRoles)
                    {
                        await _unitOfWork.UserRoleMappingRepository.DeleteAsync(mapping);
                    }
                }

                // 3. Check role permission assignments using UnitOfWork
                var rolePrivileges = await _unitOfWork.RolePermissionRepository
                    .GetWhereAsync(rp => rp.RoleId == request.Id);

                if (rolePrivileges != null && rolePrivileges.Any())
                {
                    foreach (var mapping in rolePrivileges)
                    {
                        await _unitOfWork.RolePermissionRepository.DeleteAsync(mapping);
                    }
                }

                // 4. Proceed with role deletion
                var result = await _roleRepository.DeleteAsync(role);

                if (!result)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail("Couldn't delete role");
                }

                await _unitOfWork.CommitAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail("An error occurred while deleting the role");
            }
        }
    }
}
