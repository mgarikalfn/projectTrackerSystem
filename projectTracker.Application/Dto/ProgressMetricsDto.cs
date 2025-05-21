using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    public class ProgressMetricsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OpenIssues { get; set; }
        public int TotalStoryPoints { get; set; }
        public int CompletedStoryPoints { get; set; }
        public int ActiveBlockers { get; set; }         // new
        public int RecentUpdates { get; set; }          // new
        public DateTime LastCalculated { get; set; }
    }


}
