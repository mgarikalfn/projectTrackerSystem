using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Interfaces;

namespace projectTracker.Application.Features.Privilege.Query
{

    public record GetAllPermissionsQuery : IRequest<Result<IReadOnlyList<PermissionDto>>>;

    public class GetAllPrivilegesQueryHandler
                    : IRequestHandler<GetAllPermissionsQuery, Result<IReadOnlyList<PermissionDto>>>
    {
        private readonly IRepository<Domain.Entities.Permission> _repository;
        private readonly IMapper _mapper;

        public GetAllPrivilegesQueryHandler(IRepository<Domain.Entities.Permission> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<Result<IReadOnlyList<PermissionDto>>> Handle(
           GetAllPermissionsQuery request,
           CancellationToken cancellationToken)
        {
            var permissions = await _repository.GetAllAsync();
            var permissionDtos = permissions
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    PermissionName = p.PermissionName,
                    Description = p.Description,
                    Action = p.Action
                })
                .ToList(); // Removed .AsReadOnly() to ensure compatibility with IReadOnlyList  

            return Result.Ok<IReadOnlyList<PermissionDto>>(permissionDtos); // Explicitly specify the generic type  
        }
    }

}
