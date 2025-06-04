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
            CreateMap<RoleUpdateDto, UpdateRoleCommand>();
            CreateMap<UpdateRoleCommand, UserRole>();


            CreateMap<Permission, PermissionDto>();
            CreateMap<CreatePermissionDto, Permission>();
            CreateMap<UpdatePermissionDto, Permission>();
        }
    }
}
