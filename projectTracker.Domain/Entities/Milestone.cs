using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Enums;

namespace projectTracker.Domain.Entities
{
    public class Milestone 
    {
        public string Id { get; private set; } 
        public string Name { get; private set; }
        public DateTime DueDate { get; private set; }
        public MilestoneStatus Status { get; private set; }
        public string? Description { get; private set; } 

        
        public string ProjectId { get; private set; } 
         public Project Project { get; private set; } // Navigation property if needed, but often not in child entities within the same aggregate

        // Private constructor for EF Core
        private Milestone() { }

        // Factory method to create a new Milestone
        public static Milestone Create(string name, DateTime dueDate, string projectId, string? description = null, MilestoneStatus status = MilestoneStatus.Planned)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Milestone name cannot be null or whitespace.", nameof(name));
            }
            if (dueDate == default)
            {
                throw new ArgumentException("Milestone due date cannot be default.", nameof(dueDate));
            }
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Project ID for milestone cannot be null or whitespace.", nameof(projectId));
            }

            return new Milestone
            {
                Id = Guid.NewGuid().ToString(), // Assuming GUID as string for IDs
                Name = name,
                DueDate = dueDate,
                Status = status,
                Description = description,
                ProjectId = projectId
            };
        }

        
        public void Update(string name, DateTime dueDate, MilestoneStatus status, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Milestone name cannot be null or whitespace.", nameof(name));
            }
            if (dueDate == default)
            {
                throw new ArgumentException("Milestone due date cannot be default.", nameof(dueDate));
            }

            Name = name;
            DueDate = dueDate;
            Status = status;
            Description = description;
        }
    }
}
