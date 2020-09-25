using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Distance;
using Autodesk.AutoCAD.DatabaseServices;
using NTSJoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSOperation
    {
        public static DBObjectCollection Trim(this Polyline polyline, Curve curve)
        {
            return ThCADCoreNTSGeometryClipper.Clip(polyline, curve);
        }

        public static double IndexedDistance(this Polyline polyline, Curve curve)
        {
            return IndexedFacetDistance.Distance(polyline.ToNTSLineString(), curve.ToNTSLineString());
        }

        public static DBObjectCollection Buffer(this Polyline polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSPolygon(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }
    }
}
