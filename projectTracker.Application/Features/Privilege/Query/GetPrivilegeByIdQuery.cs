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
    // GetPrivilegeByIdQuery.cs
    
        public record GetPrivilegeByIdQuery(int Id) : IRequest<Result<PrivilegeDto>>;

        public class GetPrivilegeByIdQueryHandler : IRequestHandler<GetPrivilegeByIdQuery, Result<PrivilegeDto>>
        {
            private readonly IRepository<Domain.Entities.Privilege> _repository;
            private readonly IMapper _mapper;

            public GetPrivilegeByIdQueryHandler(IRepository<Domain.Entities.Privilege> repository, IMapper mapper)
            {
                _repository = repository;
                _mapper = mapper;
            }

            public async Task<Result<PrivilegeDto>> Handle(GetPrivilegeByIdQuery request, CancellationToken cancellationToken)
            {
                var privilege = await _repository.GetByIdAsync(request.Id.ToString());
                if (privilege == null) return Result.Fail<PrivilegeDto>("Privilege not found");

                return Result.Ok(_mapper.Map<PrivilegeDto>(privilege));
            }
        }
    
}
