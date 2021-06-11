using System.IO;
using System.Text;
using Newtonsoft.Json;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.GeoJSON;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO
{
    public class ThGeoOutput
    {
        public static void Output(List<ThGeometry> geos,string path,string fileName)
        {
            var stream =  File.Create(Path.Combine(path, string.Format("{0}.Info.geojson", fileName)));
            using (StreamWriter geoJson = new StreamWriter(stream, new UTF8Encoding(false)))
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
            using (var geoJson = new ExtentedStringWriter(new UTF8Encoding(false)))
            using (var writer = new JsonTextWriter(geoJson)
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
    public sealed class ExtentedStringWriter : StringWriter
    {
        private readonly Encoding stringWriterEncoding;
        public ExtentedStringWriter(Encoding desiredEncoding)
            : base()
        {
            this.stringWriterEncoding = desiredEncoding;
        }

        public override Encoding Encoding
        {
            get
            {
                return this.stringWriterEncoding;
            }
        }
    }
}
