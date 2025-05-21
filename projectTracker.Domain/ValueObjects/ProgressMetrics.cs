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
        public decimal StoryPointsTotal { get; init; }

        public int ActiveBlockers { get; init; }       // New: open blocker/critical issues
        public int RecentUpdates { get; init; }        // New: recent issue updates

       // public decimal OnTrackPercentage { get; init; }  // Optional but good
       // public decimal? VelocityTrend { get; init; }     // Optional trend metric


        public ProgressMetrics(int totalTasks, int completedTasks, 
            decimal storyPointsCompleted, decimal storyPointsTotal, 
            int activeBlockers, int recentUpdates
           )
        {
            TotalTasks = totalTasks;
            CompletedTasks = completedTasks;
            StoryPointsCompleted = storyPointsCompleted;
            StoryPointsTotal = storyPointsTotal;
            ActiveBlockers = activeBlockers;
            RecentUpdates = recentUpdates;
           
        }

        public ProgressMetrics()
        {
        }

        public static ProgressMetrics Empty() =>
            new()
            {
                TotalTasks = 0,
                CompletedTasks = 0,
                StoryPointsCompleted = 0,
                StoryPointsTotal = 0,
                ActiveBlockers = 0,
                RecentUpdates = 0
            };
    }

}
