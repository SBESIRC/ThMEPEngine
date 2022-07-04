using System.IO;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.GeoJSON;
using System.Collections.Generic;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;

namespace ThMEPEngineCore.IO.WKT
{
    public class ThGeometryWKTWriter : WKTWriter
    {
        public string Write(List<ThGeometry> geos)
        {
            using (var writer = new StringWriter())
            {
                Write(geos, writer);
                return writer.ToString();
            }
        }

        public void Write(List<ThGeometry> geos, string path)
        {
            var stream = File.Create(path);
            using (StreamWriter writer = new StreamWriter(stream))
            {
                Write(geos, writer);
            }
        }

        private void Write(List<ThGeometry> geos, TextWriter writer)
        {
            var wkt = new WKTWriter();
            wkt.WriteFormatted(ToNTSGeometryCollection(geos), writer);
        }

        private GeometryCollection CreateGeometryCollection(Geometry[] geometries)
        {
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateGeometryCollection(geometries);
        }

        private Geometry ToNTSGeometryCollection(List<ThGeometry> geos)
        {
            var features = ThGeometryFeatureCollection.Construct(geos);
            return CreateGeometryCollection(features.Select(f => f.Geometry).ToArray());
        }
    }
}
