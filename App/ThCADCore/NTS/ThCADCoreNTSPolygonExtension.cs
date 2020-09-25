﻿using NetTopologySuite.Simplify;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Operation.Predicate;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolygonExtension
    {
        public static DBObjectCollection Difference(this AcPolygon polygon, DBObjectCollection curves)
        {
            return polygon.ToNTSPolygon().Difference(curves.UnionGeometries()).ToDbCollection();
        }

        public static bool Contains(this AcPolygon polygon, Point3d pt)
        {
            var locator = new SimplePointInAreaLocator(polygon.ToNTSPolygon());
            return locator.Locate(pt.ToNTSCoordinate()) == Location.Interior;
        }

        public static bool IndexedContains(this AcPolygon polygon, Point3d pt)
        {
            var locator = new IndexedPointInAreaLocator(polygon.ToNTSPolygon());
            return locator.Locate(pt.ToNTSCoordinate()) == Location.Interior;
        }

        public static bool Contains(this AcPolygon polygon, Curve curve)
        {
            return polygon.ToNTSPolygon().Contains(curve.ToNTSGeometry());
        }

        public static bool Intersects(this AcPolygon polygon, Curve curve)
        {
            return polygon.ToNTSPolygon().Intersects(curve.ToNTSGeometry());
        }

        public static bool RectIntersects(this AcPolygon polygon, Curve curve)
        {
            return RectangleIntersects.Intersects(polygon.ToNTSPolygon(), curve.ToNTSGeometry());
        }

        public static bool RectContains(this AcPolygon polygon, Curve curve)
        {
            return RectangleContains.Contains(polygon.ToNTSPolygon(), curve.ToNTSGeometry());
        }

        public static DBObjectCollection SplitBy(this AcPolygon polygon, Curve curve)
        {
            var geometries = new Geometry[]
            {
                polygon.ToNTSPolygon(),
                curve.ToNTSLineString()
            };

            var objs = new DBObjectCollection();
            foreach (Polygon geometry in UnaryUnionOp.Union(geometries).Polygonize())
            {
                objs.Add(geometry.Shell.ToDbPolyline());
            }
            return objs;
        }

        /// <summary>
        /// 预处理封闭多段线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static DBObjectCollection PreprocessAsPolygon(this Polyline polyline)
        {
            // 剔除重复点（在一定公差范围内）
            // 鉴于主要的使用场景是建筑底图，选择1毫米作为公差
            var result = TopologyPreservingSimplifier.Simplify(polyline.ToFixedNTSLineString(), 1.0);

            // 自相交处理
            var polygons = result.Polygonize();

            // 返回结果
            var objs = new DBObjectCollection();
            foreach (Polygon polygon in polygons)
            {
                objs.Add(polygon.Shell.ToDbPolyline());
            }
            return objs;
        }
    }
}
