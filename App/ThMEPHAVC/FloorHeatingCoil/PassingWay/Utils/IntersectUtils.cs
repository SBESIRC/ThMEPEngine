using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public static class IntersectUtils
    {
        public static List<Point3d> PolylineIntersectionPolyline(Polyline a,Polyline b)
        {
            var inter = a.Intersect(b, Intersect.OnBothOperands).ToHashSet();
            var points = PassageWayUtils.GetPolyPoints(a);
            var s = PassageWayUtils.GetPointIndex(b.StartPoint, points, 1e-3);
            if (s == -1) s = PassageWayUtils.GetSegIndexOnPolyline(b.StartPoint, points);
            if (s != -1)
                inter.Add(b.StartPoint);
            var e = PassageWayUtils.GetPointIndex(b.EndPoint, points, 1e-3);
            if (e == -1) e = PassageWayUtils.GetSegIndexOnPolyline(b.EndPoint, points);
            if (e != -1)
                inter.Add(b.EndPoint);
            points = PassageWayUtils.GetPolyPoints(b);
            s = PassageWayUtils.GetPointIndex(a.StartPoint, points, 1e-3);
            if (s == -1) s = PassageWayUtils.GetSegIndexOnPolyline(a.StartPoint, points);
            if (s != -1)
                inter.Add(a.StartPoint);
            e = PassageWayUtils.GetPointIndex(a.EndPoint, points, 1e-3);
            if (e == -1) e = PassageWayUtils.GetSegIndexOnPolyline(a.EndPoint, points);
            if (e != -1)
                inter.Add(b.EndPoint);
            return inter.ToList();
            //var coords = a.ToNTSLineString().Intersection(b.ToNTSLineString()).Coordinates.ToHashSet();
            //return coords.ToPoint3dList();
        }
        public static Polyline PolylineIntersectionPolygon(Polyline a,Polyline b)
        {
            return a.ToNTSLineString().Intersection(b.ToNTSPolygon()).ToDbCollection().Cast<Polyline>().First();
        }
        public static Point3d LineIntersectionPolygon(this Line line,Polyline other)
        {
            var geometry = line.ToNTSLineString().Intersection(other.ToNTSLineString());
            if (geometry is Point point)
            {
                return point.ToAcGePoint3d();
            }
            else if(geometry is MultiPoint multi)
            {
                return (multi.OrderBy(o => o.Distance(line.StartPoint.ToNTSPoint())).First() as Point).ToAcGePoint3d();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public static Point3d GetClosedPointOnShell(Point3d point, Polyline shell, bool OnHLine = false, double offset = 100000)
        {
            if (OnHLine == true)
            {
                var left = new Point3d(point.X - offset, point.Y, 0);
                var right = new Point3d(point.X + offset, point.Y, 0);
                var line = new Line(left, right);
                var points = line.ToNTSLineString().Intersection(shell.ToNTSLineString()).Coordinates.ToHashSet();
                var point_H = points.ToPoint3dList().FindByMin(o => o.DistanceTo(point));
                line.Dispose();
                return point_H;
            }
            else
            {
                var up = new Point3d(point.X, point.Y + offset, 0);
                var down = new Point3d(point.X, point.Y - offset, 0);
                var line = new Line(up, down);
                var points = line.ToNTSLineString().Intersection(shell.ToNTSLineString()).Coordinates.ToHashSet();
                var point_V = points.ToPoint3dList().FindByMin(o => o.DistanceTo(point));
                line.Dispose();
                return point_V;
            }

            //var line = new Polyline();
            //line.AddVertexAt(0, left.ToPoint2D(), 0, 0, 0);
            //line.AddVertexAt(1, right.ToPoint2D(), 0, 0, 0);

        }
        static List<Point3d> ToPoint3dList(this HashSet<Coordinate> coords)
        {
            var ret = new List<Point3d>();
            foreach (var coord in coords)
                ret.Add(coord.ToAcGePoint3d());
            return ret;
        }
        /// <summary>
        /// 多段线插入点
        /// </summary>
        /// <param name="p"></param>
        /// <param name="points"></param>
        /// <param name="eps"></param>
        public static void InsertPoint(Point3d p, ref List<Point3d> points, double eps = 1e-5)
        {
            for (int j = 0; j < points.Count - 1; j++)
                if (PassageWayUtils.PointOnSegment(p, points[j], points[j + 1], eps))
                {
                    points.Insert(j + 1, p);
                    return;
                }
        }
    }
}
