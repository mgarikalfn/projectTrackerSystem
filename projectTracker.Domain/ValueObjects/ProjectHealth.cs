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
        public HealthLevel Level { get; init; }  // Enum: OnTrack, AtRisk, Critical
        public string Reason { get; init; }      // Explanation string

        public double? Score { get; init; }      // Optional: 0.0 - 1.0 for analytics
        public string? Confidence { get; init; } // Optional: "Low", "Medium", "High"

        public static ProjectHealth Unknown() =>
            new() { Level = HealthLevel.Unknown, Reason = "Not calculated yet" };
    }



}
