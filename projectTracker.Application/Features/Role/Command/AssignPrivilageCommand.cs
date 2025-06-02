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
    public class AssignPrivilegeCommand :IRequest<Result<string>>
    {
        public string RoleId { get; set; }
        public List<int> PrivilageIds = new List<int>();
    }


    public class AssignPrivilegeCommandHandler : IRequestHandler<AssignPrivilegeCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AssignPrivilegeCommandHandler> _logger;

        public AssignPrivilegeCommandHandler(
            IUnitOfWork unitOfWork,
            ILogger<AssignPrivilegeCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<string>> Handle(AssignPrivilegeCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate role exists
            var role = await _unitOfWork.RoleRepository.GetByIdAsync(request.RoleId);
            if (role == null)
            {
                return Result.Fail<string>("Role not found");
            }

            // 2. Validate all privilege IDs exist
            var allPrivileges = await _unitOfWork.PrivilegeRepository.GetAllAsync();
            var invalidPrivilegeIds = request.PrivilageIds.Except(allPrivileges.Select(p => p.Id)).ToList();

            if (invalidPrivilegeIds.Any())
            {
                return Result.Fail<string>($"Invalid privilege IDs detected: {string.Join(", ", invalidPrivilegeIds)}");
            }

            // 3. Start transaction
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 4. Get existing privileges for the role
                var existingPrivileges = await _unitOfWork.RolePrivilegeRepository
                    .GetWhereAsync(rp => rp.RoleId == request.RoleId);

                // 5. Determine privileges to remove (not in the new list)
                var privilegesToRemove = existingPrivileges
                    .Where(ep => !request.PrivilageIds.Contains(ep.PrivilageId))
                    .ToList();

                // 6. Determine privileges to add (not already assigned)
                var privilegesToAdd = request.PrivilageIds
                    .Except(existingPrivileges.Select(ep => ep.PrivilageId))
                    .Select(pid => new RolePrivilege
                    {
                        RoleId = request.RoleId,
                        PrivilageId = pid,
                        
                    })
                    .ToList();

                // 7. Execute changes
                foreach (var priv in privilegesToRemove)
                {
                    await _unitOfWork.RolePrivilegeRepository.DeleteAsync(priv);
                }

                foreach (var priv in privilegesToAdd)
                {
                    await _unitOfWork.RolePrivilegeRepository.AddAsync(priv);
                }

                // 8. Commit transaction
                await _unitOfWork.CommitAsync();

                return Result.Ok("Privileges assigned successfully");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error assigning privileges to role {RoleId}", request.RoleId);
                return Result.Fail<string>($"Failed to assign privileges: {ex.Message}");
            }
        }
    }

}
