using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Dto.Project
{
    public class ProjectResponseDto
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Lead { get; set; }

        public ProjectHealthDto Health { get; set; }
        public ProgressMetricsDto Progress { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public OverallProjectStatus Status { get; set; }
        public OwnerDto Owner { get; set; }
        public DateTime TargetEndDate {get; set;}

        public bool Critical { get; set; }
    }

    public class ProjectHealthDto
    {
        public int Level { get; set; }
        public string Reason { get; set; }
        public double? Score { get; set; }
        public string? Confidence { get; set; }
    }

    public class ProgressMetricsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public decimal StoryPointsCompleted { get; set; }
        public decimal StoryPointsTotal { get; set; }
        public int ActiveBlockers { get; set; }
        public int RecentUpdates { get; set; }
    }

    public class OwnerDto
    {
        public string Name { get; set; }
        public string ContactInfo { get; set; }
    }

}
