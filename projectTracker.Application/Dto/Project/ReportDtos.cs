using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.Project
{
   
       
        public class ProjectSprintOverviewDto
        {
            public string ProjectKey { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public List<SprintListItemDto> Sprints { get; set; } = new List<SprintListItemDto>();
        }

       
        public class SprintReportDto
        {
            public Guid Id { get; set; }
            public int JiraId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? CompleteDate { get; set; }
            public string? Goal { get; set; }
            public string BoardName { get; set; } = string.Empty;

            
            public decimal TotalStoryPoints { get; set; }
            public decimal CompletedStoryPoints { get; set; }
            public decimal StoryPointCompletionPercentage { get; set; }
            public int TotalTasks { get; set; }
            public int CompletedTasks { get; set; }
            public decimal TaskCompletionPercentage { get; set; }
            public int ActiveBlockers { get; set; }
            public int OverdueTasks { get; set; }
            public int BugsCreatedThisSprint { get; set; }
            public int TasksMovedFromPreviousSprint { get; set; } // This needs historical data/changelog parsing

            // Task Status Breakdown
            public Dictionary<string, int> TaskStatusCounts { get; set; } = new Dictionary<string, int>(); // "To Do": 10, "In Progress": 5, "Done": 15
            public Dictionary<string, int> IssueTypeCounts { get; set; } = new Dictionary<string, int>(); // "Story": 10, "Bug": 5

            // Team Workload
            public List<DeveloperWorkloadDto> DeveloperWorkloads { get; set; } = new List<DeveloperWorkloadDto>();

            // Recent Activity (simplified for report summary; detailed log is separate)
            public List<RecentActivityItemDto> RecentActivities { get; set; } = new List<RecentActivityItemDto>();
            public IEnumerable<TaskDto> TasksInSprint { get; set; } = new List<TaskDto>();
    }

        public class DeveloperWorkloadDto
        {
            public string AssigneeId { get; set; } = string.Empty;
            public string AssigneeName { get; set; } = string.Empty;
            public decimal EstimatedWork { get; set; } // Could be SP or TimeEstimateMinutes
            public decimal CompletedWork { get; set; }
            public Dictionary<string, int> TaskStatusBreakdown { get; set; } = new Dictionary<string, int>(); // e.g., for this dev: "To Do": 3, "Done": 5
        }

        public class RecentActivityItemDto
        {
            public string TaskKey { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty; // e.g., "Status changed to Done"
            public string? ChangedBy { get; set; }
            public DateTime Timestamp { get; set; }
        }


    public class SprintListItemDto
    {
        public Guid Id { get; set; }
        public int JiraId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty; // e.g., "active", "closed", "future"
    }

}
