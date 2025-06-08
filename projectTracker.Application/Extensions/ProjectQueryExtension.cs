using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Dto.Project;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Enums;

namespace projectTracker.Infrastructure.Extensions
{
    public static class ProjectQueryExtensions
    {
        public static IQueryable<Project> ApplyProjectQueryOptions(this IQueryable<Project> query, ProjectFilterDto filter)
        {
            // Filtering
            if (!string.IsNullOrWhiteSpace(filter.Name))
                query = query.Where(p => p.Name.Contains(filter.Name));

            if (!string.IsNullOrWhiteSpace(filter.Description))
                query = query.Where(p => p.Description != null && p.Description.Contains(filter.Description));

            if (!string.IsNullOrWhiteSpace(filter.Lead))
                query = query.Where(p => p.Lead != null && p.Lead.Contains(filter.Lead));

            if (filter.HealthLevel.HasValue)
                query = query.Where(p => p.Health.Level == filter.HealthLevel.Value);

            if (filter.IsCritical.HasValue)
            {
                query = filter.IsCritical.Value
                    ? query.Where(p => p.Health.Level == HealthLevel.Critical)  // Critical only
                    : query.Where(p => p.Health.Level != HealthLevel.Critical); // Non-critical
            }

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "lead" => filter.SortDescending ? query.OrderByDescending(p => p.Lead) : query.OrderBy(p => p.Lead),
                "health" => filter.SortDescending ? query.OrderByDescending(p => p.Health.Level) : query.OrderBy(p => p.Health.Level),
                _ => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
            };

            // Pagination
            query = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);

            return query;
        }
    }

}
