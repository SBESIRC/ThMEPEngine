using System.IO;
using System.Text;
using Newtonsoft.Json;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Interface;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThGeometryJsonWriter : GeoJsonWriter, IThGeometryWriter
    {
        public string Write(List<ThGeometry> geos)
        {
            using (var geoJson = new ExtentedStringWriter(new UTF8Encoding(false)))
            using (var writer = new JsonTextWriter(geoJson)
            {
                Indentation = 4,
                IndentChar = ' ',
                Formatting = Formatting.Indented,
            })
            {
                Write(geos, writer);
                return geoJson.ToString();
            }
        }

        public void Write(List<ThGeometry> geos, string path)
        {
            var stream = File.Create(path);
            using (StreamWriter geoJson = new StreamWriter(stream, new UTF8Encoding(false)))
            using (JsonTextWriter writer = new JsonTextWriter(geoJson)
            {
                Indentation = 4,
                IndentChar = ' ',
                Formatting = Formatting.Indented,
            })
            {                
                Write(geos, writer);
            }
        }

        private void Write(List<ThGeometry> geos, JsonWriter writer)
        {
            Write(ThGeometryFeatureCollection.Construct(geos), writer);
        }
    }
}
