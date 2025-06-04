using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Command
{
    public class AssignPermissionCommand : IRequest<Result<string>>
    {
        public string RoleId { get; set; }
        public List<string> PermissionIds { get; set; } = new List<string>();
    }

    public class AssignPermissionCommandHandler : IRequestHandler<AssignPermissionCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignPermissionCommand> _logger;

        public AssignPermissionCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<AssignPermissionCommand> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<string>> Handle(AssignPermissionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate input
                if (request == null)
                    return Result.Fail<string>("Request cannot be null");

                if (string.IsNullOrWhiteSpace(request.RoleId))
                    return Result.Fail<string>("Role ID is required");

                request.PermissionIds ??= new List<string>();

                // Validate role exists
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(request.RoleId);
                if (role == null)
                {
                    return Result.Fail<string>("Role not found");
                }

                // Early return if no privileges to assign
                if (!request.PermissionIds.Any())
                {
                    return Result.Ok("No privileges to assign");
                }

                // Validate all privilege IDs exist
                var existingPrivileges = await _unitOfWork.PermissionRepository
                    .GetWhereAsync(p => request.PermissionIds.Contains(p.Id));

                var existingPrivilegeIds = existingPrivileges.Select(p => p.Id).ToList();
                var invalidPrivilegeIds = request.PermissionIds.Except(existingPrivilegeIds).ToList();

                if (invalidPrivilegeIds.Any())
                {
                    return Result.Fail<string>($"Invalid privilege IDs detected: {string.Join(", ", invalidPrivilegeIds)}");
                }

                // Start transaction
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Get existing privilege mappings
                    var existingMappings = await _unitOfWork.RolePermissionRepository
                        .GetWhereAsync(rp => rp.RoleId == request.RoleId);

                    // Determine changes
                    var currentPrivilegeIds = existingMappings.Select(em => em.PermissionId).ToList();
                    var privilegesToRemove = currentPrivilegeIds.Except(request.PermissionIds).ToList();
                    var privilegesToAdd = request.PermissionIds.Except(currentPrivilegeIds).ToList();

                    // Remove unwanted mappings
                    if (privilegesToRemove.Any())
                    {
                        var mappingsToRemove = existingMappings
                            .Where(em => privilegesToRemove.Contains(em.PermissionId))
                            .ToList();

                        foreach (var mapping in mappingsToRemove)
                        {
                            await _unitOfWork.RolePermissionRepository.DeleteAsync(mapping);
                        }
                    }

                    // Add new mappings
                    if (privilegesToAdd.Any())
                    {
                        var newMappings = privilegesToAdd
                            .Select(privilegeId => new RolePermission
                            {
                                RoleId = request.RoleId,
                                PermissionId = privilegeId,
                                // AssignedAt = DateTime.UtcNow
                            })
                            .ToList();

                        foreach (var mapping in newMappings)
                        {
                            await _unitOfWork.RolePermissionRepository.AddAsync(mapping);
                        }
                    }

                    // Commit transaction
                    await _unitOfWork.CommitAsync();
                    return Result.Ok("Privileges assigned successfully");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed while assigning privileges to role {RoleId}", request.RoleId);
                    return Result.Fail<string>($"Failed to assign privileges: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignPrivilegeCommandHandler for role {RoleId}", request.RoleId);
                return Result.Fail<string>($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}



