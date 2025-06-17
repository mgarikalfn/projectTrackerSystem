using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    public class JiraProjectDto
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; } // Can be rich text, JsonElement if parsing not needed here

        [JsonPropertyName("lead")]
        public JiraUserResponse? Lead { get; set; } // Reusing JiraUserResponse for project lead
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
