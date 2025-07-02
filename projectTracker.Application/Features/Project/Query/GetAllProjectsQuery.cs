using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using projectTracker.Application.Dto;
using projectTracker.Application.Dto.Project;
using projectTracker.Application.Extensions;
using projectTracker.Application.Interfaces;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Features.Project.Query
{
    public record GetAllProjectsQuery(ProjectFilterDto filter) : IRequest<Result<PagedList<ProjectResponseDto>>>;

    public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, Result<PagedList<ProjectResponseDto>>>
    {
        private readonly IRepository<Domain.Aggregates.Project> _projectRepository;
        private readonly IMapper _mapper;
           
        public GetAllProjectsQueryHandler(IRepository<Domain.Aggregates.Project> projectRepository, IMapper mapper)
        {
            _projectRepository = projectRepository;
            _mapper = mapper;
        }
        public async Task<Result<PagedList<ProjectResponseDto>>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
        {
            var filter = request.filter;

            // Get queryable projects
            var query = _projectRepository.GetQueryable();

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {

                var term = filter.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(term) ||
                    u.Description.ToLower().Contains(term) ||
                    u.Lead.ToLower().Contains(term));
                   

            }
            if (filter.HealthLevel.HasValue)
                query = query.Where(p => p.Health.Level == filter.HealthLevel.Value);

            if (filter.IsCritical.HasValue)
            {
                query = filter.IsCritical.Value
                    ? query.Where(p => p.Health.Level == HealthLevel.Critical)
                    : query.Where(p => p.Health.Level != HealthLevel.Critical);
            }
            if (filter.Status.HasValue)
            {
                query = query.Where(p => p.OverallProjectStatus == filter.Status.Value);
            }
           
            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "lead" => filter.SortDescending ? query.OrderByDescending(p => p.Lead) : query.OrderBy(p => p.Lead),
                "health" => filter.SortDescending ? query.OrderByDescending(p => p.Health.Level) : query.OrderBy(p => p.Health.Level),
                _ => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var projects = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var projectDtos = _mapper.Map<List<ProjectResponseDto>>(projects);

            // Create paged list (no need for async here since we're working with in-memory objects)
            var response = new PagedList<ProjectResponseDto>(
                projectDtos,
                totalCount,
                filter.PageNumber,
                filter.PageSize
            );

            return Result.Ok(response);
        }

    }
}
