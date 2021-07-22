using Newtonsoft.Json;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using System.Collections.Generic;

namespace ThMEPEngineCore.IO.GeoJSON
{
    public class ThGeometryJsonWriter : GeoJsonWriter
    {
        public void Write(List<ThGeometry> geos, JsonWriter writer)
        {
            Write(ThGeometryFeatureCollection.Construct(geos), writer);
        }
    }
}
