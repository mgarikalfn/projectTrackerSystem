using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto.User
{
    public class CreateUserDto
    {
        public string FirstName { get; set; } = String.Empty;
        public string LastName { get; set; } = String.Empty;
        public string AccountId { get; set; } = String.Empty;
        public string Email {  get; set; } = String.Empty;

    }
}
