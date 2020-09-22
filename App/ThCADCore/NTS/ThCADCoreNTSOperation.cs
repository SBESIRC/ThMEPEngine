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
            return polyline.Clip(curve);
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

        public static DBObjectCollection Clip(this Polyline polyline, Curve curve, bool inverted = false)
        {
            var objs = new DBObjectCollection();
            var geometry = curve.ToNTSGeometry();
            var polygon = polyline.ToNTSPolygon();
            var result = ClipGeometry(polygon, geometry, inverted);
            if (result.IsEmpty)
            {
                return objs;
            }

            if (result is MultiLineString lineStrings)
            {
                foreach (LineString lineString in lineStrings.Geometries)
                {
                    objs.Add(lineString.ToDbPolyline());
                }
            }
            else if (result is LineString lineStr)
            {
                if (lineStr.StartPoint != null && lineStr.EndPoint != null)
                {
                    objs.Add(lineStr.ToDbPolyline());
                }
            }
            else if (result is GeometryCollection collection)
            {
                foreach (var col in collection.Geometries)
                {
                    if (col is LineString line)
                    {
                        objs.Add(line.ToDbPolyline());
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            return objs;
        }

        private static Geometry ClipGeometry(Polygon polygon, Geometry geometry, bool inverted = false)
        {
            Geometry other = geometry;
            if (other is LinearRing ring)
            {
                // 若是多面形，则转换成面面相切
                other = ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon(ring);
            }
            return inverted ? other.Difference(polygon) : polygon.Intersection(other);
        }
    }
}
