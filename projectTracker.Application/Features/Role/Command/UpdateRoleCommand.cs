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
            // Initialize logger (you'll need to inject ILogger in constructor)
            // private readonly ILogger<UpdateRoleCommandHandler> _logger;

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

                // 2. Update role properties
                if (request.Name != null && role.Name != request.Name)
                {
                    _logger.LogDebug("Updating role name from '{OldName}' to '{NewName}'", role.Name, request.Name);
                    role.Name = request.Name;
                    role.NormalizedName = request.Name.ToUpper();
                    hasChanges = true;
                }

                if (request.Description != null && role.Description != request.Description)
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

                // 3. Handle permissions
                if (request.PermissionIdsToAdd != null && request.PermissionIdsToAdd.Any())
                {
                    _logger.LogDebug("Processing {Count} permission IDs to add", request.PermissionIdsToAdd.Count);

                    // Get existing permissions
                    _logger.LogDebug("Fetching existing role permissions");
                    var allRolePermissions = await _unitOfWork.RolePermissionRepository.GetAllAsync();
                    var existingPermissionIds = allRolePermissions
                        .Where(rp => rp.RoleId == request.Id)
                        .Select(rp => rp.PermissionId)
                        .ToList();

                    _logger.LogDebug("Found {Count} existing permissions for role", existingPermissionIds.Count);

                    // Get all valid permissions
                    _logger.LogDebug("Fetching all permissions to validate");
                    var allPermissions = await _unitOfWork.PermissionRepository.GetAllAsync();
                    var validPermissions = allPermissions
                        .Where(p => request.PermissionIdsToAdd.Contains(p.Id))
                        .ToList();

                    _logger.LogDebug("Found {Count} valid permissions out of {RequestedCount} requested",
                        validPermissions.Count, request.PermissionIdsToAdd.Count);

                    // Check for invalid permissions
                    var invalidIds = request.PermissionIdsToAdd.Except(validPermissions.Select(p => p.Id)).ToList();
                    if (invalidIds.Any())
                    {
                        _logger.LogWarning("Invalid permission IDs: {InvalidIds}", string.Join(", ", invalidIds));
                    }

                    // Add missing permissions
                    int addedCount = 0;
                    foreach (var permission in validPermissions)
                    {
                        if (!existingPermissionIds.Contains(permission.Id))
                        {
                            _logger.LogDebug("Adding permission {PermissionId} to role {RoleId}",
                                permission.Id, role.Id);

                            var rolePermission = new RolePermission
                            {
                                RoleId = role.Id,
                                PermissionId = permission.Id
                            };

                            await _unitOfWork.RolePermissionRepository.AddAsync(rolePermission);
                            addedCount++;
                        }
                        else
                        {
                            _logger.LogDebug("Permission {PermissionId} already exists for role {RoleId}",
                                permission.Id, role.Id);
                        }
                    }

                    _logger.LogInformation("Added {Count} new permissions to role {RoleId}", addedCount, role.Id);
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