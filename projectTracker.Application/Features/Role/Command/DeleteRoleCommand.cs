using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using MediatR;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Features.Role.Command
{
    public class DeleteRoleCommand :IRequest<Result>
    {
        public string Id { get; set; }
    }

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
    {
        private readonly IRepository<UserRole> _roleRepository;

        public async  Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                return  Result.Fail("role not found");
            }
            try
            {
                var result = await _roleRepository.DeleteAsync(role);
                return result ? Result.Ok() : Result.Fail("couldn't delete role");
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }

        }
    }
}
