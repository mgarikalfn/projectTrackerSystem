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
        public string AccountId { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("avatarUrls")]
        public Dictionary<string, string> AvatarUrls { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        // Add other properties you need
        [JsonPropertyName("timeZone")]
        public string TimeZone { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }
    }
}
