using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Dto.Project.MileStone;

namespace projectTracker.Application.Dto.Project
{
    public class ProjectDetailDto
    {
        public string Id { get; set; } = default!;
        public string Key { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? Lead { get; set; }

        // Existing metric/health fields
        public string HealthLevel { get; set; } = default!; // Enum to string
        public string? HealthReason { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public decimal StoryPointsTotal { get; set; }
        public decimal StoryPointsCompleted { get; set; }
        public int ActiveBlockers { get; set; }

        // NEW Strategic fields
        public string OverallProjectStatus { get; set; } = default!; // Enum to string
        public string? ExecutiveSummary { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerContactInfo { get; set; }
        public DateTime? ProjectStartDate { get; set; }
        public DateTime? TargetEndDate { get; set; }

        public List<MilestoneDto> Milestones { get; set; } = new();
        public List<RiskDto> Risks { get; set; } = new();
    }
}
