using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using projectTracker.Domain.Entities;

namespace projectTracker.Application.Dto.User
{
    public class UserFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
       
        public string? SearchTerm { get; set; }
        public UserSource? Source { get; set; }
        public string? Role { get; set; } 
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
    }
}
