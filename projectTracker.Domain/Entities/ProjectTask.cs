using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Enums;
using projectTracker.Domain.Common;
using System;
using TaskStatus = projectTracker.Domain.Enums.TaskStatus;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Alias for clarity if needed

namespace projectTracker.Domain.Entities
{
    public class ProjectTask : AggregateRoot<string> // Using string for Id based on your provided code
    {
        public string Id { get; private set; }
        public string Key { get; private set; } // Jira Task Key (e.g., "PROJ-123")
        public string Summary { get; private set; }
        public string? Description { get; private set; }
        public TaskStatus Status { get; private set; }
        public DateTime StatusChangedDate { get; private set; }
        public DateTime? DueDate { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime UpdatedDate { get; private set; } // <<-- This field is now correctly storing Jira's 'updated' field
       
        public DateTime Updated { get; private set; } // <<-- This field is your local timestamp, currently only updated on status change via UpdateDetails
        public string? Priority { get; private set; }

        // Foreign Key to Project Aggregate
        public string ProjectId { get; private set; }
        public virtual Project Project { get; private set; } = default!; // Navigation property

        [StringLength(450)] // Match AccountId length in AppUser
        [ForeignKey("Assignee")] // This tells EF that AssigneeId is the FK for the Assignee navigation property
        public string? AssigneeId { get; private set; } // Stores Jira user ID

        public virtual AppUser? Assignee { get; private set; } // Navigation property to AppUser

        [StringLength(100)] // Example length for DisplayName
        public string? AssigneeName { get; private set; } // Stored for convenience


        // --- New Fields for Issue Types and Hierarchy ---
        public string IssueType { get; private set; } = string.Empty; // e.g., "Story", "Bug", "Task", "Epic", "Sub-task"
        public string? EpicKey { get; private set; } // If this task is part of an Epic, store the Epic's Jira Key
        public string? ParentKey { get; private set; } // If this task is a Sub-task, store its parent task's Jira Key

        // --- New Fields for Sprint Relationship ---
        // Sprint Relationship
        public Guid? SprintId { get; private set; }
        [ForeignKey("SprintId")] // Associates SprintId property with the navigation property
        public virtual Sprint? Sprint { get; private set; }

        public int? JiraSprintId { get; private set; }
        public string? CurrentSprintName { get; private set; }
        public string? CurrentSprintState { get; private set; }

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
            UpdatedDate = DateTime.UtcNow; // Initialize to current time
            StatusChangedDate = DateTime.UtcNow;
            Updated = DateTime.UtcNow;
            IssueType = "Task"; // Default issue type
        }

        // --- Domain Methods ---
        // Enhanced UpdateFromJira to include new fields
        public void UpdateFromJira(
            string title,
            string description,
            string jiraStatusName,
            string assigneeAccountId,
            string assigneeDisplayName,
            DateTime? dueDate,
            decimal? storyPoints,
            int? timeEstimateMinutes,
            DateTime? jiraUpdatedDate, // This is Jira's 'updated' field
            string issueType,
            string epicKey,
            string parentKey,
            int? jiraSprintId,
            Guid? localSprintId,
            string? priority
        )
        {
            Summary = title;
            Description = description;

            // If the status changes, update StatusChangedDate
            var newStatus = TaskStatusMapper.FromJira(jiraStatusName);
            if (Status != newStatus)
            {
                Status = newStatus;
                StatusChangedDate = DateTime.UtcNow; // Local timestamp for status change
            }
            // Always store the Jira status name for reference
            // JiraStatusName = jiraStatusName; // <--- Assuming you have a JiraStatusName property, it's missing in this provided ProjectTask snippet

            //AssigneeId = assigneeAccountId;
            //AssigneeName = assigneeDisplayName;
            DueDate = dueDate;
            StoryPoints = storyPoints;
            TimeEstimateMinutes = timeEstimateMinutes;

            // CRUCIAL: Store Jira's updated date in UpdatedDate
            UpdatedDate = jiraUpdatedDate ?? DateTime.UtcNow; // Use Jira's updated date, or current UTC if null

            IssueType = issueType;
            EpicKey = epicKey;
            ParentKey = parentKey;
            JiraSprintId = jiraSprintId;
            SprintId = localSprintId;
            this.Priority = priority;

            // Updated is *not* set here, maintaining its "status change" behavior as per your design
        }

        // Example of a simpler update method for specific domain changes (e.g., local edits)
        public void UpdateDetails(string summary, TaskStatus status, string? assigneeId, DateTime updated)
        {
            Summary = summary;
            if (Status != status)
            {
                StatusChangedDate = DateTime.UtcNow;
            }
            Status = status;
           // AssigneeId = assigneeId;
            Updated = updated; // This one explicitly updates the 'Updated' field
        }

        // Method to set the internal SprintId (called by SyncManager after resolution)
        public void SetSprint(Guid? sprintId)
        {
            SprintId = sprintId;
        }

        // Fix for CS1737: Optional parameters must appear after all required parameters
        public static ProjectTask Create(
            string taskKey,
            string title,
            string projectId,
            DateTime jiraCreatedDate,
            DateTime jiraUpdatedDate,
            string issueType = "Task", // Moved optional parameters after required ones
            string? priority = null,
            Guid? sprintId = null)
        {
            return new ProjectTask
            {
                Id = Guid.NewGuid().ToString(),
                Key = taskKey,
                Summary = title,
                ProjectId = projectId,
                IssueType = issueType,
                Status = TaskStatus.ToDo,
                CreatedDate = jiraCreatedDate, // Use Jira's creation date
                UpdatedDate = jiraUpdatedDate, // Use Jira's updated date
                StatusChangedDate = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                SprintId = sprintId,
                Priority = priority // Set the new property
            };
        }

        // New method to set the resolved AppUser.Id and DisplayName on the Task
        public void SetAssigneeUser(string? appUserId, string? assigneeDisplayName)
        {
            AssigneeId = appUserId;       // This now stores AppUser.Id
            AssigneeName = assigneeDisplayName;
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