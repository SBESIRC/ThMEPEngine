using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class MainPipeCalculator
    {
        //外部输入数据
        Polyline region;
        List<BufferPoly> shortest_way { get; set; }
        int main_index { get; set; }
        double buffer { get; set; } = 500;
        double room_buffer { get; set; } = 200;
        bool main_has_output { get; set; } = true;
        //整理后数据
        Point3d pin { get; set; } = Point3d.Origin;
        Point3d pout { get; set; } = Point3d.Origin;
        double in_buffer { get; set; } = -1;
        double out_buffer { get; set; } = -1;
        Polyline main_shortest_way { get; set; } = null;
        // 临时需要的数据
        BufferTreeNode buffer_tree = null;
        bool if_find = true;
        bool is_CCW = true;
        // 输出数据
        List<Polyline> skeleton { get; set; } = null;

        public MainPipeCalculator(Polyline region, List<BufferPoly> shortest_way, int main_index, double buffer, double room_buffer, bool main_has_output)
        {
            this.region = region;
            this.shortest_way = shortest_way;
            this.main_index = main_index;
            this.buffer = buffer;
            this.room_buffer = room_buffer;
            this.main_has_output = main_has_output;

            pin = shortest_way[main_index].poly[0];
            in_buffer = shortest_way[main_index].buff[0];
            if (main_has_output)
            {
                pout = shortest_way[main_index].poly.Last();
                out_buffer = shortest_way[main_index].buff.Last();
            }
            main_shortest_way = PassageWayUtils.BuildPolyline(shortest_way[main_index].poly);
            skeleton = new List<Polyline>();
        }
        public void Calculate()
        {
            var main_region = CalculateMainRegion();
            if (!if_find) return;
            buffer_tree = GetBufferTree(main_region);
            GetSkeleton(buffer_tree);
        }
        Polyline CalculateMainRegion()
        {
            var shortest_way_polylines = new List<Polyline>();
            for (int i = 0; i < shortest_way.Count; i++)
            {
                shortest_way_polylines.Add(PassageWayUtils.BuildPolyline(shortest_way[i].poly));
            }

            // init region
            Polyline main_region = new Polyline();
            //region = region.Buffer(max_dw * 0.5).Cast<Polyline>().OrderByDescending(o => o.Area).First();
            if (shortest_way_polylines.Count > 1)
            {
                main_region.Dispose();
                if (main_index == 0)
                    main_region = MainRegionCalculator.GetMainRegion(shortest_way_polylines[main_index + 1], region, true);
                else if (main_index == shortest_way_polylines.Count - 1)
                    main_region = MainRegionCalculator.GetMainRegion(shortest_way_polylines[main_index - 1], region, false);
                else
                    main_region = MainRegionCalculator.GetMainRegion(shortest_way_polylines[main_index - 1], shortest_way_polylines[main_index + 1], region);
            }


            // init remove part
            DBObjectCollection rest = new DBObjectCollection();
            if (shortest_way_polylines.Count > 1)
            {
                if (main_index > 0)
                    rest.Add(shortest_way[main_index - 1].Buffer(4));
                if (main_index < shortest_way_polylines.Count - 1)
                    rest.Add(shortest_way[main_index + 1].Buffer(4));
            }
            foreach (var poly in shortest_way_polylines)
                poly.Dispose();
            shortest_way_polylines.Clear();
            // remove other pipe part
            rest = main_region.Difference(rest);

            // init smaller part
            //var new_room = AdjustBufferRoom();
            var smaller_room = region.Buffer(-room_buffer - 0.25 * buffer);

            Polyline new_region = new Polyline();
            List<Polyline> new_region_list = rest.OfType<Polyline>().ToList();
            if (new_region_list.Count > 0)
            {
                new_region = new_region_list.FindByMax(o => o.Area);
                rest = new_region.Intersection(smaller_room);
                new_region_list = rest.OfType<Polyline>().ToList();
                if (new_region_list.Count > 0)
                    new_region = new_region_list.FindByMax(o => o.Area);
                else
                {
                    skeleton.Add(main_shortest_way);
                    if_find = false;
                }
            }
            else
            {
                skeleton.Add(main_shortest_way);
                if_find = false;
            }
            new_region.Closed = true;
            return new_region;
        }
        Polyline AdjustBufferRoom()
        {
            var points = PassageWayUtils.GetPolyPoints(region);
            // calculate start direction
            var pre = PassageWayUtils.GetSegIndexOnPolygon(shortest_way[main_index].poly[0], points);
            var next = (pre + 1) % points.Count;
            var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
            var start_dir = (dir + 3) % 4;
            // adjust pin's neighbor edge
            if (pin.DistanceTo(points[pre]) <= room_buffer + in_buffer + 1)
            {
                var ppre = (pre - 1 + points.Count) % points.Count;
                var old_pre_point = points[pre];
                dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[ppre]);
                if (dir == start_dir)
                {
                    points[pre] = pin + (old_pre_point - pin).GetNormal() * (buffer / 4 + room_buffer);
                    points[ppre] += (points[pre] - old_pre_point);
                }
            }
            else if (pin.DistanceTo(points[next]) <= room_buffer + in_buffer + 1)
            {
                var nnext = (next + 1) % points.Count;
                var old_next_point = points[next];
                dir = PassageWayUtils.GetDirBetweenTwoPoint(points[next], points[nnext]);
                if (dir == start_dir)
                {
                    points[next] = pin + (old_next_point - pin).GetNormal() * (buffer / 4 + room_buffer);
                    points[nnext] += (points[next] - old_next_point);
                }
            }
            // adjust pout's neighbor edge
            if (main_has_output)
            {
                pre = PassageWayUtils.GetSegIndexOnPolygon(pout, points);
                next = (pre + 1) % points.Count;
                dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                var end_dir = (dir + 3) % 4;
                if (pout.DistanceTo(points[pre]) <= room_buffer + out_buffer + 1)
                {
                    var ppre = (pre - 1 + points.Count) % points.Count;
                    var old_pre_point = points[pre];
                    dir = PassageWayUtils.GetDirBetweenTwoPoint(points[ppre], points[pre]);
                    if (dir == end_dir)
                    {
                        points[pre] = pout + (old_pre_point - pout).GetNormal() * (buffer / 4 + room_buffer);
                        points[ppre] += (points[pre] - old_pre_point);
                    }
                }
                else if (pout.DistanceTo(points[next]) <= room_buffer + out_buffer + 1)
                {
                    var nnext = (next + 1) % points.Count;
                    var old_next_point = points[next];
                    dir = PassageWayUtils.GetDirBetweenTwoPoint(points[nnext], points[next]);
                    if (dir == end_dir)
                    {
                        points[next] = pout + (old_next_point - pout).GetNormal() * (buffer / 4 + room_buffer);
                        points[nnext] += (points[next] - old_next_point);
                    }
                }
            }
            // build buffer region
            points.Add(points.First());
            return PassageWayUtils.BuildPolyline(points);
        }
        BufferTreeNode GetBufferTree(Polyline poly, bool flag = false)
        {
            BufferTreeNode node = new BufferTreeNode(poly);
            var next_buffer = PassageWayUtils.Buffer(poly, -buffer);
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
        void GetSkeleton(BufferTreeNode node)
        {
            DealWithShell(node);
            if (node.childs == null) return;
            foreach (var child in node.childs)
                GetSkeleton(child);
        }
        void DealWithShell(BufferTreeNode node)
        {
            PassageShowUtils.ShowEntity(node.shell, 0);
        }
        //public Point3d GetInputPoint()
        //{
        //    Polyline polyStart = new Polyline();
        //    Polyline polyEnd = new Polyline();
        //    Point3d start = new Point3d();
        //    Point3d end = new Point3d();

        //    List<Point3d> intersectionPointList = IntersectUtils.PolylineIntersectionPolyline(main_shortest_way, buffer_tree.shell);
        //    if (intersectionPointList.Count == 0) return Point3d.Origin;

        //    Dictionary<Point3d, double> pointDis = new Dictionary<Point3d, double>();
        //    for (int i = 0; i < intersectionPointList.Count; i++)
        //    {
        //        double ptDis = GetDis(main_shortest_way, intersectionPointList[i]);
        //        pointDis.Add(intersectionPointList[i], ptDis);
        //    }

        //    intersectionPointList = intersectionPointList.OrderBy(x => pointDis[x]).ToList();
        //    start = intersectionPointList.First();
        //    end = intersectionPointList.Last();

        //    List<Point3d> coords = PassageWayUtils.GetPolyPoints(main_shortest_way);
        //    int indexStart = PassageWayUtils.GetSegIndex2(start, coords);
        //    for (int i = 0; i <= indexStart; i++)
        //    {
        //        polyStart.AddVertexAt(polyStart.NumberOfVertices, coords[i].ToPoint2D(), 0, 0, 0);
        //    }
        //    polyStart.AddVertexAt(polyStart.NumberOfVertices, start.ToPoint2D(), 0, 0, 0);

        //    int indexEnd = PassageWayUtils.GetSegIndex2(end, coords);
        //    polyEnd.AddVertexAt(polyEnd.NumberOfVertices, end.ToPoint2D(), 0, 0, 0);
        //    for (int i = indexEnd + 1; i <= coords.Count - 1; i++)
        //    {
        //        polyEnd.AddVertexAt(polyEnd.NumberOfVertices, coords[i].ToPoint2D(), 0, 0, 0);
        //    }

        //    if (polyStart.NumberOfVertices > 1) skeleton.Add(polyStart);
        //    if (polyEnd.NumberOfVertices > 1) skeleton.Add(polyEnd);

        //    return start;
        //}
        //public double GetDis(Polyline pl, Point3d pt)
        //{
        //    double dis = 0;
        //    var coords = PassageWayUtils.GetPolyPoints(pl);
        //    int index = PassageWayUtils.GetSegIndex2(pt, coords);
        //    for (int i = 0; i < index; i++)
        //    {
        //        dis += (coords[i + 1] - coords[i]).Length;
        //    }
        //    dis += (pt - coords[index]).Length;
        //    return dis;
        //}
    }
}
