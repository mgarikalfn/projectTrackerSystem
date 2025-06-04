using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Domain.Entities
{
    

        [Table("RolePermission")]
        public class RolePermission 
        {
            public string RoleId { get; set; } = string.Empty;
            [ForeignKey(nameof(RoleId))]
            public virtual UserRole Role { get; set; } = null!;
            public string PermissionId { get; set; } = string.Empty;
            [ForeignKey(nameof(PermissionId))]
            public virtual Permission Permission { get; set; } = null!;

        }
    

}
