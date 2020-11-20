using NetTopologySuite.Operation.Buffer;
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

        public static DBObjectCollection Buffer(this Polyline polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSPolygon(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }

        public static DBObjectCollection Buffer(this DBObjectCollection objs, double distance)
        {
            var buffer = new BufferOp(objs.ToMultiLineString(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
                EndCapStyle = EndCapStyle.Flat,
            });
            return buffer.GetResultGeometry(distance).ToDbCollection();
        }
    }
}
