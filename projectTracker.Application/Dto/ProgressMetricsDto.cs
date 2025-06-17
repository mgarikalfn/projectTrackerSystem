

namespace projectTracker.Application.Dto
{
    public class ProgressMetricsDto
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OpenIssues { get; set; }
        public decimal TotalStoryPoints { get; set; }
        public decimal CompletedStoryPoints { get; set; }
        public int ActiveBlockers { get; set; }
        public int RecentUpdates { get; set; }
        public DateTime LastCalculated { get; set; }
        public int OverdueTasks { get; set; } // NEW: Added for risk calculation
    }

    // ... (rest of your DTOs)
}