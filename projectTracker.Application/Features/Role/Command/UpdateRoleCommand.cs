using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Command
{
    public class UpdateRoleCommand : IRequest<Result<string>>
    {
        public string Id { get; set; }
    }


    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<String>>
    { 

        private readonly IRepository<UserRole> _roleRepository;
        private readonly IMapper _mapper;

        public UpdateRoleCommandHandler(IRepository<UserRole> roleRepository , IMapper mapper)
        {
           _roleRepository = roleRepository;
            _mapper = mapper;
        }
        public async Task<Result<string>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {

            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                return Result.Fail<string>("Role not found");   
            }

            _mapper.Map<UserRole>(request);

            try
            {
                var updateResult = await _roleRepository.UpdateAsync(role);
                return updateResult ? Result.Ok(role.Id) : Result.Fail<string>("update failed");
            }
            catch (Exception ex)
            {
                return Result.Fail<string>($"Update failed: {ex.Message}");
            }
        }
    }
}
