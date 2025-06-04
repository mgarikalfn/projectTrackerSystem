using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace projectTracker.Domain.Entities
{
    public class UserRole : IdentityRole
    {
        public string RoleName { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();   
    }
}
