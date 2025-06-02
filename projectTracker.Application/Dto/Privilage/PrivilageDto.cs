using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.Privilage
{
   
        public class PrivilegeDto
        {
            public int Id { get; set; }
            public string PrivilegeName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
        }

        public class CreatePrivilegeDto
        {
            public string PrivilegeName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
        }

        public class UpdatePrivilegeDto : CreatePrivilegeDto
        {
            public int Id { get; set; }
        }
    
}
