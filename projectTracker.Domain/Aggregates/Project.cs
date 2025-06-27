using System;
using System.Collections.Generic;
using System.Linq; // For .ToList() in constructor, etc.
using projectTracker.Domain.Common;
using projectTracker.Domain.Entities; // For Milestone, Risk
using projectTracker.Domain.Enums;    // For OverallProjectStatus, MilestoneStatus, RiskImpact, RiskLikelihood, RiskStatus
using projectTracker.Domain.ValueObjects; // For ProjectOwner

namespace projectTracker.Domain.Aggregates
{
    public class Project : AggregateRoot<string>
    {
        // Core Identity (Existing)
        public string Key { get; private set; }
        public string Name { get; private set; }
        public string? Description { get; private set; }
        public string? Lead { get; private set; } // Jira Technical Lead

        // Managed by Background Service (Existing)
        public ProjectHealth Health { get; private set; }
        public ProgressMetrics Progress { get; private set; }

        // NEW: Strategic Project Properties (Managed by Project Manager via your app)
        public OverallProjectStatus OverallProjectStatus { get; private set; }
        public string? ExecutiveSummary { get; private set; }
        public ProjectOwner? Owner { get; private set; } // Using the ProjectOwner Value Object
        public DateTime? ProjectStartDate { get; private set; } // Nullable, as it might be set later
        public DateTime? TargetEndDate { get; private set; } // Nullable, as it might be set later

        // NEW: Navigation Collections for Milestones and Risks
        private readonly List<Milestone> _milestones = new();
        public IReadOnlyCollection<Milestone> Milestones => _milestones.AsReadOnly();

        private readonly List<Risk> _risks = new();
        public IReadOnlyCollection<Risk> Risks => _risks.AsReadOnly();

        // Navigation (for domain rules - Existing)
        private readonly List<ProjectTask> _tasks = new();
        public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

        private List<SyncHistory> _syncHistory = new();
        public IReadOnlyCollection<SyncHistory> SyncHistory => _syncHistory.AsReadOnly();


        // Domain Logic (Existing)
        public bool Critical => Health.Level == HealthLevel.Critical;
        // public bool IsOnTrack => Progress.OnTrackPercentage >= 80;


        // Domain Methods (Existing)
        public void UpdateHealthMetrics(ProjectHealth health)
        {
            if (health is null)
            {
                throw new ArgumentNullException(nameof(health), "Health metrics cannot be null.");
            }
            Health = health;
            // AddDomainEvent(new ProjectHealthUpdatedEvent(this)); // Re-enable if using Domain Events
        }

        public void UpdateProgressMetrics(ProgressMetrics progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress), "Progress metrics cannot be null.");
            }
            Progress = progress;
        }

        // --- NEW: Method for Jira-synced updates ---
        public void UpdateJiraSyncedDetails(string name, string? description, string? leadName)
        {
            Name = name;
            Description = description;
            Lead = leadName;
            // Only update fields that originate from Jira.
            // Do NOT touch OverallProjectStatus, ExecutiveSummary, Owner, ProjectStartDate, TargetEndDate, Milestones, Risks here.
        }
        // --- END NEW METHOD ---


        // NEW: Methods to Update Strategic Project Properties (PM-curated)
        public void UpdateOverallStatus(OverallProjectStatus status)
        {
            OverallProjectStatus = status;
        }

        public void UpdateExecutiveSummary(string? summary)
        {
            ExecutiveSummary = summary;
        }

        public void UpdateProjectOwner(string ownerName, string? contactInfo)
        {
            Owner = new ProjectOwner(ownerName, contactInfo);
        }

        public void SetProjectStartAndTargetEndDates(DateTime? startDate, DateTime? targetEndDate)
        {
            ProjectStartDate = startDate;
            TargetEndDate = targetEndDate;
        }


        // NEW: Methods to Manage Milestones
        public Milestone AddMilestone(string name, DateTime dueDate, string? description = null, MilestoneStatus status = MilestoneStatus.Planned)
        {
            // Ensure ID is passed down correctly
            var milestone = Milestone.Create(name, dueDate, this.Id, description, status);
            _milestones.Add(milestone);
            // AddDomainEvent(new MilestoneAddedEvent(this, milestone)); // Example Domain Event
            return milestone;
        }

        public void UpdateMilestone(string milestoneId, string name, DateTime dueDate, MilestoneStatus status, string? description)
        {
            var existingMilestone = _milestones.FirstOrDefault(m => m.Id == milestoneId);
            if (existingMilestone == null)
            {
                throw new InvalidOperationException($"Milestone with ID '{milestoneId}' not found in project '{this.Id}'.");
            }
            existingMilestone.Update(name, dueDate, status, description);
            // AddDomainEvent(new MilestoneUpdatedEvent(this, existingMilestone));
        }

        public void RemoveMilestone(string milestoneId)
        {
            var milestoneToRemove = _milestones.FirstOrDefault(m => m.Id == milestoneId);
            if (milestoneToRemove == null)
            {
                throw new InvalidOperationException($"Milestone with ID '{milestoneId}' not found in project '{this.Id}'.");
            }
            _milestones.Remove(milestoneToRemove);
            // AddDomainEvent(new MilestoneRemovedEvent(this, milestoneToRemove));
        }


        // NEW: Methods to Manage Risks
        public Risk AddRisk(string description, RiskImpact impact, RiskLikelihood likelihood, string? mitigationPlan = null, RiskStatus status = RiskStatus.Open)
        {
            // Ensure ID is passed down correctly
            var risk = Risk.Create(description, impact, likelihood, this.Id, mitigationPlan, status);
            _risks.Add(risk);
            // AddDomainEvent(new RiskAddedEvent(this, risk)); // Example Domain Event
            return risk;
        }

        public void UpdateRisk(string riskId, string description, RiskImpact impact, RiskLikelihood likelihood, string? mitigationPlan, RiskStatus status)
        {
            var existingRisk = _risks.FirstOrDefault(r => r.Id == riskId);
            if (existingRisk == null)
            {
                throw new InvalidOperationException($"Risk with ID '{riskId}' not found in project '{this.Id}'.");
            }
            existingRisk.Update(description, impact, likelihood, mitigationPlan, status);
            // AddDomainEvent(new RiskUpdatedEvent(this, existingRisk));
        }

        public void RemoveRisk(string riskId)
        {
            var riskToRemove = _risks.FirstOrDefault(r => r.Id == riskId);
            if (riskToRemove == null)
            {
                throw new InvalidOperationException($"Risk with ID '{riskId}' not found in project '{this.Id}'.");
            }
            _risks.Remove(riskToRemove);
            // AddDomainEvent(new RiskRemovedEvent(this, riskToRemove));
        }


        // Private constructor for entity framework and internal use (Existing)
        private Project() { }

        // Factory Method (Existing, updated to initialize new properties)
        public static Project Create(string Id, string key, string name, string? Lead, string? Description)
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
                Id = Id, // This ID should ideally be the JiraProjectId if that's the primary linkage
                Key = key,
                Name = name,
                Lead = Lead,
                Description = Description,
                Health = ProjectHealth.Unknown(), // Assuming ProjectHealth.Unknown()
                Progress = ProgressMetrics.Empty(), // Assuming ProgressMetrics.Empty()
                OverallProjectStatus = OverallProjectStatus.NotStarted, // Default new status for new projects
                ExecutiveSummary = null,
                Owner = null,
                ProjectStartDate = null,
                TargetEndDate = null,
                // Milestones and Risks collections are initialized empty by default
            };
        }

        // --- Original UpdateDetails method, now ONLY for PM-curated strategic updates ---
        public void UpdateDetails( // Consider renaming this to UpdateStrategicDetails or UpdatePmCuratedDetails
         
            OverallProjectStatus overallStatus,
            string? executiveSummary,
            string? ownerName,
            string? ownerContact,
            DateTime? projectStartDate,
            DateTime? targetEndDate
        )
        {
            // IMPORTANT: Decide if Name, Description, Lead can also be updated by PM.
            // If they are ONLY Jira-synced, remove them from this method's parameters
            // and the assignments below.
            // Name = name;
            // Description = description;
            // Lead = leadName;

            OverallProjectStatus = overallStatus;
            ExecutiveSummary = executiveSummary;

            if (!string.IsNullOrWhiteSpace(ownerName))
            {
                Owner = new ProjectOwner(ownerName, ownerContact);
            }
            else
            {
                Owner = null; // Clear owner if name is empty
            }

            ProjectStartDate = projectStartDate;
            TargetEndDate = targetEndDate;

            // AddDomainEvent if appropriate for this combined update
        }
    }
}