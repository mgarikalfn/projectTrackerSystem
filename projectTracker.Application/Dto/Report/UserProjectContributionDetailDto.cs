using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Application.Dto.Project;

namespace projectTracker.Application.Dto.Report
{
        public class UserProjectContributionDetailDto
        {
            [Required]
            public string UserId { get; set; } = string.Empty;

            [Required]
            public string UserName { get; set; } = string.Empty; // Display name of the user

            [Required]
            public string ProjectId { get; set; } = string.Empty;

            [Required]
            public string ProjectKey { get; set; } = string.Empty;

            [Required]
            public string ProjectName { get; set; } = string.Empty;

            // Overall metrics for this user within THIS project
            public int TotalTasksAssigned { get; set; }
            public int CompletedTasks { get; set; }
            public decimal TotalStoryPointsAssigned { get; set; }
            public decimal CompletedStoryPoints { get; set; }
            public int OverdueTasks { get; set; }
            public int ActiveBlockers { get; set; }

            // Percentage calculations
            public decimal TaskCompletionPercentage => TotalTasksAssigned > 0 ?
                (decimal)CompletedTasks / TotalTasksAssigned * 100 : 0;
            public decimal StoryPointCompletionPercentage => TotalStoryPointsAssigned > 0 ?
                CompletedStoryPoints / TotalStoryPointsAssigned * 100 : 0;

            /// <summary>
            /// A list of tasks assigned to this user within this project.
            /// Can be filtered or paginated in a real application.
            /// </summary>
            public List<TaskDto> UserTasksInProject { get; set; } = new List<TaskDto>();

            /// <summary>
            /// Breakdown of task statuses for this user within this project.
            /// Key: Status Name, Value: Count
            /// </summary>
            public Dictionary<string, int> TaskStatusCounts { get; set; } = new Dictionary<string, int>();

            /// <summary>
            /// Breakdown of issue types for this user within this project.
            /// Key: Issue Type Name, Value: Count
            /// </summary>
            public Dictionary<string, int> IssueTypeCounts { get; set; } = new Dictionary<string, int>();

            /// <summary>
            /// Breakdown of priorities for this user within this project.
            /// Key: Priority Name, Value: Count
            /// </summary>
            public Dictionary<string, int> PriorityCounts { get; set; } = new Dictionary<string, int>();

            /// <summary>
            /// Optional: List of sprints this user has been involved in within this project.
            /// This could be extended to show per-sprint contribution summaries if desired.
            /// </summary>
            public List<SprintListItemDto> SprintsInvolvedIn { get; set; } = new List<SprintListItemDto>();
        }
    
}
