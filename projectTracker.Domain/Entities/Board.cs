using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Common;

namespace projectTracker.Domain.Entities
{
    public class Board : AggregateRoot<Guid>
    {
        public int JiraId { get; private set; } // Jira's internal Board ID
        public string Name { get; private set; } = string.Empty;
        public string Type { get; private set; } = string.Empty; // e.g., Scrum, Kanban
        public virtual ICollection<Sprint> Sprints { get; private set; } = new List<Sprint>();

        private Board() { }
        public Board(int jiraId, string name, string type)
        {
            Id = Guid.NewGuid();
            JiraId = jiraId;
            Name = name;
            Type = type;
        }


        public void UpdateDetails(string name, string type)
        {
            Name = name;
            Type = type;
            // You might also add logic here to update a LastUpdated timestamp if desired
        }
    }
}
