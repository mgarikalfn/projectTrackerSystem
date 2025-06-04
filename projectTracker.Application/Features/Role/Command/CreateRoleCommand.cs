using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using projectTracker.Domain.Entities;
using FluentResults;
using projectTracker.Application.Interfaces;

namespace projectTracker.Application.Features.Role.Command
{
    public class CreateRoleCommand : IRequest<Result<string>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> PermissionIds { get; set; } // List of existing permission IDs to assign
    }
    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateRoleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Result.Fail<string>("Role name is required");
                }

                // Check if role already exists
                var existingRole = (await _unitOfWork.RoleRepository
                    .GetAllAsync())
                    .FirstOrDefault(r => r.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

                if (existingRole != null)
                {
                    return Result.Fail<string>("Role with this name already exists");
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
                if (request.PermissionIds != null && request.PermissionIds.Any())
                {
                    // Get all valid permissions in one query
                    var allPermissions = await _unitOfWork.PermissionRepository.GetAllAsync();
                    var validPermissionIds = allPermissions.Select(p => p.Id).ToList();

                    foreach (var permissionId in request.PermissionIds)
                    {
                        if (!validPermissionIds.Contains(permissionId))
                        {
                            await _unitOfWork.RollbackAsync();
                            return Result.Fail<string>($"Permission with ID {permissionId} not found");
                        }

                        // Create role-permission mapping
                        var rolePermission = new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permissionId,
                            // Set default permissions (adjust based on your requirements)
                            
                        };

                        await _unitOfWork.RolePermissionRepository.AddAsync(rolePermission);
                    }
                }

                await _unitOfWork.CommitAsync();
                return Result.Ok(role.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail<string>($"Failed to create role: {ex.Message}");
            }
        }
    }
}