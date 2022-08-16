using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
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
    public static class PassageWayUtils
    {
        public static List<Polyline> Buffer(Polyline frame, double distance)
        {
            var points = PassageWayUtils.GetPolyPoints(frame);
            points = SmoothUtils.SmoothPoints(points);
            points.Add(points.First());
            Polyline newPl = PassageWayUtils.BuildPolyline(points); 
            var results = newPl.Buffer(distance);
            newPl.Dispose();
            return results.Cast<Polyline>().ToList();
        }
        public static Polyline HVBuffer(Polyline poly,double distance)
        {
            var list_buffer = new List<Polyline>();
            for (int i = 0; i < poly.NumberOfVertices - 1; ++i) 
            {
                var p0 = poly.GetPoint3dAt(i);
                var p1 = poly.GetPoint3dAt(i + 1);
                if (i > 0)
                    p0 += (p0 - p1).GetNormal() * distance;
                if (i < poly.NumberOfVertices - 2)
                    p1 += (p1 - p0).GetNormal() * distance;
                var line = new Line(p0, p1);
                list_buffer.Add(line.Buffer(distance));
                line.Dispose();
            }
            var ret = list_buffer.ToArray().ToCollection().UnionPolygons().Cast<Polyline>().First();
            foreach (var polyline in list_buffer)
                polyline.Dispose();
            return ret;
        }
        public static List<Point3d> GetPolyPoints(Polyline poly, bool first_equal_last = false)
        {
            var points = Enumerable.Range(0, poly.NumberOfVertices).Select(i => poly.GetPoint3dAt(i)).ToList();
            if (first_equal_last)
            {
                if (points.First() != points.Last())
                    points.Add(points.First());
            }
            else
            {
                if (points.First() == points.Last())
                    points.RemoveAt(points.Count - 1);
            }
            if (poly.ToNTSPolygon().Shell.IsCCW)
                points.Reverse();
            return points;
        }
        /// <summary>
        /// 判断点是否在线段上（不包含端点）
        /// </summary>
        /// <param name="p">目标点</param>
        /// <param name="s">线段端点</param>
        /// <param name="e">线段端点</param>
        /// <param name="eps">容差</param>
        /// <returns></returns>
        public static bool PointOnSegment(Point3d p, Point3d s, Point3d e, double eps = 2)
        {
            if (Math.Abs(s.X - e.X) < eps)
                return Math.Abs(p.X - s.X) < eps && p.Y > Math.Min(s.Y, e.Y) && p.Y < Math.Max(s.Y, e.Y);
            if (Math.Abs(s.Y - e.Y) < eps)
                return Math.Abs(p.Y - s.Y) < eps && p.X > Math.Min(s.X, e.X) && p.X < Math.Max(s.X, e.X);
            return false;
        }
        /// <summary>
        /// 判断角pre-p-next是否为轮廓凹角
        /// </summary>
        /// <param name="p"></param>
        /// <param name="pre"></param>
        /// <param name="next"></param>
        /// <param name="eps"></param>
        /// <returns></returns>
        public static bool IsConCaveAngle(Point3d p, Point3d pre, Point3d next, double eps = 1e-5)
        {
            return (pre - p).CrossProduct(next - p).Z < eps;
        }
        /// <summary>
        /// 判断两个向量是否平行
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="eps">容差，角度制</param>
        /// <returns></returns>
        public static bool IsParallel(Vector3d a, Vector3d b, double eps = 3)
        {
            var angle = a.GetAngleTo(b) / Math.PI * 180;
            return Math.Abs(angle - 180) < eps || Math.Abs(angle) < eps;

        }
        public static int GetPointIndex(Point3d p, List<Point3d> points, double eps = 1)
        {
            for (int i = 0; i < points.Count; ++i)
                if (points[i].DistanceTo(p) < eps) 
                    return i;
            return -1;
        }
        /// <summary>
        /// 查找点p在多边形（首尾点不同）边上的索引
        /// </summary>
        /// <param name="p">目标点</param>
        /// <param name="points">多边形边界点集</param>
        /// <returns></returns>
        public static int GetSegIndexOnPolygon(Point3d p, List<Point3d> points)
        {
            for (int j = 0; j < points.Count; j++)
                if (PointOnSegment(p,points[j],points[(j+1)%points.Count]) || points[(j+1) % points.Count].DistanceTo(p) < 5)
                    return j;
            return -1;
        }
        /// <summary>
        /// 查找点p在多段线（或首尾点相同的多边形）上的索引
        /// </summary>
        /// <param name="p">目标点</param>
        /// <param name="points">多段线（或首尾点相同的多边形）的点集序列</param>
        /// <returns></returns>
        public static int GetSegIndexOnPolyline(Point3d p,List<Point3d> points)
        {
            for (int j = 0; j < points.Count - 1; j++)
                if (PointOnSegment(p, points[j], points[j + 1])) 
                    return j;
            return -1;
        }
        public static int GetDirBetweenTwoPoint(Point3d a, Point3d b)
        {
            var dx = Math.Sign(b.X - a.X);
            var dy = Math.Sign(b.Y - a.Y);
            var abs_dx = Math.Abs(b.X - a.X);
            var abs_dy = Math.Abs(b.Y - a.Y);
            if (abs_dx > abs_dy)
                return dx == 1 ? 0 : 2;
            else
                return dy == 1 ? 1 : 3;
        }
        public static void RearrangePoints(ref List<Point3d> points,int index)
        {
            var head = points.GetRange(0, index);
            points.RemoveRange(0, index);
            points.AddRange(head);
        }
        public static Polyline BuildPolyline(List<Point3d> points)
        {
            var shell = new Polyline();
            shell.Closed = points.Last() == points.First();
            for (int j = 0; j < points.Count; ++j)
                shell.AddVertexAt(j, points[j].ToPoint2D(), 0, 0, 0);
            return shell;
        }
        public static Polyline BuildPolyline(Line line)
        {
            var poly = new Polyline();
            poly.AddVertexAt(0, line.StartPoint.ToPoint2D(), 0, 0, 0);
            poly.AddVertexAt(1, line.EndPoint.ToPoint2D(), 0, 0, 0);
            return poly;
        }
        public static Polyline BuildRectangle(RectBox rb, int color_index = 4)
        {
            var rect = new Polyline();
            rect.Closed = true;
            rect.AddVertexAt(0, new Point2d(rb.xmin, rb.ymax), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(rb.xmin, rb.ymin), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(rb.xmax, rb.ymin), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(rb.xmax, rb.ymax), 0, 0, 0);
            rect.AddVertexAt(4, new Point2d(rb.xmin, rb.ymax), 0, 0, 0);
            rect.ColorIndex = color_index;
            return rect;
        }
        public static void ClearListPoly(List<Polyline> list)
        {
            foreach (var poly in list)
                poly.Dispose();
            list.Clear();
        }

        //新增函数
        public static int GetSegIndex2(Point3d p, List<Point3d> points)
        {
            for (int j = 0; j < points.Count-1; j++)
                if (PointOnSegment(p, points[j], points[(j + 1) % points.Count]) || p.DistanceTo(points[j+1]) < 2)
                    return j;
            return -1;
        }


    }
}
