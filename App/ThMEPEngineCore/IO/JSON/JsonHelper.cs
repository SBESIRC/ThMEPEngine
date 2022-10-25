using System.Text.Json;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.JSON
{
    public class JsonHelper
    {
        public static string SerializeObject(object o)
        {
            return JsonSerializer.Serialize(o);
        }

        public static T DeserializeJsonToObject<T>(string json) where T : class
        {
            return JsonSerializer.Deserialize<T>(json);
        }

        public static List<T> DeserializeJsonToList<T>(string json) where T : class
        {
            return JsonSerializer.Deserialize<List<T>>(json);
        }
    }
}
