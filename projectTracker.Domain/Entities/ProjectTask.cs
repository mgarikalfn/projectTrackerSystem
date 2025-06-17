using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Enums;
using projectTracker.Domain.Common;
using System;
using TaskStatus = projectTracker.Domain.Enums.TaskStatus; // Alias for clarity if needed

namespace projectTracker.Domain.Entities
{
    public class ProjectTask : AggregateRoot<string> // Using string for Id based on your provided code
    {
        public string Key { get; private set; } // Jira Task Key (e.g., "PROJ-123")
        public string Summary { get; private set; }
        public string? Description { get; private set; }
        public TaskStatus Status { get; private set; }
        public DateTime StatusChangedDate { get; private set; }
        public DateTime? DueDate { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime UpdatedDate { get; private set; } // Jira's 'updated' field
        public string? AssigneeId { get; private set; } // Can be null if unassigned
        public string? AssigneeName { get; private set; } // Can be null if unassigned
        public DateTime Updated { get; private set; } // Local timestamp when this record was last updated in your system

        // Foreign Key to Project Aggregate
        public string ProjectId { get; private set; }
        public virtual Project Project { get; private set; } = default!; // Navigation property

        // --- New Fields for Issue Types and Hierarchy ---
        public string IssueType { get; private set; } = string.Empty; // e.g., "Story", "Bug", "Task", "Epic", "Sub-task"
        public string? EpicKey { get; private set; } // If this task is part of an Epic, store the Epic's Jira Key
        public string? ParentKey { get; private set; } // If this task is a Sub-task, store its parent task's Jira Key

        // --- New Fields for Sprint Relationship ---
        public Guid? SprintId { get; private set; } // Foreign Key to the local Sprint entity
        public virtual Sprint? Sprint { get; private set; } // Navigation property (assuming Sprint is an Entity)

        // Metrics (StoryPoints is nullable, as not all IssueTypes have them)
        public decimal? StoryPoints { get; private set; }
        public int? TimeEstimateMinutes { get; private set; }

        // Private constructor for EF Core (good practice)
        private ProjectTask() { }

        // Constructor for creating a NEW ProjectTask from scratch (less common for sync)
        // Typically, you'd use the factory method below for initial creation during sync.
        public ProjectTask(string key, string summary, string projectId)
        {
            Id = Guid.NewGuid().ToString();
            Key = key;
            Summary = summary;
            ProjectId = projectId;
            Status = TaskStatus.ToDo; // Default status for new tasks
            CreatedDate = DateTime.UtcNow;
            UpdatedDate = DateTime.UtcNow;
            StatusChangedDate = DateTime.UtcNow;
            Updated = DateTime.UtcNow;
            IssueType = "Task"; // Default issue type
        }

        // --- Domain Methods ---
        // Enhanced UpdateFromJira to include new fields
        public void UpdateFromJira(
            string title,
            string? description,
            string jiraStatusName, // Renamed to avoid confusion with TaskStatus enum
            string? assigneeAccountId, // Jira's accountId
            string? assigneeDisplayName,
            DateTime? dueDate,
            decimal? storyPoints,
            int? timeEstimateMinutes,
            DateTime jiraUpdatedDate,
            string issueType,          // New parameter
            string? epicKey,           // New parameter
            string? parentKey,         // New parameter
            int? jiraSprintId)         // New parameter (Jira's ID for the current sprint)
        {
            Summary = title;
            Description = description;

            var newStatus = TaskStatusMapper.FromJira(jiraStatusName);
            if (Status != newStatus)
            {
                StatusChangedDate = DateTime.UtcNow;
            }
            Status = newStatus;

            AssigneeId = assigneeAccountId;
            AssigneeName = assigneeDisplayName;
            DueDate = dueDate;

            // Only set StoryPoints if the issue type typically has them
            // You might need a more robust check here, e.g., based on a configuration
            // For now, we'll just assign it directly, assuming it's null if not applicable.
            StoryPoints = storyPoints;

            TimeEstimateMinutes = timeEstimateMinutes;
            UpdatedDate = jiraUpdatedDate; // This is the 'updated' timestamp from Jira
            Updated = DateTime.UtcNow;     // This is the local timestamp of when this update occurred

            IssueType = issueType;
            EpicKey = epicKey;
            ParentKey = parentKey;

            // SprintId will be set by SyncManager after Sprints are synced and resolved
            // This method only receives the Jira Sprint ID.
        }

        // Example of a simpler update method for specific domain changes
        public void UpdateDetails(string summary, TaskStatus status, string? assigneeId, DateTime updated)
        {
            Summary = summary;
            if (Status != status)
            {
                StatusChangedDate = DateTime.UtcNow;
            }
            Status = status;
            AssigneeId = assigneeId;
            Updated = updated;
        }

        // Method to set the internal SprintId (called by SyncManager after resolution)
        public void SetSprint(Guid? sprintId)
        {
            SprintId = sprintId;
        }


        // --- Factory Method ---
        public static ProjectTask Create(
            string taskKey,
            string title,
            string projectId, // Changed to string to match Project.Id
            string issueType = "Task") // Default issue type for new local tasks
        {
            return new ProjectTask
            {
                Id = Guid.NewGuid().ToString(),
                Key = taskKey,
                Summary = title,
                ProjectId = projectId,
                IssueType = issueType,
                Status = TaskStatus.ToDo,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow, // Initialize to current time
                StatusChangedDate = DateTime.UtcNow, // Initialize
                Updated = DateTime.UtcNow // Initialize
            };
        }
    }

    // TaskStatusMapper remains the same
    public static class TaskStatusMapper
    {
        public static TaskStatus FromJira(string jiraStatus)
        {
            switch (jiraStatus.ToLower())
            {
                case "to do": return TaskStatus.ToDo;
                case "in progress": return TaskStatus.InProgress;
                case "done": return TaskStatus.Done;
                case "blocked": return TaskStatus.Blocked;
                default: return TaskStatus.ToDo;
            }
        }
    }
}