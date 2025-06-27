using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Domain.Enums
{
    public enum RiskImpact
    {
        Low,
        Medium,
        High
    }

    public enum RiskLikelihood
    {
        Low,
        Medium,
        High
    }

    public enum RiskStatus
    {
        Open,
        Mitigated,
        Closed
    }
}
