using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Interfaces;
using projectTracker.Infrastructure.Extensions;

namespace projectTracker.Application.Features.Project.Query
{
    public record GetAllProjectsQuery(ProjectFilterDto filter) : IRequest<Result<List<ProjectResponseDto>>>;

    public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, Result<List<ProjectResponseDto>>>
    {
        private readonly IRepository<Domain.Aggregates.Project> _projectRepository;
        private readonly IMapper _mapper;
        public GetAllProjectsQueryHandler(IRepository<Domain.Aggregates.Project> projectRepository, IMapper mapper)
        {
            _projectRepository = projectRepository;
            _mapper = mapper;
        }
        public async Task<Result<List<ProjectResponseDto>>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
        {
            var filter = request.filter;

            // Get queryable projects
            var query = _projectRepository.GetQueryable();

            // Apply filtering, sorting, and pagination
            query = query.ApplyProjectQueryOptions(filter);

            // Execute query
            var projects = await query.ToListAsync(cancellationToken);

            // Map to DTOs
            var projectDtos = _mapper.Map<List<ProjectResponseDto>>(projects);

            return Result.Ok(projectDtos);
        }

    }
}
