using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Enums;

namespace projectTracker.Domain.ValueObjects
{
    public record ProjectHealth
    {
        public HealthLevel Level { get; init; }  // OnTrack, AtRisk, Critical
        public string Reason { get; init; }  // "Blocked tasks", "Sprint behind", etc.

        public static ProjectHealth Unknown() =>
            new() { Level = HealthLevel.Unknown, Reason = "Not calculated yet" };
    }

  
}
