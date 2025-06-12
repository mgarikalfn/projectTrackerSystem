using System;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Privilege.Command
{
    public class UpdatePermissionCommand : IRequest<Result>
    {
        public string Id { get; set; }
        public string PermissionName { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
    }

    public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePermissionCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var permission = await _unitOfWork.PermissionRepository.GetByIdAsync(request.Id);
                if (permission == null)
                {
                    return Result.Fail("Permission not found");
                }

                // Track changes for partial update
                bool hasChanges = false;

                if (!string.IsNullOrWhiteSpace(request.PermissionName) &&
     permission.PermissionName != request.PermissionName)
                {
                    permission.PermissionName = request.PermissionName;
                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(request.Description) &&
                    permission.Description != request.Description)
                {
                    permission.Description = request.Description;
                    hasChanges = true;
                }

                if (!string.IsNullOrWhiteSpace(request.Action) &&
                    permission.Action != request.Action)
                {
                    permission.Action = request.Action;
                    hasChanges = true;
                }


                if (hasChanges)
                {
                    await _unitOfWork.PermissionRepository.UpdateAsync(permission);
                    await _unitOfWork.CommitAsync();
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return Result.Fail($"Failed to update permission: {ex.Message}");
            }
        }
    }
}