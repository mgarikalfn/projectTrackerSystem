using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Common;

namespace projectTracker.Domain.Entities
{
    public class Sprint : AggregateRoot<Guid>
    {
        public int JiraId { get; private set; } // Jira's internal Sprint ID
        public Guid BoardId { get; private set; } // Foreign key to the Board this sprint belongs to
        public string Name { get; private set; } = string.Empty;
        public string State { get; private set; } = string.Empty; // e.g., 'active', 'closed', 'future'
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public DateTime? CompleteDate { get; private set; }
        public string? Goal { get; private set; } = string.Empty; // Sprint goal or objective

        // Navigation property if you want to link back to the board
        public virtual Board Board { get; private set; } = default!;
        

        // Constructor/Factory
        private Sprint() { }
        public Sprint(int jiraId, Guid boardId, string name, string state, DateTime? startDate, DateTime? endDate, DateTime? completeDate)
        {
            Id = Guid.NewGuid();
            JiraId = jiraId;
            BoardId = boardId;
            Name = name;
            State = state;
            StartDate = startDate;
            EndDate = endDate;
            CompleteDate = completeDate;
        }

        // Methods to update state, e.g., CloseSprint()

        public void UpdateDetails(string name, string state, DateTime? startDate, DateTime? endDate, DateTime? completeDate, string? goal)
        {
            Name = name;
            State = state;
            StartDate = startDate;
            EndDate = endDate;
            CompleteDate = completeDate;
            Goal = goal ?? string.Empty;
            // You might also add logic here to update a LastUpdated timestamp if desired
        }

        public Sprint(int jiraId, Guid boardId, string name, string state, DateTime? startDate, DateTime? endDate, DateTime? completeDate, string? goal)
        {
            Id = Guid.NewGuid(); // Your internal GUID
            JiraId = jiraId;
            BoardId = boardId;
            Name = name;
            State = state;
            StartDate = startDate;
            EndDate = endDate;
            CompleteDate = completeDate;
            Goal = goal ?? string.Empty;
        }
    }
}
