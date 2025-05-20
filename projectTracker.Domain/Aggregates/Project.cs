using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Common;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;
using projectTracker.Domain.ValueObjects;

namespace projectTracker.Domain.Aggregates
{
    public class Project : AggregateRoot<string>
    {
        // Core Identity
        public string Key { get; private set; }
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public string? Lead { get; private set; }

        // Managed by Background Service
        public ProjectHealth Health { get; private set; }
        public ProgressMetrics Progress { get; private set; }
       

        // Domain Logic
        public bool IsAtRisk => Health.Level == HealthLevel.AtRisk;
        public bool IsOnTrack => Progress.OnTrackPercentage >= 80;

        // Navigation (for domain rules)
        private readonly List<ProjectTask> _tasks = new();
        public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

        private List<SyncHistory> _syncHistory = new();
        public IReadOnlyCollection<SyncHistory> SyncHistory => _syncHistory.AsReadOnly();

        // Domain Methods
        public void UpdateHealthMetrics(ProjectHealth health)
        {
            
            if (health is null)
            {
                throw new ArgumentNullException(nameof(health), "Health metrics cannot be null.");
            }
            Health = health;
           
            //  AddDomainEvent(new ProjectHealthUpdatedEvent(this));
        }

        public void UpdateProgressMetrics(ProgressMetrics progress)
        {
            
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress), "Progress metrics cannot be null.");
            }
            Progress = progress;
        }

        // Private constructor for entity framework and internal use.
        private Project() { }

        // Factory Method
        public static Project Create(string Id,string key, string name)
        {
            
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
            }
            return new Project
            {
                Id = Id,
                Key = key,
                Name = name,
                Health = ProjectHealth.Unknown(),
                Progress = ProgressMetrics.Empty()
            };
        }
    }
}
