

using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Common;
using System;
using System.Collections.Generic;

namespace projectTracker.Domain.Entities
{
    public class Board : AggregateRoot<Guid> // Assuming Board ID is GUID based on your Sprint entity
    {
        public Guid Id { get; private set; } // Local GUID ID for the board
        public int JiraId { get; private set; } // Jira's internal ID for the board
        public string Name { get; private set; } = string.Empty;
        public string Type { get; private set; } = string.Empty; // e.g., "scrum", "kanban"

        // NEW: Foreign Key to Project
        public string ProjectId { get; private set; } // Foreign key to Project.Id (which is string)
        public virtual Project Project { get; private set; } = default!; // Navigation property

        // Collection of Sprints associated with this board
        public virtual ICollection<Sprint> Sprints { get; private set; } = new List<Sprint>();

        private Board() { } // Private constructor for EF Core

        public Board(int jiraId, string name, string type, string projectId) // Add projectId to constructor
        {
            Id = Guid.NewGuid();
            JiraId = jiraId;
            Name = name;
            Type = type;
            ProjectId = projectId; // Set the ProjectId
        }

        // Method to update board details (existing or new)
        public void UpdateDetails(string name, string type)
        {
            Name = name;
            Type = type;
           
        }
    }
}