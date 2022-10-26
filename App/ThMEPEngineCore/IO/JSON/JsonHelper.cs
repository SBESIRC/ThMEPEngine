using System.Text.Json;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.JSON
{
    public class JsonHelper
    {
        private static JsonSerializerOptions _serializerOptions;

        public static string SerializeObject(object o)
        {
            return JsonSerializer.Serialize(o);
        }

        public static T DeserializeJsonToObject<T>(string json) where T : class
        {
            return JsonSerializer.Deserialize<T>(json, SerializerOptions());
        }

        public static List<T> DeserializeJsonToList<T>(string json) where T : class
        {
            return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions());
        }

        private static JsonSerializerOptions SerializerOptions()
        {
            if (_serializerOptions == null)
            {
                _serializerOptions = new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                };
                _serializerOptions.Converters.Add(new StringConverter());
            }
            return _serializerOptions;
        }
    }
}
