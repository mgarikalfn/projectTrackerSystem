using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Common;
using projectTracker.Domain.Enums;

namespace projectTracker.Domain.Entities
{

    public class SyncHistory : AggregateRoot<string>
    {
        public DateTime SyncTime { get; private set; }
        public SyncType Type { get; private set; } // Full/Incremental
        public SyncStatus Status { get; private set; }

        // References
        public string? ProjectId { get; private set; } // Null = system-wide sync
        public Project? Project { get; private set; }

        // Metrics
        public int TasksProcessed { get; private set; }
        public int TasksCreated { get; private set; }
        public int TasksUpdated { get; private set; }

        // Diagnostics
        public string? ErrorMessage { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string SyncTrigger { get; private set; } // Manual/Scheduled/Webhook

        // Jira Metadata
        public string? JiraRequestId { get; private set; }
        public DateTime? JiraDataCutoff { get; private set; } // For incremental syncs

        // Domain Methods
        public static SyncHistory Start(
    SyncType type,
    string? projectId,
    string trigger)
        {
            return new SyncHistory
            {
                Id = Guid.NewGuid().ToString(), // Convert Guid to string
                SyncTime = DateTime.UtcNow,
                Type = type,
                Status = SyncStatus.Running,
                ProjectId = projectId?.ToString(), // Convert Guid? to string?
                SyncTrigger = trigger,
                JiraDataCutoff = type == SyncType.Incremental ? DateTime.UtcNow : null
            };
        }

        public void Complete(int created, int updated)
        {
            Status = SyncStatus.Completed;
            TasksCreated = created;
            TasksUpdated = updated;
            TasksProcessed = created + updated;
            Duration = DateTime.UtcNow - SyncTime;
        }

        public void Fail(string error)
        {
            Status = SyncStatus.Failed;
            ErrorMessage = error;
            Duration = DateTime.UtcNow - SyncTime;
        }
    }

    public enum SyncType
    {
        Full,       // Complete resync
        Incremental // Only changes since last sync
    }

    public enum SyncStatus
    {
        Running,
        Completed,
        Failed,
        Partial     // Some items failed
    }
}
