using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    
    public class MenuItemDto
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public int? Order { get; set; }
        public int? ParentId { get; set; }
        public List<MenuItemDto>? Children { get; set; } = new List<MenuItemDto>();
        public string ? RequiredPermission { get; set; }
    }
}
