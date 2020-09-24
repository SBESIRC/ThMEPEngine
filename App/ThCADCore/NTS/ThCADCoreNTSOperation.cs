using System;
using NetTopologySuite.Geometries;
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
            var objs = new DBObjectCollection();
            var polygon = polyline.ToNTSPolygon();
            var parameters = new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            };
            var buffer = BufferOp.Buffer(polygon, distance, parameters);
            if (buffer.IsEmpty)
            {
                return objs;
            }
            if (buffer is Polygon bufferPolygon)
            {
                objs.Add(bufferPolygon.Shell.ToDbPolyline());
            }
            else if (buffer is MultiPolygon mPolygon)
            {
                foreach(Polygon item in mPolygon.Geometries)
                {
                    objs.Add(item.Shell.ToDbPolyline());
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
        }
    }
}
