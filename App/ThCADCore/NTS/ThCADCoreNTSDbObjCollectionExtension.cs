using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Geometries.Utilities;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSDbObjCollectionExtension
    {
        public static MultiPolygon ToNTSMultiPolygon(this DBObjectCollection objs)
        {
            var polygons = objs.Cast<Entity>().Select(o => o.ToNTSPolygonalGeometry());
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons.ToArray());
        }

        public static MultiLineString ToMultiLineString(this DBObjectCollection objs)
        {
            var lineStrings = objs.Cast<Curve>().Where(x=>!(x is Spline)).Select(o => o.ToNTSLinealGeometry());
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiLineString(lineStrings.ToArray());
        }

        public static GeometryCollection ToGeometryCollection(this DBObjectCollection objs)
        {
            var geometries = objs.OfType<Entity>().Select(e => e.ToNTSGeometry());
            return ThCADCoreNTSService.Instance.GeometryFactory.CreateGeometryCollection(geometries.ToArray());
        }

        public static Geometry ToNTSNodedLineStrings(this MultiLineString linestrings)
        {
            // UnaryUnionOp.Union()有Robust issue
            // 会抛出"non-noded intersection" TopologyException
            // OverlayNGRobust.Union()在某些情况下仍然会抛出TopologyException (NTS 2.2.0)
            Geometry lineString = ThCADCoreNTSService.Instance.GeometryFactory.CreateLineString();
            return OverlayNGRobust.Overlay(linestrings, lineString, SpatialFunction.Union);
        }

        public static Geometry ToNTSNodedLineStrings(this DBObjectCollection objs)
        {
            return objs.ToMultiLineString().ToNTSNodedLineStrings();
        }

        public static Geometry UnionGeometries(this DBObjectCollection curves)
        {
            return OverlayNGRobust.Union(curves.ToNTSMultiPolygon().Geometries);
        }

        public static DBObjectCollection UnionPolygons(this DBObjectCollection curves, bool keepHole = false)
        {
            if (curves.Count > 0)
            {
                return curves.UnionGeometries().ToDbCollection(keepHole);
            }
            return new DBObjectCollection();
        }

        public static DBObjectCollection BufferPolygons(this DBObjectCollection polygons, double distance)
        {
            var bufferPara = new BufferParameters()
            {
                JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Mitre,
                EndCapStyle = NetTopologySuite.Operation.Buffer.EndCapStyle.Square
            };
            return polygons.ToNTSMultiPolygon().Buffer(distance, bufferPara).ToDbCollection();
        }

        public static Geometry Intersection(this DBObjectCollection curves, Curve curve)
        {
            return OverlayNGRobust.Overlay(
                curves.ToMultiLineString(),
                curve.ToNTSGeometry(),
                SpatialFunction.Intersection);
        }

        public static Polyline GetMinimumRectangle(this DBObjectCollection curves)
        {
            // GetMinimumRectangle()对于非常远的坐标（WCS下，>10E10)处理的不好
            // Workaround就是将位于非常远的图元临时移动到WCS原点附近，参与运算
            // 运算结束后将运算结果再按相同的偏移从WCS原点附近移动到其原始位置
            var geometry = curves.Combine();
            var rectangle = MinimumDiameter.GetMinimumRectangle(geometry);
            if (rectangle is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Geometry Combine(this DBObjectCollection curves)
        {
            return GeometryCombiner.Combine(curves.ToMultiLineString().Geometries);
        }

        public static DBObjectCollection ToDbCollection(this Geometry geometry, bool keepHoles = false)
        {
            return geometry.ToDbObjects(keepHoles).ToCollection();
        }

        public static DBObjectCollection Tessellate(this DBObjectCollection curves)
        {
            var objs = new DBObjectCollection();
            foreach (Entity e in curves)
            {
                if (e is Ellipse ellipse)
                {
                    objs.Add(ellipse.Tessellate(ThCADCoreNTSService.Instance.ChordHeightTolerance));
                }
                else
                {
                    objs.Add(e);
                }
            }
            return objs;
        }
    }
}