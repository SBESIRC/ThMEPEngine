using ThCADCore.NTS;
using GeoAPI.Geometries;
using NetTopologySuite.Operation.Union;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Linemerge;

namespace ThCADCore.Geometry
{
    public static class ThCADCorePolylineExtension
    {
        public static DBObjectCollection Preprocess(this Polyline polyline)
        {
            var merger = new LineMerger();
            merger.Add(UnaryUnionOp.Union(polyline.ToNTSLineString()));
            var objs = new DBObjectCollection();
            foreach (ILineString segment in merger.GetMergedLineStrings())
            {
                objs.Add(segment.ToDbPolyline());
            }
            return objs;
        }
    }
}
