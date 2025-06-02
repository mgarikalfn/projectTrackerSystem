using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Query
{
    public class GetRolesByIdQuery : IRequest<ActionResult<RoleDto>>
    {
        public string Id { get; set; }
    }


    public class GEtRolesByIdQueryHandler : IRequestHandler<GetRolesByIdQuery, ActionResult<RoleDto>>
    {
        private readonly IRepository<UserRole> _roleRepository;

        public GEtRolesByIdQueryHandler(IRepository<UserRole> roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<ActionResult<RoleDto>> Handle(GetRolesByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                return new NotFoundResult(); // Fix for CS1955: Use the correct instantiation of NotFoundResult.
            }

            return new RoleDto(
                role.CreatedAt,
                role.Name,
                role.Description,
                role.RolePrivilage.Select(rp => rp.Privilage.PrivilageName).ToList()
            );
        }
    }
}
