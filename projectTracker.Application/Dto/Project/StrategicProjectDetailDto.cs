using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Dto.Project
{
    public class StrategicProjectDetailDto
    {
        public OverallProjectStatus OverallStatus { get; set; }
        public string? ExecutiveSummary { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerContact { get; set; }
        public DateTime? ProjectStartDate { get; set; }
        public DateTime? TargetEndDate { get; set; }
    }
}
