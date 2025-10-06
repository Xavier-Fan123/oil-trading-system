using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;

namespace OilTrading.Api.Converters;

/// <summary>
/// Custom DateTime converter for consistent ISO 8601 serialization
/// Ensures all DateTime values are serialized in UTC with ISO 8601 format
/// </summary>
public class DateTimeConverter : JsonConverter<DateTime>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrWhiteSpace(dateString))
            {
                throw new JsonException("Invalid date format: empty or null string");
            }

            // Try to parse ISO 8601 format first
            if (DateTime.TryParse(dateString, null, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                // Ensure we return UTC time for consistency
                return dateTime.Kind == DateTimeKind.Unspecified ? 
                    DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : 
                    dateTime.ToUniversalTime();
            }

            throw new JsonException($"Invalid date format: {dateString}");
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always convert to UTC before serializing
        var utcDateTime = value.Kind == DateTimeKind.Unspecified ? 
            DateTime.SpecifyKind(value, DateTimeKind.Utc) : 
            value.ToUniversalTime();

        // Write in ISO 8601 format with Z suffix
        writer.WriteStringValue(utcDateTime.ToString(DateTimeFormat));
    }
}

/// <summary>
/// Custom nullable DateTime converter for consistent ISO 8601 serialization
/// Handles nullable DateTime values properly
/// </summary>
public class NullableDateTimeConverter : JsonConverter<DateTime?>
{
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // Try to parse ISO 8601 format first
            if (DateTime.TryParse(dateString, null, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                // Ensure we return UTC time for consistency
                return dateTime.Kind == DateTimeKind.Unspecified ? 
                    DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) : 
                    dateTime.ToUniversalTime();
            }

            throw new JsonException($"Invalid date format: {dateString}");
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Always convert to UTC before serializing
        var utcDateTime = value.Value.Kind == DateTimeKind.Unspecified ? 
            DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : 
            value.Value.ToUniversalTime();

        // Write in ISO 8601 format with Z suffix
        writer.WriteStringValue(utcDateTime.ToString(DateTimeFormat));
    }
}