using System.IO;
using Newtonsoft.Json;
using NetTopologySuite.IO;
using NetTopologySuite.Features;

namespace ThMEPEngineCore.IO
{
    public class ThMEPGeoJSONService
    {
        public static FeatureCollection Export2NTSFeatures(string geojson)
        {
            var serializer = GeoJsonSerializer.Create();
            using (var stringReader = new StringReader(geojson))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                return serializer.Deserialize<FeatureCollection>(jsonReader);
            }
        }

        public static string Features2GeoJSON(FeatureCollection features)
        {
            var serializer = GeoJsonSerializer.Create();
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter)
            {
                Indentation = 4,
                IndentChar = ' ',
                Formatting = Formatting.Indented,
            })
            {
                serializer.Serialize(jsonWriter, features);
                return stringWriter.ToString();
            }
        }

        public static void Export2File(string geojson, string file)
        {
            using (StreamWriter outputFile = new StreamWriter(file))
            {
                outputFile.Write(geojson);
            }
        }
    }
}
