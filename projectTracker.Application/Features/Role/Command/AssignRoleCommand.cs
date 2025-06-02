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
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate user exists
            var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId);
            if (user == null)
            {
                return Result.Fail<string>("User not found");
            }

            // 2. Validate all role IDs exist
            var allRoles = await _unitOfWork.RoleRepository.GetAllAsync();
            var invalidRoleIds = request.RoleIds.Except(allRoles.Select(r => r.Id)).ToList();

            if (invalidRoleIds.Any())
            {
                return Result.Fail<string>($"Invalid role IDs detected: {string.Join(", ", invalidRoleIds)}");
            }

            // 3. Start transaction
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 4. Get existing role mappings for the user
                var existingMappings = await _unitOfWork.UserRoleMappingRepository
                    .GetWhereAsync(urm => urm.UserId == request.UserId);

                // 5. Determine mappings to remove (not in the new list)
                var mappingsToRemove = existingMappings
                    .Where(em => !request.RoleIds.Contains(em.RoleId))
                    .ToList();

                // 6. Determine mappings to add (not already assigned)
                var mappingsToAdd = request.RoleIds
                    .Except(existingMappings.Select(em => em.RoleId))
                    .Select(roleId => new UserRoleMapping
                    {
                        UserId = request.UserId,
                        RoleId = roleId,
                        AssignedAt = DateTime.UtcNow
                    })
                    .ToList();

                // 7. Execute changes
                foreach (var mapping in mappingsToRemove)
                {
                    await _unitOfWork.UserRoleMappingRepository.DeleteAsync(mapping);
                }

                foreach (var mapping in mappingsToAdd)
                {
                    await _unitOfWork.UserRoleMappingRepository.AddAsync(mapping);
                }

                // 8. Commit transaction
                await _unitOfWork.CommitAsync();

                return Result.Ok("Roles assigned successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error assigning roles to user {UserId}", request.UserId);
                return Result.Fail<string>($"Failed to assign roles: {ex.Message}");
            }
        }
    }
}