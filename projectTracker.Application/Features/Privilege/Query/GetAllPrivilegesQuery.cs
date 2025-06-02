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
   
        public record GetAllPrivilegesQuery : IRequest<Result<IReadOnlyList<PrivilegeDto>>>;

        public class GetAllPrivilegesQueryHandler
            : IRequestHandler<GetAllPrivilegesQuery, Result<IReadOnlyList<PrivilegeDto>>>
        {
            private readonly IRepository<Domain.Entities.Privilege> _repository;
            private readonly IMapper _mapper;

            public GetAllPrivilegesQueryHandler(IRepository<Domain.Entities.Privilege> repository, IMapper mapper)
            {
                _repository = repository;
                _mapper = mapper;
            }

            public async Task<Result<IReadOnlyList<PrivilegeDto>>> Handle(
                GetAllPrivilegesQuery request,
                CancellationToken cancellationToken)
            {
                var privileges = await _repository.GetAllAsync();
                var privilegeDtos = _mapper.Map<IReadOnlyList<PrivilegeDto>>(privileges);
                return Result.Ok(privilegeDtos);
            }
        }
    
}
