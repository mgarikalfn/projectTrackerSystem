using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;
using FluentResults;
using AutoMapper;

namespace projectTracker.Application.Features.Role.Command
{
    public class CreateRoleCommand :IRequest<Result<string>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Privileges { get; set; } // Assuming privileges are passed as a list of strings

    }


    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<string>>
    {
        private readonly IRepository<UserRole> _roleRepository;
        private readonly IMapper _mapper;
         
        public CreateRoleCommandHandler(IRepository<UserRole> roleRepository, IMapper mapper )
        {
            _roleRepository = roleRepository;
            _mapper = mapper;
        }

        public async Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        { 
           if(request == null)
            {
                return Result.Fail<string>("Request cannot be null");
            }

            var role = _mapper.Map<UserRole>(request);
            await _roleRepository.AddAsync(role);
            return Result.Ok(role.Id); // Assuming UserRole has an Id property
        }
    }
}
