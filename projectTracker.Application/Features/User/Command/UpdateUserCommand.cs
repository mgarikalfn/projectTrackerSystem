using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;

namespace projectTracker.Application.Features.User.Command
{

    public class UpdateUserCommand : IRequest<Result>
    {
        public string UserId { get; set; }
        public string FirstName { get; set; } = String.Empty;
        public string LastName { get; set; } = String.Empty;
        public bool IsActive { get; set; } = true;

        //Additional informaition

        public string AccountId { get; set; } = String.Empty;
        public string DisplayName { get; set; } = String.Empty;
        public string AvatarUrl { get; set; } = String.Empty;
        public string TimeZone { get; set; } = String.Empty;
        public decimal CurrentWorkload { get; set; } = 0;
        public string Location { get; set; } = String.Empty;
    }

   

        public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
        {
            private readonly IUnitOfWork _unitOfWork;

            public UpdateUserCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
            {
                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId);
                    if (user == null)
                    {
                        return Result.Fail("User not found");
                    }

                    bool hasChanges = false;

                    if (user.FirstName != request.FirstName)
                    {
                        user.FirstName = request.FirstName;
                        hasChanges = true;
                    }

                    if (user.LastName != request.LastName)
                    {
                        user.LastName = request.LastName;
                        hasChanges = true;
                    }

                    if (user.IsActive != request.IsActive)
                    {
                        user.IsActive = request.IsActive;
                        hasChanges = true;
                    }

                    if (user.AccountId != request.AccountId)
                    {
                        user.AccountId = request.AccountId;
                        hasChanges = true;
                    }

                    if (user.DisplayName != request.DisplayName)
                    {
                        user.DisplayName = request.DisplayName;
                        hasChanges = true;
                    }

                    if (user.AvatarUrl != request.AvatarUrl)
                    {
                        user.AvatarUrl = request.AvatarUrl;
                        hasChanges = true;
                    }

                    if (user.TimeZone != request.TimeZone)
                    {
                        user.TimeZone = request.TimeZone;
                        hasChanges = true;
                    }

                    if (user.CurrentWorkload != request.CurrentWorkload)
                    {
                        user.CurrentWorkload = request.CurrentWorkload;
                        hasChanges = true;
                    }

                    if (user.Location != request.Location)
                    {
                        user.Location = request.Location;
                        hasChanges = true;
                    }

                    if (hasChanges)
                    {
                        await _unitOfWork.UserRepository.UpdateAsync(user);
                        await _unitOfWork.CommitAsync();
                    }

                    return Result.Ok();
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result.Fail($"Failed to update user: {ex.Message}");
                }
            }
        }


    
}

