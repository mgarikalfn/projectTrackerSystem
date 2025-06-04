using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.Role
{
    public record RoleUpdateDto(string? Name, String? Description, List<String>? PermissionsToAdd);
   
    
}
