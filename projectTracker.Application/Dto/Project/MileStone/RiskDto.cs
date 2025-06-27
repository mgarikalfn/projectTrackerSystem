using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.Project.MileStone
{
    public class RiskDto
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Impact { get; set; }     // Enum to string
        public string Likelihood { get; set; } // Enum to string
        public string? MitigationPlan { get; set; }
        public string Status { get; set; }     // Enum to string
        public string ProjectId { get; set; }

        public RiskDto(string id, string description, string impact, string likelihood, string? mitigationPlan, string status, string projectId)
        {
            Id = id;
            Description = description;
            Impact = impact;
            Likelihood = likelihood;
            MitigationPlan = mitigationPlan;
            Status = status;
            ProjectId = projectId;
        }
    }
}
