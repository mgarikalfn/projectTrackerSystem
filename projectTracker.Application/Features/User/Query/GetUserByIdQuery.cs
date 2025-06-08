using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.User.Query
{
    public class GetUserByIdQuery:IRequest<Result<UsersDto>>
    {
        public string UserId { get; set; }
    }

    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UsersDto>>
    {
        private readonly IRepository<AppUser> _userRepository;
        public GetUserByIdQueryHandler(IRepository<AppUser> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UsersDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _userRepository.GetByIdAsync(request.UserId);
                if(result == null)
                {
                    return Result.Fail<UsersDto>(new Error("user not found").CausedBy(request.UserId));
                }
                var userDto = new UsersDto
                {
                    DisplayName = result.DisplayName,
                    Email = result.Email,
                    Source = result.Location,
                    AccountId = result.AccountId,
                    AvatarUrl = result.AvatarUrl,
                };
                return Result.Ok(userDto);
            }
            catch (Exception ex)
            {
                return Result.Fail<UsersDto>(new Error("failed to fetch User").CausedBy(ex));
            }
            
        }
    }

}
