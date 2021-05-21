using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSMPolygonExtension
    {
        public static MPolygon ToDbMPolygon(this Polygon polygon)
        {
            List<Curve> holes = new List<Curve>();
            var shell = polygon.Shell.ToDbPolyline();
            polygon.Holes.ForEach(o => holes.Add(o.ToDbPolyline()));
            return ThMPolygonTool.CreateMPolygon(shell, holes);
        }

        public static MPolygon ToDbMPolygon(this MultiPolygon multiPolygon)
        {
            var loops = multiPolygon.Geometries.Cast<Polygon>().Select(o => o.ToDbCollection()).ToList();
            return ThMPolygonTool.CreateMPolygon(loops);
        }
    }
}
