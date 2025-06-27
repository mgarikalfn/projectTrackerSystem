using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Enums;

namespace projectTracker.Domain.Entities
{
    public class Risk  
    {
        public string Id { get; set; }
        public string Description { get; private set; }
        public RiskImpact Impact { get; private set; }
        public RiskLikelihood Likelihood { get; private set; }
        public string? MitigationPlan { get; private set; }
        public RiskStatus Status { get; private set; }

        // Foreign key to Project
        public string ProjectId { get; private set; } 
         public Project Project { get; private set; } 

        // Private constructor for EF Core
        private Risk() { }

        // Factory method to create a new Risk
        public static Risk Create(string description, RiskImpact impact, RiskLikelihood likelihood, string projectId, string? mitigationPlan = null, RiskStatus status = RiskStatus.Open)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Risk description cannot be null or whitespace.", nameof(description));
            }
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Project ID for risk cannot be null or whitespace.", nameof(projectId));
            }

            return new Risk
            {
                Id = Guid.NewGuid().ToString(), 
                Description = description,
                Impact = impact,
                Likelihood = likelihood,
                MitigationPlan = mitigationPlan,
                Status = status,
                ProjectId = projectId
            };
        }

       
        public void Update(string description, RiskImpact impact, RiskLikelihood likelihood, string? mitigationPlan, RiskStatus status)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Risk description cannot be null or whitespace.", nameof(description));
            }

            Description = description;
            Impact = impact;
            Likelihood = likelihood;
            MitigationPlan = mitigationPlan;
            Status = status;
        }
    }
}
