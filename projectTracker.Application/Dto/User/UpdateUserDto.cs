using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.User
{
    public class UpdateUserDto
    {
        public string FirstName { get; set; } = String.Empty;
        public string LastName { get; set; } = String.Empty;
        public bool IsActive { get; set; } = true;

        //Additional informaition

        public string AccountId { get; set; } = String.Empty;
        public string DisplayName { get; set; } = String.Empty;
        public string AvatarUrl { get; set; } = String.Empty;
        public string TimeZone { get; set; } = String.Empty;
        public decimal CurrentWorkload { get; set; } = 0;
        public string Location { get; set; } = String.Empty;
    }
}
