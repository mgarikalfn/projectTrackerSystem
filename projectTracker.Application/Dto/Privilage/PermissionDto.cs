using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.Privilage
{
   
        public class PermissionDto
        {
            public string Id { get; set; }
            public string PermissionName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
        }

        public class CreatePermissionDto
        {
            public string PermissionName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
        }

        public class UpdatePermissionDto 
        {
        public string PermissionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
    
}
