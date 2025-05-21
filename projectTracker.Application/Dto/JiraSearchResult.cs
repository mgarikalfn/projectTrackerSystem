using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    public class JiraSearchResult
    {
        public List<JiraIssueDto> Issues { get; set; } = new();
    }

    public class JiraIssueDto
    {
        public string Key { get; set; } = string.Empty;
        public JiraIssueFieldsDto Fields { get; set; } = new();
    }

    public class JiraIssueFieldsDto
    {
        public string Summary { get; set; } = string.Empty;
        public JiraStatusDto Status { get; set; } = new();
        public JiraAssigneeDto? Assignee { get; set; }
        public DateTime Updated { get; set; }
        [JsonPropertyName("customfield_10035")] // Add this attribute
        public double? StoryPoints { get; set; }
    }
    public class JiraStatusDto
    {
        public string Name { get; set; } = string.Empty;
        public JiraStatusCategoryDto StatusCategory { get; set; } = new();
    }

    public class JiraStatusCategoryDto
    {
        public string Key { get; set; } = string.Empty;  // "done", "in-progress", etc.
    }

    public class JiraAssigneeDto
    {
        public string AccountId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

}
