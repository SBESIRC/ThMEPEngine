using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil.PassageWay
{
    class NomalPipeIntersector
    {
        public static Polyline IntersectWithBuffer(Polyline line, Polyline polygon, ref List<double> polyline_buffer, double polygon_buffer, bool turn_left = true)
        {
            // get inter points
            var inter = line.ToNTSLineString().Intersection(polygon.ToNTSLineString()).Coordinates.ToHashSet();
            if (inter.Count == 0)
                return line;
            HashSet<Point3d> inter_set = new HashSet<Point3d>(inter.Count());
            foreach (var coordinate in inter)
                inter_set.Add(coordinate.ToAcGePoint3d());
            // update line points
            var line_set = inter_set.Except(line.GetPoints());
            var line_points = line.GetPoints().ToList();
            foreach (var point in line_set)
                InsertPoint(point, ref line_points, ref polyline_buffer);
            // calculate inter points' index by line
            List<KeyValuePair<int, Point3d>> index_list = new List<KeyValuePair<int, Point3d>>();
            foreach (var point in inter_set)
                index_list.Add(new KeyValuePair<int, Point3d>(PassageWayUtils.GetPointIndex(point, line_points), point));
            // sort inter points
            index_list = index_list.OrderBy(o => o.Key).ToList();
            // update polygon points
            var polygon_set = inter_set.Except(polygon.GetPoints());
            var polygon_points = polygon.GetPoints().ToList();
            if (polygon.ToNTSPolygon().Shell.IsCCW)
                polygon_points.Reverse();
            foreach (var point in polygon_set)
                InsertPoint(point, ref polygon_points);
            if (polygon_points.Last() == polygon_points.First())
                polygon_points.RemoveAt(polygon_points.Count - 1);
            // calculate inter points' index by polygon
            int start_index = PassageWayUtils.GetPointIndex(index_list.First().Value, polygon_points);
            int end_index = PassageWayUtils.GetPointIndex(index_list.Last().Value, polygon_points);
            // add first seg
            List<Point3d> target = new List<Point3d>();
            for (int i = 0; i < index_list[0].Key; ++i) target.Add(line_points[i]);
            // choose mid seg
            int next = turn_left ? (start_index + 1) % polygon_points.Count : (start_index - 1 + polygon_points.Count) % polygon_points.Count;
            var predir = GetDirBetweenTwoPoint(target.Last(), polygon_points[start_index]);
            var curdir = GetDirBetweenTwoPoint(polygon_points[start_index], polygon_points[next]);
            bool choose_polygon = turn_left ? (predir + 1) % 4 == curdir : (predir + 3) % 4 == curdir;
            // add mid seg
            if (choose_polygon)
            {
                var insert_index = index_list.First().Key;
                polyline_buffer.RemoveRange(index_list.First().Key, index_list.Last().Key - index_list.First().Key);
                for (int i = start_index; i != end_index; i = turn_left ? (i + 1) % polygon_points.Count : (i - 1 + polygon_points.Count) % polygon_points.Count)
                {
                    target.Add(polygon_points[i]);
                    polyline_buffer.Insert(insert_index++, polygon_buffer);
                }
            }
            else
                for (int i = index_list[0].Key; i < index_list.Last().Key; ++i)
                    target.Add(line_points[i]);
            // add last seg
            for (int i = index_list.Last().Key; i < line_points.Count; ++i)
                target.Add(line_points[i]);
            return PassageWayUtils.BuildPolyline(target);
        }
        public static bool PointOnSegment(Point3d p, Point3d s, Point3d e, double eps = 1e-5)
        {
            return Math.Abs((s - p).GetNormal().DotProduct((e - p).GetNormal()) + 1) < eps;
        }
        public static void InsertPoint(Point3d p, ref List<Point3d> points, ref List<double> buffer, double eps = 1e-5)
        {
            for (int j = 0; j < points.Count - 1; j++)
                if (PointOnSegment(p, points[j], points[j + 1])) 
                {
                    points.Insert(j + 1, p);
                    buffer.Insert(j + 1, buffer[j]);
                    return;
                }
        }
        public static void InsertPoint(Point3d p, ref List<Point3d> points, double eps = 1e-5)
        {
            for (int j = 0; j < points.Count - 1; j++)
                if (PointOnSegment(p, points[j], points[j + 1]))
                {
                    points.Insert(j + 1, p);
                    return;
                }
        }
        public static int GetDirBetweenTwoPoint(Point3d a, Point3d b)
        {
            var dx = Math.Sign(b.X - a.X);
            var dy = Math.Sign(b.Y - a.Y);
            if (dx == 1)
                return 0;
            if (dx == -1)
                return 2;
            if (dy == 1)
                return 1;
            return 3;
        }
        public static Polyline SmoothPolyline(Polyline poly,ref List<double> buffer,double eps=30)
        {
            var points = poly.GetPoints().ToList();
            List<int> remove_index = new List<int>();
            for(int i = 0; i < points.Count - 1; ++i)
            {
                var dis = points[i + 1].DistanceTo(points[i]);
                if (dis < eps)
                {
                    var dir = GetDirBetweenTwoPoint(points[i], points[i + 1]);
                    if (i + 2 < points.Count)
                    {
                        if (dir % 2 == 0)
                            points[i + 2] = new Point3d(points[i].X, points[i + 2].Y, 0);
                        else
                            points[i + 2] = new Point3d(points[i].Y, points[i + 2].X, 0);
                    }
                    remove_index.Add(i + 1);
                }
            }
            for (int i = remove_index.Count - 1; i >= 0; i--)
            {
                points.RemoveAt(remove_index[i]);
                buffer.RemoveAt(remove_index[i] - 1);
            }
            for(int j = points.Count - 2; j > 0; j--)
            {
                if(PointOnSegment(points[j],points[j-1],points[j+1]))
                {
                    points.RemoveAt(j);
                    buffer.RemoveAt(j);
                }    
            }
            return PassageWayUtils.BuildPolyline(points);
        }
        public static int GetPointIndex(Point3d p, List<Point3d> points)
        {
            for (int i = 1; i < points.Count; ++i)
                if (points[i].DistanceTo(p) < 1)
                    return i;
            return -1;
        }
    }
}
