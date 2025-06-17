using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    public class TaskDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; } // Can contain rich text, handle parsing in domain
        public string? Status { get; set; } // Raw Jira status name
        public string? StatusCategory { get; set; } // e.g., "To Do", "In Progress", "Done"
        public string? AssigneeId { get; set; } // Jira Account ID
        public string? AssigneeName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; } // Last update timestamp from Jira
        public DateTime? DueDate { get; set; }
        public decimal? StoryPoints { get; set; } // Nullable, as not all issue types have SP
        public int? TimeEstimateMinutes { get; set; } // Mapped from Jira's originalEstimateSeconds
        public string? IssueType { get; set; } // e.g., "Story", "Bug", "Task", "Epic", "Sub-task"
        public string? EpicKey { get; set; } // Jira Key of the Epic this task belongs to
        public string? ParentKey { get; set; } // Jira Key of the parent task if this is a sub-task
        public List<string>? Labels { get; set; }
        public string? Priority { get; set; }

        // --- Sprint Information (from Jira, to be mapped to your local SprintId) ---
        // Jira's API typically returns a list of sprint objects in the 'sprint' field,
        // but for a single current sprint, we can map to these properties.
        public int? CurrentSprintJiraId { get; set; }
        public string? CurrentSprintName { get; set; }
        public string? CurrentSprintState { get; set; }
    }

}
