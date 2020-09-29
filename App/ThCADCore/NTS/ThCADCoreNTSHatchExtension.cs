using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSHatchExtension
    {
        public static MultiPolygon ToNTSPolygons(this Hatch hatch)
        {
            // 支持有多个“环”的填充
            var polygons = hatch.ToPolylines().Select(o => o.ToNTSPolygon());
            // 不支持有“洞”的填充
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons.ToArray());
        }
    }
}
