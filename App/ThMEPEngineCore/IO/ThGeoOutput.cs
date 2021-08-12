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
            var geoJsonWriter = new ThGeometryJsonWriter();
            geoJsonWriter.Write(geos, Path.Combine(path, string.Format("{0}.Info.geojson", fileName)));
        }

        public static string Output(List<ThGeometry> geos)
        {
            var geoJsonWriter = new ThGeometryJsonWriter();
            return geoJsonWriter.Write(geos);
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
