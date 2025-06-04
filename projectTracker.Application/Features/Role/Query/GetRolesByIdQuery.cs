using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Query
{
    public class GetRolesByIdQuery : IRequest<RoleDto>
    {
        public string Id { get; set; }
    }

    public class GetRolesByIdQueryHandler : IRequestHandler<GetRolesByIdQuery, RoleDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetRolesByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RoleDto> Handle(GetRolesByIdQuery request, CancellationToken cancellationToken)
        {
            // Get the role
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                return null;
            }

            // Get all role-permission mappings for this role
            var rolePermissions = (await _unitOfWork.RolePermissionRepository.GetAllAsync())
                .Where(rp => rp.RoleId == request.Id)
                .ToList();

            // Get all related permissions in one query
            var permissionIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToList();
            var permissions = (await _unitOfWork.PermissionRepository.GetAllAsync())
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.PermissionName)
                .ToList();

            // Create the response DTO
            return new RoleDto(
                CreatedAt: role.CreatedAt,
                Name: role.Name,
                Description: role.Description,
                Permissions: permissions
            );
        }
    }
}