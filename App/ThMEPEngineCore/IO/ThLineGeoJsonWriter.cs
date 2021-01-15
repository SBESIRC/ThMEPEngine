using System.Linq;
using ThCADCore.NTS;
using Newtonsoft.Json;
using Dreambuild.AutoCAD;
using NetTopologySuite.IO;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Features;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.IO
{
    public class ThLineGeoJsonWriter : GeoJsonWriter
    {
        public void Write(List<Line> lines, JsonWriter writer)
        {
            ThLineFeatureCollection.Construct(lines)
                .OrderBy(o => o.Geometry, new ThCADCoreNTSGeometryComparer()).ForEach(o => Write(o, writer));
        }
    }
}
