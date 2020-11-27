using System;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
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

        public static DBObjectCollection BuildArea(this DBObjectCollection objs)
        {
            var poylgons = new DBObjectCollection();
            var builder = new ThCADCoreNTSBuildArea();
            Geometry geometry = builder.Build(objs.Explode().ToMultiLineString());
            if (geometry is Polygon polygon)
            {
                poylgons.Add(polygon.ToMPolygon());
            }
            else if (geometry is MultiPolygon mPolygons)
            {
                mPolygons.Geometries.Cast<Polygon>().ForEach(o =>
                {
                    poylgons.Add(o.ToMPolygon());
                });
            }
            else
            {
                throw new NotSupportedException();
            }
            return poylgons;
        }
    }
}
