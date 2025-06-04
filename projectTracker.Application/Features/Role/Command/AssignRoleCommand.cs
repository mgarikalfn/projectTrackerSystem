using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Command
{
    public class AssignRoleCommand : IRequest<Result<string>>
    {
        public string UserId { get; set; }
        public List<string> RoleIds { get; set; } = new List<string>();
    }

    public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignRoleCommandHandler> _logger;

        public AssignRoleCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<AssignRoleCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<string>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate input
                if (request == null)
                    return Result.Fail<string>("Request cannot be null");

                if (string.IsNullOrWhiteSpace(request.UserId))
                    return Result.Fail<string>("User ID is required");

                request.RoleIds ??= new List<string>();

                // Validate user exists
                var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    return Result.Fail<string>("User not found");
                }

                // Early return if no roles to assign
                if (!request.RoleIds.Any())
                {
                    return Result.Ok("No roles to assign");
                }

                // Validate all role IDs exist
                var existingRoles = await _unitOfWork.RoleRepository
                    .GetWhereAsync(r => request.RoleIds.Contains(r.Id));

                var existingRoleIds = existingRoles.Select(r => r.Id).ToList();
                var invalidRoleIds = request.RoleIds.Except(existingRoleIds).ToList();

                if (invalidRoleIds.Any())
                {
                    return Result.Fail<string>($"Invalid role IDs detected: {string.Join(", ", invalidRoleIds)}");
                }

                // Start transaction
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    // Get existing role mappings
                    var existingMappings = await _unitOfWork.UserRoleMappingRepository
                        .GetWhereAsync(urm => urm.UserId == request.UserId);

                    // Determine changes
                    var currentRoleIds = existingMappings.Select(em => em.RoleId).ToList();
                    var rolesToRemove = currentRoleIds.Except(request.RoleIds).ToList();
                    var rolesToAdd = request.RoleIds.Except(currentRoleIds).ToList();

                    // Remove unwanted mappings
                    if (rolesToRemove.Any())
                    {
                        var mappingsToRemove = existingMappings
                            .Where(em => rolesToRemove.Contains(em.RoleId))
                            .ToList();

                        foreach (var mapping in mappingsToRemove)
                        {
                            await _unitOfWork.UserRoleMappingRepository.DeleteAsync(mapping);
                        }
                    }

                    // Add new mappings
                    if (rolesToAdd.Any())
                    {
                        var newMappings = rolesToAdd
                            .Select(roleId => new UserRoleMapping
                            {
                                UserId = request.UserId,
                                RoleId = roleId,
                                AssignedAt = DateTime.UtcNow
                            })
                            .ToList();

                        foreach (var mapping in newMappings)
                        {
                            await _unitOfWork.UserRoleMappingRepository.AddAsync(mapping);
                        }
                    }

                    // Commit transaction
                    await _unitOfWork.CommitAsync();
                    return Result.Ok("Roles assigned successfully");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogError(ex, "Transaction failed while assigning roles to user {UserId}", request.UserId);
                    return Result.Fail<string>($"Failed to assign roles: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignRoleCommandHandler for user {UserId}", request.UserId);
                return Result.Fail<string>($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}