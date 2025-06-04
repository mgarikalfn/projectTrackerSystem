using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Dto.Privilage;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Privilege.Command
{

    public record CreatePermissionCommand(CreatePermissionDto PermissionDto)
            : IRequest<Result<Permission>>
    {
       
    }

    public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Result<Permission>>
    {
        private readonly IRepository<Domain.Entities.Permission> _repository;

        public CreatePermissionCommandHandler(IRepository<Domain.Entities.Permission> repository)
        {
            _repository = repository;
        }

        public async Task<Result<Permission>> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
        {
            var permission = new Domain.Entities.Permission
            {
                Id = Guid.NewGuid().ToString(), // Generate a unique ID
                PermissionName = request.PermissionDto.PermissionName,
                Description = request.PermissionDto.Description,
                Action = request.PermissionDto.Action,
                CreatedAt = DateTime.UtcNow
            };
            try
            {
                var createdPermission = await _repository.AddAsync(permission);
                return Result.Ok(createdPermission);
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                return Result.Fail<Domain.Entities.Permission>(new Error("Failed to create permission").CausedBy(ex));
            }
        }
    }

}
