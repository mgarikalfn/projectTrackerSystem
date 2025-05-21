using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    public class JiraProjectDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public JiraLeadDto? Lead { get; set; }
    }

    public class JiraProjectDescriptionDto
    {
        public string? Content { get; set; } = string.Empty;
    }

    public class JiraLeadDto
    {
        public string DisplayName { get; set; } = string.Empty;
    }

}
