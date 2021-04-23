using System.IO;
using Newtonsoft.Json;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;

namespace ThCADCore.NTS.IO
{
    public static class ThCADCoreNTSGeometryWriter
    {
        public static string ToGeoJSON(this Geometry geo)
        {
            using (StringWriter geoJson = new StringWriter())
            using (JsonTextWriter writer = new JsonTextWriter(geoJson)
            {
                Indentation = 4,
                IndentChar = ' ',
                Formatting = Formatting.Indented,
            })
            {
                var geoJsonWriter = new GeoJsonWriter();
                geoJsonWriter.Write(geo, writer);
                return geoJson.ToString();
            }
        }
    }
}
