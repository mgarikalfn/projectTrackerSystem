using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Features.Role.Command;
using projectTracker.Domain.Entities;

namespace projectTracker.Infrastructure.Mapping
{
    public class MappingProfile :Profile
    {
        public MappingProfile()
        {
            CreateMap<CreateRoleCommand, UserRole>();
            CreateMap<UpdateRoleDto, UpdateRoleCommand>();
            CreateMap<UpdateRoleCommand, UserRole>();


            CreateMap<Privilege, PrivilegeDto>();
            CreateMap<CreatePrivilegeDto, Privilege>();
            CreateMap<UpdatePrivilegeDto, Privilege>();
        }
    }
}
