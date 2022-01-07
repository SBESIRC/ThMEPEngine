using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.AreaLayout.GridLayout.Method
{
    public static class VisiblePolygon
    {
        private const double Epsilon = 0.0000001;
        private class pointmap
        {
            public pointmap(int seg, int point, double a)
            {
                segment_index = seg;
                point_index = point;
                angle = a;
            }
            public int segment_index { get; }
            public int point_index { get; }
            public double angle { get; }
        }
        //计算可视多边形
        public static Polygon Compute(Coordinate position, Polygon polygon)
        {
            List<Coordinate> vis_vertexs = new List<Coordinate>();
            var bounds = ConvertToSegments(polygon);
            var sorted = SortPoints(position, bounds);
            var start = new Coordinate(position.X + 1, position.Y);
            List<int> heap = new List<int>();
            List<int> map = Enumerable.Repeat(-1, bounds.Count).ToList();
            for (int i = 0; i < bounds.Count; i++)
            {
                var a1 = angle(bounds[i].P0, position);
                var a2 = angle(bounds[i].P1, position);
                bool active = false;
                if (a1 > -180 && a1 <= 0 && a2 <= 180 && a2 >= 0 && a2 - a1 > 180) active = true;
                if (a2 > -180 && a2 <= 0 && a1 <= 180 && a1 >= 0 && a1 - a2 > 180) active = true;

                if (active)
                    insert(i, heap, position, bounds, start, map);
            }
            for (int i = 0; i < sorted.Length;)
            {
                var extend = false;
                var shorten = false;
                var orig = i;
                Coordinate vertex = (sorted[i].point_index == 0) ? bounds[sorted[i].segment_index].P0 : bounds[sorted[i].segment_index].P1;
                var old_segment = heap[0];
                do
                {
                    if (map[sorted[i].segment_index] != -1)
                    {
                        if (sorted[i].segment_index == old_segment)
                        {
                            extend = true;
                            vertex = (sorted[i].point_index == 0) ? bounds[sorted[i].segment_index].P0 : bounds[sorted[i].segment_index].P1;
                        }
                        remove(map[sorted[i].segment_index], heap, position, bounds, vertex, map);
                    }
                    else
                    {
                        insert(sorted[i].segment_index, heap, position, bounds, vertex, map);
                        if (heap[0] != old_segment)
                            shorten = true;
                    }
                    i++;
                    if (i >= sorted.Length) break;
                } while (sorted[i].angle < sorted[orig].angle + Epsilon);
                if (extend)
                {
                    vis_vertexs.Add(vertex);
                    var cur = intersectLines(bounds[heap[0]].P0, bounds[heap[0]].P1, position, vertex);
                    if (cur != null && !equal(cur, vertex)) vis_vertexs.Add(cur);
                }
                else if (shorten)
                {
                    var add1 = intersectLines(bounds[old_segment].P0, bounds[old_segment].P1, position, vertex);
                    if (add1 != null) vis_vertexs.Add(add1);
                    var add2 = intersectLines(bounds[heap[0]].P0, bounds[heap[0]].P1, position, vertex);
                    if (add2 != null) vis_vertexs.Add(add2);
                }
            }
            vis_vertexs.Add(vis_vertexs[0]);
            //var pline = new Polyline()
            //{
            //    Closed = lineString.IsClosed,
            //};
            //pline.CreatePolyline(lineString.Coordinates.ToAcGePoint3ds());
            return new Polygon(new LinearRing(vis_vertexs.ToArray()));
        }
        //计算半径有限的可见多边形
        public static Polygon ComputeWithRadius(Coordinate position, Polygon polygon, double radius, int numPoints = 20)
        {
            //探测圆
            var circle = new Circle(new Point3d(position.X, position.Y, 0), Vector3d.ZAxis, radius);
            var circle_polygon = circle.ToNTSPolygon(numPoints);
            circle.Dispose();
            //房间点数少，先求可见多边形，再与圆求交
            if (polygon.NumPoints < 30)
            {
                //可视多边形
                var vis = Compute(position, polygon);
                //求交得探测器实际的探测范围
                return vis.Intersection(circle_polygon) as Polygon;
            }
            //房间点数多，先与圆求交，再求可见多边形
            else
            {
                var temp_detect = circle_polygon.Intersection(polygon);
                //若交集连通
                if (temp_detect is Polygon poly)
                {
                    //面积等于圆，说明探测区域就是圆
                    if (poly.Area == circle_polygon.Area)
                        return circle_polygon;
                    //面积不等于圆，继续求可见多边形
                    else
                        return Compute(position, poly);
                }
                else
                {
                    //若交集不连通
                    if (temp_detect is MultiPolygon multiPolygon)
                    {
                        foreach (Polygon polygon1 in multiPolygon)
                        {
                            //寻找包含该点的区域
                            var locator = new IndexedPointInAreaLocator(polygon1);
                            if (locator.Locate(position) == Location.Interior)
                                //求可见多边形
                                return Compute(position, polygon1);
                        }
                    }
                }
            }
            return null;
        }
        //计算outer内可以看见整个inner的区域
        public static NetTopologySuite.Geometries.Geometry ComputePolygonWithRadius(Polygon inner, Polygon outer, double radius)
        {
            var poly = outer.Buffer(0.001) as Polygon;
            var objs = new List<Polygon>();
            foreach (Coordinate coordinate in inner.Shell.Coordinates)
                objs.Add(ComputeWithRadius(coordinate, poly, radius));
            //求交集
            NetTopologySuite.Geometries.Geometry geometry = objs[0];
            for (int i = 1; i < objs.Count; i++)
                if (geometry.Intersects(objs[i]))
                    geometry = geometry.Intersection(objs[i]);
                else return null;

            return geometry;
        }
        //将polygons转成线段集合
        private static List<LineSegment> ConvertToSegments(Polygon polygon)
        {
            List<LineSegment> segments = new List<LineSegment>();
            //找到边界上一条长度大于1的边的终点
            int origin = 0, n = polygon.Shell.NumPoints - 1, i, next;
            bool flag = false;
            for (; origin != 1; origin = (origin - 1 + n) % n)
                if (polygon.Shell.Coordinates[(origin - 1 + n) % n].Distance(polygon.Shell.Coordinates[origin]) > 1)
                    break;
            //添加边界，过滤非常接近的点
            for (i = origin, flag = false; !(flag && i == origin); i = next)
            {
                flag = true;
                Coordinate p1 = new Coordinate(polygon.Shell.Coordinates[i].X, polygon.Shell.Coordinates[i].Y);
                for (next = (i + 1) % n; next != i && polygon.Shell.Coordinates[next].Distance(p1) <= 1; next = (next + 1) % n) ;
                Coordinate p2 = new Coordinate(polygon.Shell.Coordinates[next].X, polygon.Shell.Coordinates[next].Y);
                segments.Add(new LineSegment(p1, p2));
            }

            //添加内部的洞，过滤非常接近的点
            foreach (var hole in polygon.Holes)
            {
                n = hole.NumPoints - 1;
                for (origin = 0; origin != 1; origin = (origin - 1 + n) % n)
                    if (hole.Coordinates[(origin - 1 + n) % n].Distance(hole.Coordinates[origin]) > 1)
                        break;
                for (i = origin, flag = false; !(flag && i == origin); i = next)
                {
                    flag = true;
                    Coordinate p1 = new Coordinate(hole.Coordinates[i].X, hole.Coordinates[i].Y);
                    for (next = (i + 1) % n; next != i && hole.Coordinates[next].Distance(p1) <= 1; next = (next + 1) % n) ;
                    Coordinate p2 = new Coordinate(hole.Coordinates[next].X, hole.Coordinates[next].Y);
                    segments.Add(new LineSegment(p1, p2));
                }
            }
            return segments;
        }
        //将segments中的点按照关于position的角度沿顺时针排序
        private static pointmap[] SortPoints(Coordinate position, List<LineSegment> segments)
        {
            pointmap[] points = new pointmap[segments.Count * 2];
            for (int i = 0; i < segments.Count; i++)
            {
                double a = angle(segments[i].P0, position);
                points[2 * i] = new pointmap(i, 0, a);
                a = angle(segments[i].P1, position);
                points[2 * i + 1] = new pointmap(i, 1, a);
            }
            Array.Sort(points, (a, b) => a.angle.CompareTo(b.angle));
            return points;
        }
        //向量的角度制形式
        private static double angle(Coordinate a, Coordinate b)
        {
            var ans = Math.Atan2(b.Y - a.Y, b.X - a.X) * 180.0 / Math.PI;
            if (ans == -180) ans = 180;
            return ans;
        }
        //角abc的度数
        private static double angle2(Coordinate a, Coordinate b, Coordinate c)
        {
            var a1 = angle(a, b);
            var a2 = angle(b, c);
            var a3 = a1 - a2;
            if (a3 < 0.0) a3 += 360.0;
            if (a3 > 360.0) a3 -= 360.0;
            return a3;
        }
        private static bool equal(Coordinate a, Coordinate b)
        {
            if (a == null || b == null)
                return false;
            return Math.Abs(a.X - b.X) < Epsilon && Math.Abs(a.Y - b.Y) < Epsilon;
        }
        //寻找两条线段的交点
        private static Coordinate intersectLines(Coordinate a1, Coordinate a2, Coordinate b1, Coordinate b2)
        {
            var dbx = b2.X - b1.X;
            var dby = b2.Y - b1.Y;
            var dax = a2.X - a1.X;
            var day = a2.Y - a1.Y;
            var u_b = dby * dax - dbx * day;
            if (u_b != 0.0)
            {
                var ua = (dbx * (a1.Y - b1.Y) - dby * (a1.X - b1.X)) / u_b;
                return new Coordinate(a1.X + ua * dax, a1.Y + ua * day);
            }
            return null;
        }
        private static void insert(int index, List<int> heap, Coordinate position, List<LineSegment> segments, Coordinate destination, List<int> map)
        {
            var intersect = intersectLines(segments[index].P0, segments[index].P1, position, destination);
            if (intersect == null)
                return;
            var cur = heap.Count;
            heap.Add(index);
            map[index] = cur;
            while (cur > 0)
            {
                var parent = (cur - 1) / 2;
                if (!lessThan(heap[cur], heap[parent], position, segments, destination)) break;
                map[heap[parent]] = cur;
                map[heap[cur]] = parent;
                var temp = heap[cur];
                heap[cur] = heap[parent];
                heap[parent] = temp;
                cur = parent;
            }
        }
        private static bool lessThan(int index1, int index2, Coordinate position, List<LineSegment> segments, Coordinate destination)
        {
            var inter1 = intersectLines(segments[index1].P0, segments[index1].P1, position, destination);
            var inter2 = intersectLines(segments[index2].P0, segments[index2].P1, position, destination);
            if (inter1 == null || inter2 == null)
                return false;
            if (!equal(inter1, inter2))
            {
                var d1 = inter1.Distance(position);
                var d2 = inter2.Distance(position);
                return d1 < d2;
            }
            var pos1 = equal(inter1, segments[index1].P0) ? segments[index1].P1 : segments[index1].P0;
            var pos2 = equal(inter2, segments[index2].P0) ? segments[index2].P1 : segments[index2].P0;
            var a1 = angle2(pos1, inter1, position);
            var a2 = angle2(pos2, inter2, position);
            if (a1 < 180.0)
            {
                if (a2 > 180.0) return true;
                return a2 < a1;
            }
            return a1 < a2;
        }
        private static void remove(int index, List<int> heap, Coordinate position, List<LineSegment> segments, Coordinate destination, List<int> map)
        {
            map[heap[index]] = -1;
            if (index == heap.Count - 1)
            {
                heap.RemoveAt(index);
                return;
            }
            heap[index] = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
            map[heap[index]] = index;
            var cur = index;
            var parent = (cur - 1) / 2;
            if (cur != 0 && lessThan(heap[cur], heap[parent], position, segments, destination))
            {
                while (cur > 0)
                {
                    parent = (cur - 1) / 2;
                    if (!lessThan(heap[cur], heap[parent], position, segments, destination))
                        break;
                    map[heap[parent]] = cur;
                    map[heap[cur]] = parent;
                    var temp = heap[cur];
                    heap[cur] = heap[parent];
                    heap[parent] = temp;
                    cur = parent;
                }
            }
            else
            {
                while (true)
                {
                    var left = 2 * cur + 1;
                    var right = left + 1;
                    if (left < heap.Count && lessThan(heap[left], heap[cur], position, segments, destination) &&
                        (right == heap.Count || lessThan(heap[left], heap[right], position, segments, destination)))
                    {
                        map[heap[left]] = cur;
                        map[heap[cur]] = left;
                        var temp = heap[left];
                        heap[left] = heap[cur];
                        heap[cur] = temp;
                        cur = left;
                    }
                    else if (right < heap.Count && lessThan(heap[right], heap[cur], position, segments, destination))
                    {
                        map[heap[right]] = cur;
                        map[heap[cur]] = right;
                        var temp = heap[right];
                        heap[right] = heap[cur];
                        heap[cur] = temp;
                        cur = right;
                    }
                    else break;
                }
            }
        }
    }
}
