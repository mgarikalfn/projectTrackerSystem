using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    public class UsersDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool Active { get; set; }
        public string Source { get; set; } = string.Empty; // e.g., "Jira"
    }
}
