using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class RoomPipeGenerator:IDisposable
    {
        // input
        Polyline room { get; set; }
        PipeInput pipe_input { get; set; }
        double buffer { get; set; } = 500;
        double room_buffer { get; set; } = 100;
        double pipe_width { get; set; }

        // output
        public PipeOutput output;
        // inner structure
        BufferTreeNode buffer_tree = null;
        List<Polyline> skeleton = new List<Polyline>();             // 临时骨架线
        List<Polyline> inner_shape = new List<Polyline>();          // 最内层轮廓
        Polyline input_seg = null;                                  // 入口轮廓
        double adjust_dist = 1000;                                  // 避免轮廓尖角
        double smaller_last_dist = 0.8;                             // 最后一段内缩阈值倍数

        RoomPipeGenerator() { }

        public RoomPipeGenerator(Polyline room, List<DrawPipeData> pipe_in_list, double buffer, double room_buffer = 100)
        {
            this.room = SmoothUtils.SmoothPolygonByRoundXY(room);
            pipe_in_list[0].CenterPoint = room.GetClosestPointTo(pipe_in_list[0].CenterPoint, false);
            this.pipe_input = new PipeInput(pipe_in_list[0]);
            this.buffer = buffer;
            this.pipe_width = buffer / 4;
            this.room_buffer = room_buffer;
            output = new PipeOutput();
            output.pipe_id = 0;
            output.skeleton = new List<Polyline>();
        }
        public void CalculatePipeline()
        {
            AdjustRoom();
            buffer_tree = GetBufferTree(room);
            GetSkeleton(buffer_tree);
            AddInputSegment();
            BufferSkeleton();
        }
        void AdjustRoom()
        {
            if (room.NumberOfVertices <= 5) return;
            var points = PassageWayUtils.GetPolyPoints(room);
            var count = points.Count;
            // 斜线处理
            for (int i = 0; i < points.Count; ++i)
            {
                var next = (i + 1) % points.Count;
                var dp = points[next] - points[i];
                if (Math.Abs(dp.X) > 50 && Math.Abs(dp.Y) > 10 && dp.Length < buffer)
                {
                    Point3d new_point = new Point3d(points[i].X, points[next].Y, 0);
                    if (room.Contains(new_point))
                        points.Insert(next, new_point);
                    else
                        points.Insert(next, new Point3d(points[next].X, points[i].Y, 0));
                }
            }
            room = PassageWayUtils.BuildPolyline(points);

            int inner_idx = -1;
            int outer_idx = -1;
            double max_dis = double.MinValue;
            double minx = double.MaxValue, miny = double.MaxValue;
            double maxx = double.MinValue, maxy = double.MinValue;
            for (int i = 0; i < points.Count; ++i)
            {
                var pre = (i - 1 + count) % count;
                var next = (i + 1) % count;
                Vector3d pre_v = points[next] - points[i];
                Vector3d next_v = points[pre] - points[i];
                if (pre_v.CrossProduct(next_v).Z > 0)
                    inner_idx = i;
                if (pre_v.Length + next_v.Length > max_dis)
                {
                    max_dis = pre_v.Length + next_v.Length;
                    outer_idx = i;
                }
                minx = Math.Min(minx, points[i].X);
                miny = Math.Min(miny, points[i].Y);
                maxx = Math.Max(maxx, points[i].X);
                maxy = Math.Max(maxy, points[i].Y);
            }
            if (inner_idx == -1 || outer_idx == -1) return;
            var boundary_dist = room_buffer + pipe_width;
            var pipe_dist = buffer;
            if (Math.Abs(points[outer_idx].X - points[inner_idx].X) < (maxx - minx) / 2)
            {
                double old_x = points[inner_idx].X;
                double new_x = points[inner_idx].X;
                if (new_x > points[outer_idx].X)
                    new_x = minx + (int)((new_x - minx - 2 * boundary_dist) / pipe_dist) * pipe_dist + 2 * boundary_dist;
                else
                    new_x = maxx - (int)((maxx - new_x - 2 * boundary_dist) / pipe_dist) * pipe_dist - 2 * boundary_dist;
                for (int i = 0; i < count; ++i)
                {
                    if (points[i].X == old_x)
                        points[i] = new Point3d(new_x, points[i].Y, 0);
                }
            }
            if (Math.Abs(points[outer_idx].Y - points[inner_idx].Y) < (maxy - miny) / 2)
            {
                double old_y = points[inner_idx].Y;
                double new_y = points[inner_idx].Y;
                if (new_y > points[outer_idx].Y)
                    new_y = miny + (int)((new_y - miny - 2 * boundary_dist) / pipe_dist) * pipe_dist + 2 * boundary_dist;
                else
                    new_y = maxy - (int)((maxy - new_y - 2 * boundary_dist) / pipe_dist) * pipe_dist - 2 * boundary_dist;
                for (int i = 0; i < count; ++i)
                {
                    if (points[i].X == old_y)
                        points[i] = new Point3d(new_y, points[i].Y, 0);
                }
            }
            points.Add(points[0]);
            room.Dispose();
            room = PassageWayUtils.BuildPolyline(points);
        }
        BufferTreeNode GetBufferTree(Polyline poly, bool flag = true)
        {
            var points = SmoothUtils.SmoothPolygon(PassageWayUtils.GetPolyPoints(poly));
            points.Add(points.First());
            poly = PassageWayUtils.BuildPolyline(points);
            BufferTreeNode node = new BufferTreeNode(poly);
            var next_buffer = PassageWayUtils.Buffer(poly, flag ? -room_buffer - pipe_width : -buffer);
            if (next_buffer.Count == 0) return node;
            node.childs = new List<BufferTreeNode>();
            if (flag == true)
            {
                if (next_buffer.Count > 1)
                {
                    for(int i=next_buffer.Count-1;i>=0;i--)
                    {
                        if (next_buffer[i].Distance(pipe_input.pin) > room_buffer + buffer / 4 + 100)  
                        {
                            next_buffer[i].Dispose();
                            next_buffer.RemoveAt(i);
                        }
                    }
                }
            }
            foreach (Polyline child_poly in next_buffer)
            {
                var child = GetBufferTree(child_poly, false);
                child.parent = node;
                node.childs.Add(child);
            }
            return node;
        }
        void DealWithShell(BufferTreeNode node)
        {
            if (node.parent == null) return;
            var coords = PassageWayUtils.GetPolyPoints(node.shell);
            Point3d pin = node.parent.parent == null ? pipe_input.pin : GetClosedPointAtoB(node.parent.shell, node.shell);
            // 点列重排
            var point = node.shell.GetClosePoint(pin);
            int index = PassageWayUtils.GetPointIndex(point, coords);
            if (index == -1)
            {
                index = PassageWayUtils.GetSegIndexOnPolygon(point, coords);
                var pre = (index + coords.Count - 1) % coords.Count;
                var next = (index + 1) % coords.Count;
                if ((coords[pre] - coords[index]).CrossProduct(coords[next] - coords[index]).Z < 0)
                    index = next;
                PassageWayUtils.RearrangePoints(ref coords, index);
                //if (coords[0].DistanceTo(point) < pipe_width)
                //    coords.Reverse(1, coords.Count - 1);
                //else if (coords[1].DistanceTo(point) < pipe_width)
                //    PassageWayUtils.RearrangePoints(ref coords, 1);
            }
            else
            {
                PassageWayUtils.RearrangePoints(ref coords, index);
                if (pipe_input.in_buffer == pipe_width || node.parent.parent != null) 
                {
                    if (!PassageWayUtils.IsParallel(point - pin, coords[0] - coords[1])) 
                        coords.Reverse(1, coords.Count - 1);
                }
                else
                {
                    if (PassageWayUtils.IsParallel(point - pin, coords[0] - coords[1], 20)) 
                        coords.Reverse(1, coords.Count - 1);
                }
            }
            // 切断最后一条线
            var p0 = coords.First();
            var p1 = coords.Last();
            if (p1.DistanceTo(p0) > buffer*1.5) 
                coords.Add(p0 + (p1 - p0).GetNormal() * buffer);
            // 加入连接线
            if (node.parent.parent != null)
            {
                if (point.DistanceTo(coords[0]) < 1) 
                    coords[0] = pin;
                else
                {
                    coords[0] += (coords[0] - coords[1]).GetNormal() * pipe_width;
                    coords.Insert(0, point);
                    coords.Insert(0, pin);
                }
            }
            coords = SmoothUtils.SmoothPoints(coords);
            // 清洗骨架线的重复点、共线点
            SmoothPolyline(coords);
            if (coords.Count >= 4)
            {
                var first_line = new Line(coords[0], coords[1]);
                var first_dir = (coords[0] - coords[1]).GetNormal();
                var last_dir = (coords.Last() - coords[coords.Count - 2]).GetNormal();
                if (last_dir.DotProduct(first_dir) < 1e-3)
                {
                    var p = first_line.GetClosestPointTo(coords.Last(), last_dir, true);
                    if (p.DistanceTo(coords.Last()) < 100)
                        coords[coords.Count - 1] = p - last_dir * buffer;
                }
            }
            // 平滑当前层骨架线的第一段
            if (node.parent.parent != null && node.parent.shell.EndPoint.DistanceTo(coords[0]) < buffer)
            {
                var dir = (node.parent.shell.EndPoint - coords[0]).GetNormal();
                var dir0 = (coords[0] - coords[1]).GetNormal();
                if (dir.DotProduct(dir0) < 1e-3)
                    node.parent.shell.SetPointAt(node.parent.shell.NumberOfVertices - 1, coords[0].ToPoint2D());
                else
                    coords[0] = node.parent.shell.EndPoint;
            }
            // 处理最内层骨架线
            if (node.childs == null && coords.Count > 2)
            {
                p0 = coords.Last();
                p1 = coords[coords.Count - 2];
                var p2 = coords[coords.Count - 3];
                //PassageShowUtils.ShowText(p0, "p0");
                //PassageShowUtils.ShowText(p1, "p1");
                //PassageShowUtils.ShowText(p2, "p2");
                var dir1 = (p0 - p1).GetNormal();
                var dir2 = (p1 - p2).GetNormal();
                if (Math.Abs(dir1.DotProduct(dir2)) < 1e-3)
                {
                    // 如果倒数第二段较长，最后一段加粗
                    var L = (p1 - p2).Length;
                    if (L < 8 * pipe_width && L > 4 * pipe_width)
                    {
                        var x = L / 2;
                        coords.RemoveAt(coords.Count - 1);
                        p0 += dir1 * pipe_width;
                        var inner_points = new List<Point3d>();
                        inner_points.Add(p0 + dir2 * pipe_width);
                        inner_points.Add(p0 + dir2 * (pipe_width - x));
                        inner_points.Add(p1 + dir2 * (pipe_width - x));
                        inner_points.Add(p1 + dir2 * pipe_width);
                        inner_points.Add(inner_points.First());
                        inner_shape.Add(PassageWayUtils.BuildPolyline(inner_points));
                        //PassageShowUtils.ShowEntity(inner_skeleton.Last(), 1);
                    }
                    // 如果倒数第二段较短，最后一段略微变窄，变窄后的宽度>0.8*推荐宽度
                    else if (L < 4 * pipe_width && L > (1 + 3 * smaller_last_dist) * pipe_width)
                    {
                        var x = (L + 2 * pipe_width) / 3;
                        coords.RemoveAt(coords.Count - 1);
                        coords[coords.Count - 1] = p2 + dir2 * 2 * x;
                        p0 += dir1 * pipe_width;
                        var new_p1 = p2 + dir2 * (1.5 * x + pipe_width);
                        var dp = new_p1 - p1;
                        p0 += dp;
                        var inner_points = new List<Point3d>();
                        inner_points.Add(p0 + 0.5 * x * dir2);
                        inner_points.Add(p0 - 0.5 * x * dir2);
                        inner_points.Add(new_p1 - 0.5 * x * dir2);
                        inner_points.Add(new_p1 + 0.5 * x * dir2);
                        inner_points.Add(inner_points.First());
                        inner_shape.Add(PassageWayUtils.BuildPolyline(inner_points));
                        //PassageShowUtils.ShowEntity(inner_skeleton.Last(), 1);
                    }
                    // 如果变窄后宽度<0.8*推荐宽度，且不变之前存在狭缝
                    else if (L < (1 + 3 * smaller_last_dist) * pipe_width && L > 2 * pipe_width)  
                    {
                        var last2 = coords.Last() + (p2 - p1) / 2;
                        coords.Add(last2);
                        coords.Add(p2 + (p1 - p2) / 2);
                    }
                    // 如果不存在狭缝，则舍弃最后两段
                    else if (L < 2 * pipe_width)
                    {
                        coords.RemoveAt(coords.Count - 1);
                        coords.RemoveAt(coords.Count - 1);
                    }
                }
            }
            //PassageShowUtils.ShowEntity(node.shell);
            node.SetShell(PassageWayUtils.BuildPolyline(coords));
            //PassageShowUtils.ShowEntity(node.shell);
            skeleton.Add(node.shell);
            output.skeleton.Add(node.shell.Clone() as Polyline);
        }
        void GetSkeleton(BufferTreeNode node)
        {
            DealWithShell(node);
            if (node.childs == null) return;
            foreach (var child in node.childs)
                GetSkeleton(child);
        }
        void AddInputSegment()
        {
            // 计算入口方向
            var points = PassageWayUtils.GetPolyPoints(room);
            var point = room.GetClosePoint(pipe_input.pin);
            var pre = PassageWayUtils.GetSegIndexOnPolygon(point, points);
            var next = (pre + 1) % points.Count;
            var dir = (points[next] - points[pre]).GetNormal().RotateBy(-Math.PI / 2, Vector3d.ZAxis);

            var le = pipe_input.pin + dir * (room_buffer + pipe_width + pipe_input.pin.DistanceTo(point));
            var line = new Line(pipe_input.pin, le);
            input_seg = line.Buffer(pipe_input.in_buffer);
        }
        void BufferSkeleton()
        {
            // buffer every line
            List<Polyline> pipes = new List<Polyline>();
            foreach(var poly in skeleton)
            {
                var p0 = poly.GetPoint3dAt(0);
                var p1 = poly.GetPoint3dAt(1);
                poly.AddVertexAt(0, (p0 + (p0 - p1).GetNormal() * pipe_width).ToPoint2D(), 0, 0, 0);
                p0 = poly.GetPoint3dAt(poly.NumberOfVertices - 1);
                p1 = poly.GetPoint3dAt(poly.NumberOfVertices - 2);
                poly.AddVertexAt(poly.NumberOfVertices, (p0 + (p0 - p1).GetNormal() * pipe_width).ToPoint2D(), 0, 0, 0);
                var polylist = ThCADCoreNTSOperation.BufferFlatPL(poly, pipe_width).OfType<Polyline>().ToList();
                pipes.AddRange(polylist);
            }
            pipes.AddRange(inner_shape);
            // clear skeleton
            PassageWayUtils.ClearListPoly(skeleton);
            // deal with bad corner
            var pipe = pipes.ToArray().ToCollection().UnionPolygons().Cast<Polyline>().ToList();
            for (int i = 0; i < pipe.Count; ++i)
            {
                var old_pts = pipe[i].GetPoints().ToList();
                var new_pts = new List<Point3d>();
                var count = old_pts.Count - 1;
                for (int j = 0; j < count; ++j)
                {
                    var cur = old_pts[j];
                    var pre_idx = (j - 1 + count) % count;
                    var next_idx = (j + 1) % count;
                    var pre = old_pts[pre_idx];
                    var next = old_pts[next_idx];
                    bool is_pre_edge = pre.DistanceTo(cur) > pipe_width + 1;
                    bool is_next_edge = next.DistanceTo(cur) > pipe_width + 1;
                    if (is_pre_edge || is_next_edge)
                        new_pts.Add(cur);
                    else
                    {
                        var ppre = old_pts[(j - 2 + count) % count];
                        var nnext = old_pts[(j + 2) % count];
                        pre = pre + (pre - ppre).GetNormal() * adjust_dist;
                        next = next + (next - nnext).GetNormal() * adjust_dist;
                        var pre_line = new Line(ppre, pre);
                        var next_line = new Line(next, nnext);
                        var geometry = pre_line.ToNTSLineString().Intersection(next_line.ToNTSLineString());
                        if (geometry is Point point)
                            new_pts.Add(point.ToAcGePoint3d());
                        pre_line.Dispose();
                        next_line.Dispose();
                    }
                }
                for(int t = new_pts.Count-1; t >0;--t)
                {
                    if (new_pts[t].DistanceTo(new_pts[t - 1]) < 1)
                        new_pts.RemoveAt(t);
                }
                var shell = PassageWayUtils.BuildPolyline(new_pts);
                shell.Closed = true;
                skeleton.Add(shell);
            }
            if (skeleton.Count > 1)
            {
                skeleton.OrderByDescending(o => o.Area);
                for (int i = skeleton.Count - 1; i >= 1; i--)
                {
                    skeleton[i].Dispose();
                    skeleton.RemoveAt(i);
                }
            }
            output.shape = skeleton[0].ToNTSPolygon().Union(input_seg.ToNTSPolygon()).ToDbCollection().Cast<Polyline>().First();
            PassageWayUtils.ClearListPoly(pipes);
            PassageWayUtils.ClearListPoly(pipe);
        }
        protected void PostProcess()
        {
            var pipe = room.ToNTSPolygon().Intersection(skeleton[0].ToNTSPolygon()).ToDbCollection().Cast<Polyline>().First();
            var points = SmoothUtils.SmoothPoints(PassageWayUtils.GetPolyPoints(pipe));
            var se = IntersectUtils.PolylineIntersectionPolyline(pipe, room);
            for (int i = se.Count - 1; i >= 0; --i) 
            {
                if(PassageWayUtils.GetPointIndex(se[i],points)==-1)
                {
                    se.RemoveAt(i);
                }
            }
            if (se.Count != 2)
            {
                new NotSupportedException();
            }
            var fillet_poly=FilletUtils.FilletPolyline(pipe, se[0], se[1]);
        }
        protected List<Point3d> CleanThinBoundary(List<Point3d> coords)
        {
            //var index = GetNearestIndex(coords, pipe_in);
            var new_coords = new List<Point3d>();
            var count = coords.Count;
            for (int k = 0; k < count; k++)
            {
                int cur_index = k;
                int next_index = (k + 1) % count;
                var p0 = coords[cur_index];
                var p1 = coords[next_index];
                if (p0.DistanceTo(p1) < 1) continue;
                int p_1_index = (k-1+count) % count;
                int p2_index = (k + 2) % count;
                if ((p0 - p1).Length < buffer / 2 + 10)  
                {
                    // update the pre point and the third point
                    var dp = (p1 - p0) / 2;
                    coords[p_1_index] += dp;
                    coords[p2_index] -= dp;
                    if (new_coords.Count > 0)
                        new_coords[new_coords.Count - 1] = coords[p_1_index];
                    k += 1;
                    p0 = coords[cur_index] = coords[next_index] = new Point3d((p0.X + p1.X) / 2, (p0.Y + p1.Y) / 2, 0);
                }
                new_coords.Add(p0);
            }
            return new_coords;
        }
        /// <summary>
        /// 计算外层轮廓上距离内层轮廓的最近点
        /// </summary>
        /// <param name="a">外层轮廓，非闭合</param>
        /// <param name="b">内层轮廓，闭合</param>
        /// <returns></returns>
        Point3d GetClosedPointAtoB(Polyline a, Polyline b)
        {
            // 计算a上的最后一个满足间距的顶点。
            var points = PassageWayUtils.GetPolyPoints(a);
            points.Reverse();
            Point3d ret1 = points.FindByMin(o => b.Distance(o));
            points.Reverse();
            var dis = b.Distance(ret1);
            if (dis > 1.5 * buffer) 
            {
                var point_on_b = b.GetClosePoint(ret1);
                ret1 = a.GetClosePoint(point_on_b);
            }
            // 计算a的边上距离b最近的最后一个点
            Point3d ret2 = Point3d.Origin;
            double min_dis = double.MaxValue;
            for(int i = points.Count - 2; i >= 0; i--)
            {
                var line = new Line(points[i + 1], points[i]);
                for(int j=0;j<b.NumberOfVertices;j++)
                {
                    var point_on_a = line.GetClosestPointTo(b.GetPoint3dAt(j), false);
                    dis = point_on_a.DistanceTo(b.GetPoint3dAt(j));
                    if(dis<min_dis)
                    {
                        min_dis = dis;
                        ret2 = point_on_a;
                    }
                }
            }
            // 计算ret1的距离
            double dis1 = 0;
            int index = PassageWayUtils.GetSegIndex2(ret1, points);
            for (int i = 0; i < index; i++)
            {
                dis1 += (points[i + 1] - points[i]).Length;
            }
            dis1 += (ret1 - points[index]).Length;
            // 计算ret2的距离
            double dis2 = 0;
            index = PassageWayUtils.GetSegIndex2(ret2, points);
            for (int i = 0; i < index; i++)
            {
                dis2 += (points[i + 1] - points[i]).Length;
            }
            dis2 += (ret2 - points[index]).Length;
            return dis1 > dis2 ? ret1 : ret2;
        }
        protected void SmoothPolyline(List<Point3d> points)
        {
            if (points.Count <= 2) return;
            List<int> remove_head = new List<int>();
            for(int i = 0; i < points.Count - 1; i++)
            {
                if (points[i + 1].DistanceTo(points[i]) < 1)
                    remove_head.Add(i);
            }
            for (int i = remove_head.Count - 1; i >= 0; i--)
                points.RemoveAt(remove_head[i]);
            remove_head.Clear();
            for (int i = 1; i < points.Count - 2; ++i)
            {
                var pre_v = (points[i - 1] - points[i]).GetNormal();
                var cur_v = (points[i + 1] - points[i]).GetNormal();
                if (Math.Abs(pre_v.CrossProduct(cur_v).Z) < 1e-3 && pre_v.DotProduct(cur_v) < 0)
                    remove_head.Add(i);
                if (Math.Abs(pre_v.DotProduct(cur_v)) < 1e-3)
                    break;
            }
            List<int> remove_tail = new List<int>();
            var end = remove_tail.Count > 0 ? remove_tail.Last() : 1;
            for (int j = points.Count - 2; j > end; --j) 
            {
                var pre_v = (points[j + 1] - points[j]).GetNormal();
                var cur_v = (points[j - 1] - points[j]).GetNormal();
                if (Math.Abs(pre_v.CrossProduct(cur_v).Z) < 1e-3 && pre_v.DotProduct(cur_v) < 0)
                    remove_tail.Add(j);
                if (Math.Abs(pre_v.DotProduct(cur_v)) < 1e-3)
                    break;
            }
            for (int i = 0; i < remove_tail.Count; i++)
                points.RemoveAt(remove_tail[i]);
            for (int i = remove_head.Count-1; i >= 0; i--)
                points.RemoveAt(remove_head[i]);
            if (points.Count >= 3)
            {
                var p0 = points.Last();
                var p1 = points[points.Count - 2];
                var p2 = points[points.Count - 3];
                var dir1 = (p0 - p1).GetNormal();
                var dir2 = (p2 - p1).GetNormal();
                if (Math.Abs(dir1.CrossProduct(dir2).Z) < 1e-3 && dir1.DotProduct(dir2) > 0)
                    points.RemoveAt(points.Count - 1);
            }
        }

        public void Dispose()
        {
            PassageWayUtils.ClearListPoly(skeleton);
            PassageWayUtils.ClearListPoly(inner_shape);
            if(buffer_tree!=null)
                buffer_tree.Dispose();
        }
    }
}
