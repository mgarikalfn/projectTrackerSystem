using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.Role
{
    public class UpdateRoleDto
    {
        public string? RoleName { get; set; }
        public string? Description { get; set; }
        public List<PrivilageDto> Privilages = new List<PrivilageDto>();
    }
}
