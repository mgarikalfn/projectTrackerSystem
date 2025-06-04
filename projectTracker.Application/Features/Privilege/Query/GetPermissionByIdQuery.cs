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
  

    public record GetPermissionByIdQuery(string Id) : IRequest<Result<PermissionDto>>;

    public class GetPermissionByIdQueryHandler : IRequestHandler<GetPermissionByIdQuery, Result<PermissionDto>>
    {
        private readonly IRepository<Domain.Entities.Permission> _repository;
        private readonly IMapper _mapper;

        public GetPermissionByIdQueryHandler(IRepository<Domain.Entities.Permission> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<Result<PermissionDto>> Handle(GetPermissionByIdQuery request, CancellationToken cancellationToken)
        {
            var privilege = await _repository.GetByIdAsync(request.Id);
            if (privilege == null) return Result.Fail<PermissionDto>("Permission not found");

            return Result.Ok(_mapper.Map<PermissionDto>(privilege));
        }
    }

}
