using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Domain.Entities
{
    public class Privilege
    {
        public int Id { get; set; }
        public string PrivilageName { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<RolePrivilege> RolePrivilage { get; set; } = new List<RolePrivilege>();
        public string Action { get; set; } = String.Empty;
    }
}
