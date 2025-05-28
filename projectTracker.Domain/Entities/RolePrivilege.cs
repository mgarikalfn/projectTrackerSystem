using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Domain.Entities
{   
    public class RolePrivilege
    {
        public string RoleId { get; set; } = String.Empty;
        public UserRole Role { get; set; } = new UserRole();
        public int PrivilageId { get; set; }
        public Privilege Privilage { get; set; } = new Privilege();
    }
}
