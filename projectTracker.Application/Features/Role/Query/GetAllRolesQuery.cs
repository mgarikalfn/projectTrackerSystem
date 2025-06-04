using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Query
{
    public class GetAllRolesQuery : IRequest<List<RoleDto>>
    {

    }

    public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, List<RoleDto>>
    {
        private readonly IRepository<UserRole> _roleRepository;
        public GetAllRolesQueryHandler(IRepository<UserRole> roleRepository)
        {
            _roleRepository = roleRepository;
        }
        public async Task<List<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _roleRepository.GetAllAsync();
            var roleDtos = roles.Select(role => new RoleDto(
                 role.CreatedAt,
                 role.Name,
                 role.Description,
                 role.RolePermissions.Select(rp => rp.Permission.PermissionName).ToList()
                   )).ToList();

            return  roleDtos;
        }
    }
}
