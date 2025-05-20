using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Enums;
using projectTracker.Domain.Common;
using System;
using TaskStatus = projectTracker.Domain.Enums.TaskStatus;

namespace projectTracker.Domain.Entities
{
    public class ProjectTask : AggregateRoot<string>
    {
        public string Key { get; private set; }
        public string Summary { get; private set; }
        public string? Description { get; private set; }
        public TaskStatus Status { get; private set; }
        public DateTime StatusChangedDate { get; private set; }
        public DateTime? DueDate { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public DateTime UpdatedDate { get; private set; }
        public string AssigneeId { get; private set; }
        public string AssigneeName { get; private set; }
        public DateTime Updated { get; private set; }
        public string ProjectId { get; private set; }
        public Project Project { get; private set; } // Navigation property

        // Metrics
        public decimal? StoryPoints { get; private set; }
        public int? TimeEstimateMinutes { get; private set; }


        public ProjectTask(string key, string summary)
        {
            Id = Guid.NewGuid().ToString(); // Convert Guid to string
            Key = key;
            Summary = summary;
            // Initialize other properties with default values
            Status = TaskStatus.ToDo;
            Updated = DateTime.UtcNow;
        }

        private ProjectTask() { } // Required for EF Core


        // Domain Methods
        public void UpdateFromJira(
            string title,
            string? description,
            string status,
            string? assigneeId,
            string? assigneeName,
            DateTime? dueDate,
            decimal? storyPoints,
            int? timeEstimate,
            DateTime updatedDate)
        {
            Summary = title;
            Description = description;
            Status = TaskStatusMapper.FromJira(status);
            AssigneeId = assigneeId;
            AssigneeName = assigneeName;
            DueDate = dueDate;
            StoryPoints = storyPoints;
            TimeEstimateMinutes = timeEstimate;
            UpdatedDate = updatedDate;

            if (StatusChangedDate == default || Status != TaskStatusMapper.FromJira(status))
            {
                StatusChangedDate = DateTime.UtcNow;
            }
        }
        public void UpdateDetails(string key, string summary, TaskStatus status, string assigneeId, DateTime updated)
        {
            Summary = summary;
            Status = status;
            AssigneeId = assigneeId;
            Updated = updated;
        }

        // Factory Method
        public static ProjectTask Create(
            string taskKey,
            string title,
            Guid projectId)
        {
            return new ProjectTask
            {
                Id = Guid.NewGuid().ToString(), // Convert Guid to string
                Key = taskKey,
                Summary = title,
                ProjectId = projectId.ToString(), // Convert Guid to string
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Status = TaskStatus.ToDo
            };
        }
    }

    // Assuming TaskStatusMapper is in the same namespace or accessible
    public static class TaskStatusMapper
    {
        public static TaskStatus FromJira(string jiraStatus)
        {
            // Implement your mapping logic here based on Jira status values
            switch (jiraStatus.ToLower())
            {
                case "to do":
                    return TaskStatus.ToDo;
                case "in progress":
                    return TaskStatus.InProgress;
                case "done":
                    return TaskStatus.Done;
                case "blocked":
                    return TaskStatus.Blocked;
                // Add more mappings as needed
                default:
                    // Consider logging or throwing an exception for unmapped statuses
                    return TaskStatus.ToDo; // Default fallback
            }
        }
    }
}