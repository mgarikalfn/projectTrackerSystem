using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Dto.User;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.User.Command
{
    public record CreateUserCommand(CreateUserDto userDto)
           : IRequest<Result<UsersDto>>
    {

    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UsersDto>>
    {
        private readonly IRepository<AppUser> _repository;

        public CreateUserCommandHandler(IRepository<AppUser> repository)
        {
            _repository = repository;
        }

        public async Task<Result<UsersDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var user = new AppUser
            {
                Id = Guid.NewGuid().ToString(), // Generate a unique ID
                FirstName = request.userDto.FirstName,
                DisplayName = request.userDto.LastName,
                Email = request.userDto.Email,
                AccountId = request.userDto.AccountId,
               Source = UserSource.Local,

            };
            try
            {
                var createdUser = await _repository.AddAsync(user);

                var userDto = new UsersDto
                {
                    AccountId = createdUser.AccountId,
                    //UserId = createdUser.Id,
                    DisplayName = createdUser.DisplayName,
                    Email = createdUser.Email
                };

                return Result.Ok(userDto);
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                return Result.Fail<UsersDto>(new Error("Failed to create User").CausedBy(ex));
            }
        }
    }
}
