using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace projectTracker.Domain.Entities
{
    public class UserRoleMapping : IdentityUserRole<string>
    {
        public DateTime AssignedAt { get; set;} = DateTime.UtcNow;
        public string AssignedBy { get; set; } = String.Empty;
    }
}
