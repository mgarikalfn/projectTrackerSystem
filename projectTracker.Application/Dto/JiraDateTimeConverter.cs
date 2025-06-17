using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace projectTracker.Application.Dto
{
    public class JiraDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var dateString = reader.GetString();
                // Jira dates can be complex. Try multiple formats.
                // ISO 8601 with timezone: "2023-10-26T10:00:00.000+0000"
                // Or sometimes just "yyyy-MM-dd"
                if (DateTime.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime result))
                {
                    return result;
                }
                // Fallback for simple date without time
                if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out result))
                {
                    return result;
                }
            }
            return default; // Or throw an exception, depending on error handling strategy
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Standard ISO 8601 format for consistency
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
        }
    }
}
