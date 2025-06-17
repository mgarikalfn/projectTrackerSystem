//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using FluentResults;
//using MediatR;
//using projectTracker.Application.Dto;
//using projectTracker.Application.Dto.Role;
//using projectTracker.Application.Interfaces;
//using projectTracker.Domain.Entities;

//namespace projectTracker.Application.Features.User.Query
//{
//    public class GetUsersQuery : IRequest<Result<List<UsersDto>>>
//    {

//    }

//    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, Result<List<UsersDto>>>
//    {
//        private readonly IRepository<AppUser> _userRepository;

//        public GetUsersQueryHandler(IRepository<AppUser> userRepository)
//        {
//            _userRepository = userRepository;
//        }

//        async Task<Result<List<UsersDto>>> IRequestHandler<GetUsersQuery, Result<List<UsersDto>>>.Handle(GetUsersQuery request, CancellationToken cancellationToken)
//        {
//            var users = await _userRepository.GetAllAsync();
//            var userDto = users.Select(user => new UsersDto
//            {
//                DisplayName = user.DisplayName,
//                Email = user.Email,
//                AvatarUrl = user.AvatarUrl,
//                AccountId = user.AccountId,
//                Active = user.IsActive,
//                Source= user.Location,
//                UserId = user.Id,
//                FirstName = user.FirstName,
//                LastName = user.LastName
//            })
//                .ToList();
//            return Result.Ok(userDto);
//        }
//    }
//}
