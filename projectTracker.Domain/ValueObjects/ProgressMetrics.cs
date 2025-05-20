using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Domain.ValueObjects
{
    public class ProgressMetrics
    {
        public int TotalTasks { get; init; }
        public int CompletedTasks { get; init; }
        public decimal StoryPointsCompleted { get; init; }

        public decimal StoryPointsTotal { get; init; }  // Total story points for all tasks
        public decimal OnTrackPercentage { get; init; }  // 0-100
        public decimal? VelocityTrend { get; init; }  // -100 to +100

        public static ProgressMetrics Empty() =>
            new() { TotalTasks = 0, CompletedTasks = 0, StoryPointsCompleted = 0 };
    }
}
