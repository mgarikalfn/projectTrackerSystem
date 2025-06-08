using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore; // Add this for Include
using projectTracker.Application.Dto.Role;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Query
{
    public class GetAllRolesQuery : IRequest<Result<List<RoleDto>>>
    {
    }

    public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, Result<List<RoleDto>>>
    {
        private readonly IRepository<UserRole> _roleRepository;

        public GetAllRolesQueryHandler(IRepository<UserRole> roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<Result<List<RoleDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            // Use GetQueryable() to access the full EF Core query capabilities
            var roles = await _roleRepository.GetQueryable()
                .Include(role => role.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .ToListAsync(cancellationToken);

            var roleDtos = roles.Select(role => new RoleDto(
                role.CreatedAt,
                role.Name,
                role.Description,
                role.RolePermissions?
                    .Select(rp => rp.Permission?.PermissionName) // Make sure this matches your Permission class property name
                    .Where(name => name != null)
                    .ToList() ?? new List<string>(),
                role.Id
            )).ToList();

            return Result.Ok(roleDtos);
        }
    }
    
}