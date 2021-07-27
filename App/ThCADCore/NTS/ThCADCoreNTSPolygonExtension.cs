﻿using System;
using NFox.Cad;
using System.Linq;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Algorithm.Locate;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;
using ThCADExtension;


namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolygonExtension
    {
        public static DBObjectCollection Difference(this AcPolygon polygon, AcPolygon other)
        {
            return polygon.ToNTSPolygon().Difference(other.ToNTSPolygon()).ToDbCollection();
        }

        public static DBObjectCollection Difference(this AcPolygon polygon, DBObjectCollection curves)
        {
            return polygon.ToNTSPolygon().Difference(curves.UnionGeometries()).ToDbCollection();
        }

        public static DBObjectCollection DifferenceMP(this AcPolygon polygon, DBObjectCollection curves)
        {
            return polygon.ToNTSPolygon().Difference(curves.UnionGeometries()).ToDbCollection(true);
        }

        public static DBObjectCollection Intersection(this AcPolygon polygon, DBObjectCollection curves)
        {
            return polygon.ToNTSPolygon().Intersection(curves.UnionGeometries()).ToDbCollection();
        }

        public static bool Contains(this AcPolygon polygon, Point3d pt)
        {
            var locator = new SimplePointInAreaLocator(polygon.ToNTSPolygon());
            return locator.Locate(pt.ToNTSCoordinate()) == Location.Interior;
        }
        public static bool Contains2(this AcPolygon polygon, Point3d pt)
        {
            Point3dCollection pts = new Point3dCollection();
            Line line = new Line(pt, new Point3d(0, 0, 0));
            polygon.IntersectWith(line, Intersect.OnBothOperands, pts, 0, 0);
             
            return pts.Count % 2 != 0;
        }
        public static bool Contains3(this AcPolygon polygon, Point3d pt)
        {
            DBObjectCollection dbobjColl = new DBObjectCollection();
            polygon.Explode(dbobjColl);

            var targetLineSeg = new LineSegment2d(Point2d.Origin, pt.ToPoint2D());
            int nCross = 0;

            foreach (var obj in dbobjColl)
            {
                var lineObj = obj as Line;
                if (lineObj == null) continue;

                var lineSeg = new LineSegment2d(lineObj.StartPoint.ToPoint2D(), lineObj.EndPoint.ToPoint2D());

                
                if (targetLineSeg.IntersectWith(lineSeg) == null) continue;

                nCross++;
            }
            return nCross % 2 == 1;
        }
        public static bool ContainsOrOnBoundary(this AcPolygon polygon, Point3d pt)
        {
            var locator = new SimplePointInAreaLocator(polygon.ToNTSPolygon());
            var locateRes = locator.Locate(pt.ToNTSCoordinate());
            return locateRes == Location.Interior || locateRes == Location.Boundary;
        }

        public static bool OnBoundary(this AcPolygon polygon, Point3d pt)
        {
            var locator = new SimplePointInAreaLocator(polygon.ToNTSPolygon());
            var locateRes = locator.Locate(pt.ToNTSCoordinate());
            return locateRes == Location.Boundary;
        }

        public static bool OnBoundary(this Polygon polygon, Point3d pt)
        {
            var locator = new SimplePointInAreaLocator(polygon);
            var locateRes = locator.Locate(pt.ToNTSCoordinate());
            return locateRes == Location.Boundary;
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

        public static bool Intersects(this AcPolygon polygon, Entity entity)
        {
            return polygon.ToNTSPolygon().Intersects(entity.ToNTSGeometry());
        }

        public static bool LineIntersects(this AcPolygon polygon, Entity entity)
        {
            return polygon.ToNTSLineString().Intersects(entity.ToNTSGeometry());
        }

        public static AcPolygon OBB(this AcPolygon polygon)
        {
            // GetMinimumRectangle()对于非常远的坐标（WCS下，>10E10)处理的不好
            // Workaround就是将位于非常远的图元临时移动到WCS原点附近，参与运算
            // 运算结束后将运算结果再按相同的偏移从WCS原点附近移动到其原始位置
            var clone = polygon.Clone() as AcPolygon;
            var center = clone.GetCentroidPoint();
            var vector = center.GetVectorTo(Point3d.Origin);
            var matrix = Matrix3d.Displacement(vector);
            clone.TransformBy(matrix);
            var result = clone.GetMinimumRectangle();
            result.TransformBy(matrix.Inverse());
            return result;
        }

        public static bool IsRectangle(this AcPolygon polygon)
        {
            return polygon.IsSimilar(OBB(polygon), 0.99);
        }

        public static Point3d GetCentroidPoint(this AcPolygon polygon)
        {
            return Centroid.GetCentroid(polygon.ToNTSPolygon()).ToAcGePoint3d();
        }

        public static Point3d GetMaximumInscribedCircleCenter(this AcPolygon shell)
        {
            return shell.ToNTSPolygon().GetMaximumInscribedCircleCenter();
        }

        public static DBObjectCollection MakeValid(this AcPolygon polygon)
        {
            // zero-width buffer trick:
            //  http://lin-ear-th-inking.blogspot.com/2020/12/fixing-buffer-for-fixing-polygons.html
            // self-union trick:
            //  http://lin-ear-th-inking.blogspot.com/2020/06/jts-overlayng-tolerant-topology.html
            return polygon.ToNTSPolygon().Buffer(0).ToDbCollection();
        }

        public static DBObjectCollection GeometryIntersection(this AcPolygon polyFirst, AcPolygon polySec)
        {
            return polyFirst.ToNTSPolygon().Intersection(polySec.ToNTSPolygon())
                .ToDbCollection()
                .Cast<Entity>()
                .Where(o => o is AcPolygon)
                .ToCollection();
        }
    }
}
