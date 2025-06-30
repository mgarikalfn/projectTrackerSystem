using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Dto.Project
{
    public class ProjectFilterDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Lead { get; set; }

        public HealthLevel? HealthLevel { get; set; }
        public bool? IsCritical { get; set; }

        public string? SortBy { get; set; } = "Name";
        public bool SortDescending { get; set; } = false;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

}
