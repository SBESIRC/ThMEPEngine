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

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{ 
    class RectBox
    {
        public double xmin, xmax, ymin, ymax;
        public double height, width;
        public Point3d center;
        public int i, j;
        public RectBox(double xmin, double xmax, double ymin, double ymax, int i, int j)
        {
            this.xmin = xmin;
            this.xmax = xmax;
            this.ymin = ymin;
            this.ymax = ymax;
            this.i = i;
            this.j = j;
            height = ymax - ymin;
            width = xmax - xmin;
            center = new Point3d((xmax + xmin) / 2, (ymax + ymin) / 2, 0);
        }
        public double DistanceTo(RectBox b)
        {
            if (i == b.i && j == b.j)
                return 0;
            if (Math.Abs(i - b.i) + Math.Abs(j - b.j) != 1)
                return 120000;
            var dv = b.center - center;
            return Math.Abs(dv.X) + Math.Abs(dv.Y);
        }
    }
    class PassageWayUtils
    {
        public static List<Polyline> Buffer(Polyline frame, double distance)
        {
            var results = frame.Buffer(distance);
            return results.Cast<Polyline>().ToList();
        }
        public static List<Point3d> GetPolyPoints(Polyline poly)
        {
            var points = Enumerable.Range(0, poly.NumberOfVertices).Select(i => poly.GetPoint3dAt(i)).ToList();
            if (points.First() == points.Last())
                points.RemoveAt(points.Count - 1);
            if (poly.ToNTSPolygon().Shell.IsCCW)
                points.Reverse();
            return points;
        }
        public static bool PointOnSegment(Point3d p, Point3d s, Point3d e, double eps = 1e-5)
        {
            return Math.Abs((s - p).GetNormal().DotProduct((e - p).GetNormal()) + 1) < eps;
        }
        public static int GetPointIndex(Point3d p, List<Point3d> points)
        {
            for (int i = 0; i < points.Count; ++i)
                if (points[i].DistanceTo(p) < 1)
                    return i;
            return -1;
        }
        public static int GetSegIndex(Point3d p, List<Point3d> points)
        {
            for (int j = 0; j < points.Count; j++)
                if (PointOnSegment(p,points[j],points[(j+1)%points.Count]))
                    return j;
            return -1;
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
        public static Polyline BuildRectangle(double xmin, double xmax, double ymin, double ymax)
        {
            var rect = new Polyline();
            rect.Closed = true;
            rect.AddVertexAt(0, new Point2d(xmin, ymax), 0, 0, 0);
            rect.AddVertexAt(1, new Point2d(xmin, ymin), 0, 0, 0);
            rect.AddVertexAt(2, new Point2d(xmax, ymin), 0, 0, 0);
            rect.AddVertexAt(3, new Point2d(xmax, ymax), 0, 0, 0);
            rect.AddVertexAt(4, new Point2d(xmin, ymax), 0, 0, 0);
            return rect;
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
    }
}
