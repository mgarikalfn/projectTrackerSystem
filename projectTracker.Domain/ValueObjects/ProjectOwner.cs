using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Aggregates;
using projectTracker.Domain.Entities;
using projectTracker.Domain.Enums;

namespace projectTracker.Domain.ValueObjects
{
    public class ProjectOwner 
    {
        public string Name { get; private set; }
        public string? ContactInfo { get; private set; } // Could be email, phone, etc.

        // Private constructor for EF Core
        private ProjectOwner() { }

        public ProjectOwner(string name, string? contactInfo = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Project owner name cannot be null or whitespace.", nameof(name));
            }
            Name = name;
            ContactInfo = contactInfo;
        }

        
    }
}


