using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Command
{
    public class UpdateRoleCommand : IRequest<Result>
    {
        public string Id { get; set; }
        public string Name { get; set; }       // Optional
        public string Description { get; set; } // Optional
        public List<string> PermissionIdsToAdd { get; set; } // Optional
    }

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateRoleCommandHandler> _logger;

        public UpdateRoleCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting role update for role ID: {RoleId}", request.Id);
                await _unitOfWork.BeginTransactionAsync();

                // 1. Get existing role
                _logger.LogDebug("Fetching role with ID: {RoleId}", request.Id);
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(request.Id);
                if (role == null)
                {
                    _logger.LogWarning("Role not found with ID: {RoleId}", request.Id);
                    return Result.Fail("Role not found");
                }

                // Track if any role properties changed
                bool hasChanges = false;

                // 2. Update role properties - only if new value is not null/empty
                if (!string.IsNullOrEmpty(request.Name) && role.Name != request.Name)
                {
                    _logger.LogDebug("Updating role name from '{OldName}' to '{NewName}'", role.Name, request.Name);
                    role.Name = request.Name;
                    role.NormalizedName = request.Name.ToUpper();
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(request.Description) && role.Description != request.Description)
                {
                    _logger.LogDebug("Updating role description");
                    role.Description = request.Description;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    _logger.LogDebug("Persisting role changes");
                    await _unitOfWork.RoleRepository.UpdateAsync(role);
                }

                // 3. Handle permissions - replace all existing permissions with new list
                if (request.PermissionIdsToAdd != null) // This now represents the complete new list of permissions
                {
                    _logger.LogDebug("Processing permission updates for role");

                    // Get all valid permissions from database
                    var allPermissions = await _unitOfWork.PermissionRepository.GetAllAsync();

                    // Validate requested permissions exist
                    var invalidIds = request.PermissionIdsToAdd.Except(allPermissions.Select(p => p.Id)).ToList();
                    if (invalidIds.Any())
                    {
                        _logger.LogWarning("Invalid permission IDs: {InvalidIds}", string.Join(", ", invalidIds));
                        return Result.Fail($"Invalid permission IDs: {string.Join(", ", invalidIds)}");
                    }

                    // Get current role permissions
                    var existingRolePermissions = (await _unitOfWork.RolePermissionRepository.GetAllAsync())
                        .Where(rp => rp.RoleId == request.Id)
                        .ToList();

                    // Determine permissions to remove (existing ones not in new list)
                    var permissionsToRemove = existingRolePermissions
                        .Where(erp => !request.PermissionIdsToAdd.Contains(erp.PermissionId))
                        .ToList();

                    // Determine permissions to add (new ones not in existing list)
                    var permissionsToAdd = request.PermissionIdsToAdd
                        .Except(existingRolePermissions.Select(erp => erp.PermissionId))
                        .ToList();

                    // Remove old permissions
                    if (permissionsToRemove.Any())
                    {
                        _logger.LogDebug("Removing {Count} permissions from role", permissionsToRemove.Count);
                        foreach (var permission in permissionsToRemove)
                        {
                            await _unitOfWork.RolePermissionRepository.DeleteAsync(permission);
                        }
                    }

                    // Add new permissions
                    if (permissionsToAdd.Any())
                    {
                        _logger.LogDebug("Adding {Count} permissions to role", permissionsToAdd.Count);
                        foreach (var permissionId in permissionsToAdd)
                        {
                            await _unitOfWork.RolePermissionRepository.AddAsync(new RolePermission
                            {
                                RoleId = role.Id,
                                PermissionId = permissionId
                            });
                        }
                    }

                    _logger.LogInformation("Updated permissions for role {RoleId}. Added: {Added}, Removed: {Removed}",
                        role.Id, permissionsToAdd.Count, permissionsToRemove.Count);
                }

                _logger.LogDebug("Committing transaction");
                await _unitOfWork.CommitAsync();

                _logger.LogInformation("Successfully updated role {RoleId}", role.Id);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role {RoleId}: {ErrorMessage}", request.Id, ex.Message);
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"Update failed: {ex.Message}");
            }
        }   
    }
}