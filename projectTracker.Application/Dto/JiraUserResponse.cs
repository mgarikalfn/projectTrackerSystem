using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace projectTracker.Application.Dto
{
    public class JiraUserResponse
    {
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; } = string.Empty;

        [JsonPropertyName("emailAddress")]
        public string? EmailAddress { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("avatarUrls")]
        public Dictionary<string, string>? AvatarUrls { get; set; } // Dictionary for different sizes
    }
}
