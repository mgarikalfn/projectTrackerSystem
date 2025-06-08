using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace projectTracker.Application.Dto
{
    public class JiraDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                var dateString = reader.GetString();

                if (string.IsNullOrEmpty(dateString))
                    return DateTime.MinValue;

                // Handle Jira's format with +0000 timezone
                if (dateString.EndsWith("+0000"))
                {
                    dateString = dateString.Substring(0, dateString.Length - 5) + "Z";
                }
                // Handle other possible Jira formats if needed

                if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date))
                {
                    return date;
                }

                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O")); // ISO 8601 format
        }
    }
}