using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThMEPEngineCore.IO.JSON
{
    public class StringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {

            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? String.Empty;
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                var stringValue = reader.GetDouble();
                return stringValue.ToString();
            }
            else if (reader.TokenType == JsonTokenType.False ||
                reader.TokenType == JsonTokenType.True)
            {
                return reader.GetBoolean().ToString();
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
                reader.Skip();
                return "(not supported)";
            }
            else
            {
                throw new System.Text.Json.JsonException();
            }
        }

        public override void Write(Utf8JsonWriter writer, 
            string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}