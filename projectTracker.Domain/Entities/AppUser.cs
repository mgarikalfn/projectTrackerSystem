using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace projectTracker.Domain.Entities
{
    // Define the UserSource enum within the same namespace or in Domain.Enums folder
    public enum UserSource
    {
        Local, // Users created directly within your projectTracker system
        Jira   // Users synced from an external system like Jira
    }

    public class AppUser : IdentityUser
    {
        // Basic properties for local system users
        public string FirstName { get; set; } = String.Empty;
        public string LastName { get; set; } = String.Empty;
        public bool IsActive { get; set; } = true; 

        // Properties for users synced from external project management tools (e.g., Jira)
        public string AccountId { get; set; } = String.Empty; 
        public string DisplayName { get; set; } = String.Empty; 
        public string AvatarUrl { get; set; } = String.Empty; 
        public string TimeZone { get; set; } = String.Empty; 
        public decimal CurrentWorkload { get; set; } // Calculated metric (can be synced or derived)
        public string Location { get; set; } = String.Empty; // User's location from external system

       
        public UserSource Source { get; set; } = UserSource.Local; // Defaults to Local for new local users

        public bool MustChangePassword { get; set; } = false;
        public ICollection<ProjectTask> AssignedTasks { get; set; }
    }
}