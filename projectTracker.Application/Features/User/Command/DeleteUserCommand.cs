using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.User.Command
{
    public record DeleteUserCommand(string Id) : IRequest<Result>;


    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
    {
        private readonly IRepository<AppUser> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteUserCommandHandler(
            IRepository<AppUser> repository,
            IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(request.Id);
            if (user == null)
                return Result.Fail("User not found");

            var userRoles = await _unitOfWork.UserRoleMappingRepository
                .GetWhereAsync(rp => rp.UserId == request.Id);

            // Optional: delete user-role mappings if allowed
            if (userRoles != null && userRoles.Any())
            {
                foreach (var mapping in userRoles)
                {
                    await _unitOfWork.UserRoleMappingRepository.DeleteAsync(mapping);
                }
            }

            await _repository.DeleteAsync(user);
            return Result.Ok();
        }

    }
}
