using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.GeoJSON;

namespace ThMEPEngineCore.IO
{
    public class ThGeoOutput
    {
        public static void Output(List<ThGeometry> geos,string path,string fileName)
        {
            var stream =  File.Create(Path.Combine(path, string.Format("{0}.Info.geojson", fileName)));
            using (StreamWriter geoJson = new StreamWriter(stream,System.Text.Encoding.Default))
            using (JsonTextWriter writer = new JsonTextWriter(geoJson)
            {
                Indentation = 4,
                IndentChar = ' ',
                Formatting = Formatting.Indented,
            })
            {
                var geoJsonWriter = new ThGeometryJsonWriter();
                geoJsonWriter.Write(geos, writer);
            }
        }

        public static string Output(List<ThGeometry> geos)
        {
            using (StringWriter geoJson = new StringWriter())
            using (JsonTextWriter writer = new JsonTextWriter(geoJson)
            {
                Indentation = 4,
                IndentChar = ' ',
                Formatting = Formatting.Indented,
            })
            {
                var geoJsonWriter = new ThGeometryJsonWriter();
                geoJsonWriter.Write(geos, writer);
                return geoJson.ToString();
            }
        }
    }
}
