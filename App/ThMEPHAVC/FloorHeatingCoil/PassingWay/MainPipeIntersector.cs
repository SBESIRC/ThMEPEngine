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
    class MainPipeIntersector
    {
        public static Polyline GetMainRegion(Polyline left, Polyline right, Polyline polygon)
        {
            var inter = left.ToNTSLineString().Intersection(polygon.ToNTSPolygon()).ToDbCollection().Cast<Polyline>();
            if (inter.Count() == 0)
                return GetMainRegion(right, polygon, true);
            var left_points = inter.First().GetPoints().ToList();
            inter = right.ToNTSLineString().Intersection(polygon.ToNTSPolygon()).ToDbCollection().Cast<Polyline>();
            if (inter.Count() == 0)
                return GetMainRegion(left, polygon, false);
            var right_points = inter.First().GetPoints().ToList();
            var points = polygon.GetPoints().ToList();
            if (polygon.ToNTSPolygon().Shell.IsCCW)
                points.Reverse();
            if (points.First() != points.Last())
                points.Add(points[0]);
            var le = GetSegIndex(left_points.Last(), points);
            var re = GetSegIndex(right_points.Last(), points);
            points.RemoveAt(points.Count - 1);
            List<Point3d> target = new List<Point3d>();
            // add left seg
            for (int i = 0; i < left_points.Count; ++i)
                target.Add(left_points[i]);
            // add le-re seg
            if (le != re)
                for (int i = le; i != re; i = (i + 1) % points.Count)
                    target.Add(points[(i + 1) % points.Count]);
            // add right seg
            for (int i = right_points.Count - 1; i >= 0; --i)
                target.Add(right_points[i]);
            // add rs-ls seg
            target.Add(target[0]);
            return PassageWayUtils.BuildPolyline(target);
        }
        public static Polyline GetMainRegion(Polyline line, Polyline polygon, bool line_is_right)
        {
            var inter = line.ToNTSLineString().Intersection(polygon.ToNTSPolygon()).ToDbCollection().Cast<Polyline>();
            if (inter.Count() == 0)
                return polygon;
            var line_points = inter.First().GetPoints().ToList();
            var points = polygon.GetPoints().ToList();
            if (polygon.ToNTSPolygon().Shell.IsCCW)
                points.Reverse();
            if (points.Last() != points.First())
                points.Add(points[0]);
            var s = GetSegIndex(line_points.First(), points);
            var e = GetSegIndex(line_points.Last(), points);
            points.RemoveAt(points.Count - 1);
            List<Point3d> target = new List<Point3d>();
            if (line_is_right)
            {
                if (s != e)
                    for (int i = s; i != e; i = (i + 1) % points.Count)
                        target.Add(points[(i + 1) % points.Count]);
                else
                {
                    for (int i = (s + 1) % points.Count; i != e; i = (i + 1) % points.Count)
                        target.Add(points[i]);
                    target.Add(points[e]);
                }
                for (int i = line_points.Count - 1; i >= 0; --i)
                    target.Add(line_points[i]);
            }
            else
            {
                for (int i = 0; i < line_points.Count; ++i)
                    target.Add(line_points[i]);
                for (int i = e; i != s; i = (i + 1) % points.Count)
                    target.Add(points[(i + 1) % points.Count]);
            }
            target.Add(target[0]);
            return PassageWayUtils.BuildPolyline(target);
        }
        public static int GetSegIndex(Point3d p, List<Point3d> points)
        {
            for (int j = 0; j < points.Count - 1; j++)
                if (Math.Abs((points[j] - p).GetNormal().DotProduct(
                             (points[j + 1] - p).GetNormal()) + 1) < 1e-5)
                    return j;
            return -1;
        }
        public static Point3d GetClosedPointAtoB(Polyline a, Polyline b)
        {
            Point3d ret = a.GetPoint3dAt(1);
            var dis = b.Distance(ret);
            // A is open while B is closed
            for (int i = 2; i < a.NumberOfVertices - 1; ++i)
            {
                var cur_dis = b.Distance(a.GetPoint3dAt(i));
                if (cur_dis < dis)
                {
                    dis = cur_dis;
                    ret = a.GetPoint3dAt(i);
                }
            }
            return ret;
        }
        public static Polyline BufferPolyline(Polyline poly, double buffer)
        {
            var list_buffer = new List<Polyline>();
            for (int i = 0; i < poly.NumberOfVertices-1; ++i)
            {
                var p0 = poly.GetPoint3dAt(i);
                var p1 = poly.GetPoint3dAt(i + 1);
                if (i > 0)
                    p0 += (p0 - p1).GetNormal() * buffer;
                if (i < poly.NumberOfVertices - 2) 
                    p1 += (p1 - p0).GetNormal() * buffer;
                if (p1 == p0) continue;
                var line = new Line(p0, p1);
                list_buffer.Add(line.Buffer(buffer));
            }
            return list_buffer.ToArray().ToCollection().UnionPolygons().Cast<Polyline>().First();
        }
        public static List<Polyline> DealWithRegion(BufferPoly shortest_way, Polyline region,Polyline room, double max_dw, ref List<Polyline> skeleton)
        {
            var point = GetClosedPointAtoB(shortest_way.poly, region);
            var point_on_polygon = region.GetClosePoint(point);
            var point_on_line = shortest_way.poly.GetClosePoint(point_on_polygon);
            var dir = NomalPipeIntersector.GetDirBetweenTwoPoint(point_on_line, point_on_polygon);
            var points = shortest_way.poly.GetPoints().ToList();
            var point_index = NomalPipeIntersector.GetPointIndex(point_on_line, points);
            if (point_index != -1)
            {
                if (NomalPipeIntersector.GetDirBetweenTwoPoint(points[point_index - 1], point_on_line) % 2 != dir % 2)
                    max_dw = shortest_way.buff[point_index];
                else
                    max_dw = shortest_way.buff[point_index - 1];
            }
            else
                max_dw = shortest_way.buff[GetSegIndex(point_on_line, points)];
            var inter = room.ToNTSPolygon().Buffer(-2 * max_dw).Intersection(region.ToNTSPolygon()).ToDbCollection().Cast<Polyline>();
            if (inter.Count() == 0) return new List<Polyline>();
            region = inter.First();
            
            MainPipeGenerator mainPipeGenerator = new MainPipeGenerator(region, point_on_line, max_dw);
            mainPipeGenerator.CalculatePipeSkeleton();
            var buff_poly = mainPipeGenerator.skeleton;
            // buffer every line
            List<Polyline> pipes = new List<Polyline>();
            if (buff_poly.Count > 0)
            {
                Vector3d dp = new Vector3d(0, 0, 0);
                if (dir % 2 == 0)
                    dp = new Vector3d(point_on_line.Y - buff_poly[0].GetPoint3dAt(1).Y, 0, 0);
                else
                    dp = new Vector3d(point_on_line.X - buff_poly[0].GetPoint3dAt(1).X, 0, 0);
                buff_poly[0].RemoveVertexAt(0);
                buff_poly[0].TransformBy(Matrix3d.Displacement(dp));
                buff_poly[0].AddVertexAt(0, point_on_line.ToPoint2D(), 0, 0, 0);
                for (int j = 0; j < buff_poly.Count; ++j)
                {
                    if (j > 0)
                        buff_poly[j].TransformBy(Matrix3d.Displacement(dp));
                    pipes.Add(BufferPolyline(buff_poly[j], max_dw));
                }
            }
            return pipes;
            //skeleton.Add(poly);
        }
    

        public static void ExtandPolyline(ref List<Point3d> points,double buffer)
        {
            if (points.Count <= 1) return;
            var p0 = points[0];
            var p1 = points[1];
            points[0] = p0 + (p0 - p1).GetNormal() * buffer;
            p0 = points.Last();
            p1 = points[points.Count - 2];
            points[points.Count - 1] = p0 + (p0 - p1).GetNormal() * buffer;
        }
    }

    class MainPipeGenerator:RoomPipeGenerator
    {
        public MainPipeGenerator(Polyline room, Point3d pipe_in, double buffer = -200)
        {
            this.room = room;
            this.pipe_in = room.GetClosePoint(pipe_in);
            this.buffer = buffer * (-4);
            this.pipe_width = buffer;
        }
        public void CalculatePipeSkeleton()
        {
            buffer_tree = GetBufferTree(room);
            GetSkeleton(buffer_tree);
            AddInputSegment();
        }
        protected void AddInputSegment()
        {
            if (skeleton.Count == 0) return;
            var points = PassageWayUtils.GetPolyPoints(room);
            var seg = PassageWayUtils.GetSegIndex(pipe_in, points);
            var p0 = points[seg];
            var p1 = points[(seg + 1) % points.Count];
            var line = new Polyline();
            line.AddVertexAt(0, p0.ToPoint2D(), 0, 0, 0);
            line.AddVertexAt(1, p1.ToPoint2D(), 0, 0, 0);
            var point = line.GetClosePoint(skeleton[0].StartPoint);
            skeleton[0].SetPointAt(0, point.ToPoint2D());
        }
        BufferTreeNode GetBufferTree(Polyline poly, bool flag = true)
        {
            BufferTreeNode node = new BufferTreeNode(poly);
            var next_buffer = PassageWayUtils.Buffer(poly, flag ? buffer / 4 : buffer);
            if (next_buffer.Count == 0) return node;
            node.childs = new List<BufferTreeNode>();
            foreach (Polyline child_poly in next_buffer)
            {
                var child = GetBufferTree(child_poly, false);
                child.parent = node;
                node.childs.Add(child);
            }
            return node;
        }
    }
}
