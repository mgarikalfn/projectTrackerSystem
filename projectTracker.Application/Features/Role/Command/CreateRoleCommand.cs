using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using projectTracker.Domain.Entities;
using FluentResults;
using projectTracker.Application.Interfaces;
using projectTracker.Application.Dto.Role;

namespace projectTracker.Application.Features.Role.Command
{
    public class CreateRoleCommand : IRequest<Result<RoleDto>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> PermissionIds { get; set; }
    }

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<RoleDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateRoleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result.Fail<RoleDto>("Role name is required");
                }

                // Check if role already exists
                var existingRole = (await _unitOfWork.RoleRepository
                    .GetAllAsync())
                    .FirstOrDefault(r => r.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

                if (existingRole != null)
                {
                    return Result.Fail<RoleDto>("Role with this name already exists");
                }

                // Create the role
                var role = new UserRole
                {
                    Name = request.Name.Trim(),
                    NormalizedName = request.Name.Trim().ToUpper(),
                    Description = request.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.RoleRepository.AddAsync(role);

                // Assign permissions if provided
                List<string> permissionNames = new List<string>();
                if (request.PermissionIds != null && request.PermissionIds.Any())
                {
                    // Get all valid permissions in one query
                    var allPermissions = await _unitOfWork.PermissionRepository.GetAllAsync();
                    var validPermissions = allPermissions.ToDictionary(p => p.Id, p => p.PermissionName);

                    foreach (var permissionId in request.PermissionIds)
                    {
                        if (!validPermissions.ContainsKey(permissionId))
                        {
                            await _unitOfWork.RollbackAsync();
                            return Result.Fail<RoleDto>($"Permission with ID {permissionId} not found");
                        }

                        // Create role-permission mapping
                        var rolePermission = new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permissionId
                        };

                        await _unitOfWork.RolePermissionRepository.AddAsync(rolePermission);

                        // Add permission name to our list
                        permissionNames.Add(validPermissions[permissionId]);
                    }
                }

                await _unitOfWork.CommitAsync();

                // Create the DTO with all needed information
                var roleDto = new RoleDto(
                    role.CreatedAt,
                    role.Name,
                    role.Description,
                    permissionNames,
                    role.Id
                );

                return Result.Ok(roleDto);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail<RoleDto>($"Failed to create role: {ex.Message}");
            }
        }
    }

   
}