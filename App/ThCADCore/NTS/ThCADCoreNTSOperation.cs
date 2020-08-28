using System;
using GeoAPI.Geometries;
using NetTopologySuite.Operation.Buffer;
using Autodesk.AutoCAD.DatabaseServices;
using NTSJoinStyle = GeoAPI.Operation.Buffer.JoinStyle;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSOperation
    {
        public static DBObjectCollection Trim(this Polyline polyline, Polyline curve)
        {
            var objs = new DBObjectCollection();
            var other = curve.ToNTSLineString();
            var polygon = polyline.ToNTSPolygon();
            var result = polygon.Intersection(other);
            if (result is IMultiLineString lineStrings)
            {
                foreach (ILineString lineString in lineStrings.Geometries)
                {
                    objs.Add(lineString.ToDbPolyline());
                }
            }
            else if (result is ILineString lineStr)
            {
                objs.Add(lineStr.ToDbPolyline());
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
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
            if (buffer is IPolygon bufferPolygon)
            {
                objs.Add(bufferPolygon.Shell.ToDbPolyline());
            }
            else if (buffer is IMultiPolygon mPolygon)
            {
                foreach(IPolygon item in mPolygon.Geometries)
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
