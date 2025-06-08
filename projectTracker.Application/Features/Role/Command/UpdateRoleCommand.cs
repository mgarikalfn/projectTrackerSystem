using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
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

        public UpdateRoleCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 1. Get existing role
                var role = await _unitOfWork.RoleRepository.GetByIdAsync(request.Id);
                if (role == null)
                {
                    return Result.Fail("Role not found");
                }

                // Track if any role properties changed
                bool hasChanges = false;

                // 2. Update only provided properties (partial update)
                if (request.Name != null && role.Name != request.Name)
                {
                    role.Name = request.Name;
                    role.NormalizedName = request.Name.ToUpper();
                    hasChanges = true;
                }

                if (request.Description != null && role.Description != request.Description)
                {
                    role.Description = request.Description;
                    hasChanges = true;
                }

                // Only update if any role properties changed
                if (hasChanges)
                {
                    await _unitOfWork.RoleRepository.UpdateAsync(role);
                }

                // 3. Handle new permissions (if any provided)
                if (request.PermissionIdsToAdd != null && request.PermissionIdsToAdd.Any())
                {
                    var existingPermissionIds = (await _unitOfWork.RolePermissionRepository.GetAllAsync())
                        .Where(rp => rp.RoleId == request.Id)
                        .Select(rp => rp.PermissionId)
                        .ToList();

                    var validPermissions = (await _unitOfWork.PermissionRepository.GetAllAsync())
                        .Where(p => request.PermissionIdsToAdd.Contains(p.Id))
                        .ToList();

                    foreach (var permission in validPermissions)
                    {
                        if (!existingPermissionIds.Contains(permission.Id))
                        {
                            await _unitOfWork.RolePermissionRepository.AddAsync(new RolePermission
                            {
                                RoleId = role.Id,
                                PermissionId = permission.Id
                                
                            });
                        }
                    }
                }

                await _unitOfWork.CommitAsync();
                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"Update failed: {ex.Message}");
            }
        }
    }
}