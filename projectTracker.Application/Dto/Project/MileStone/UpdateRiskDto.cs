using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Enums;

namespace projectTracker.Application.Dto.Project.MileStone
{
    public class UpdateRiskDto
    {
        public string Description { get; set; } = default!;
        public RiskImpact Impact { get; set; }
        public RiskLikelihood Likelihood { get; set; }
        public string? MitigationPlan { get; set; }
        public RiskStatus Status { get; set; }
    }
}
