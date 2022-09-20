using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using GeometryExtensions;
using Linq2Acad;
using NetTopologySuite.Algorithm.Distance;
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
    public class RoomPipeGenerator1 : IDisposable
    {
        // input
        Polyline room { get; set; }
        PipeInput pipe_input { get; set; }
        double buffer { get; set; } = 500;
        double room_buffer { get; set; } = 100;
        double pipe_width { get; set; }

        // output
        public PipeOutput output;
        public List<Point3d> output_coords = new List<Point3d>();
        // inner structure
        BufferTreeNode buffer_tree = null;
        List<Polyline> skeleton = new List<Polyline>();             // 临时骨架线
        List<Polyline> inner_shape = new List<Polyline>();          // 最内层轮廓
        Polyline input_seg = null;                                  // 入口轮廓

        double buffer_coefficent = 0.8;                             // 向内做buffer的系数，需要大于0.5
        double buffer_threshold = 150;                                     // 最小间距

        RoomPipeGenerator1() { }

        public RoomPipeGenerator1(Polyline room, List<DrawPipeData> pipe_in_list, double buffer, double room_buffer = 100)
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
            buffer_tree = GetBufferTree(room,0);

            AddInputSegment();
        }
        BufferTreeNode GetBufferTree(Polyline poly, int depth, bool flag = true, BufferTreeNode parent = null)
        {
            var points = SmoothUtils.SmoothPolygon(PassageWayUtils.GetPolyPoints(poly));
            points.Add(points.First());
            poly = PassageWayUtils.BuildPolyline(points);

            if (PublicValue.Clear0 == 1) 
            {
                poly = ClearSinglePolyline.ClearBendsLongFirstClosed(poly, poly, Parameter.ClearSingleBufferDis);
            }

            BufferTreeNode node = new BufferTreeNode(poly, depth, parent);
            List<Polyline> next_buffers = null;
            // 第一层：先生成内层轮廓，后处理框线
            if (depth == 1)
            {
                next_buffers = PassageWayUtils.Buffer(poly, flag ? -room_buffer : -buffer / 2);
                next_buffers = DealWithNextBuffers(next_buffers, node.depth + 1);
                // 容差处理
                if (next_buffers.Count == 0)
                {
                    next_buffers = PassageWayUtils.Buffer(poly, flag ? -room_buffer : -buffer / 2 * buffer_coefficent);
                    next_buffers = DealWithNextBuffers(next_buffers, node.depth + 1);
                }

                DealWithOutShell(node, next_buffers);


            }
            // 第二层以上：先处理框线，后生成内层轮廓
            if(depth>=2)
            {
                DealWithDeepShell(node);
                points = PassageWayUtils.GetPolyPoints(node.shell);
                points.Add(points.First());
                poly = PassageWayUtils.BuildPolyline(points);
            }
            // 生成内层轮廓线
            if (depth != 1)
            {
                next_buffers = PassageWayUtils.Buffer(poly, flag ? -room_buffer : -buffer / 2);
                if (depth > 0) 
                      next_buffers = DealWithNextBuffers(next_buffers, node.depth + 1);
                // 容差处理
                if (next_buffers.Count == 0)
                {
                    next_buffers = PassageWayUtils.Buffer(poly, flag ? -room_buffer : -buffer / 2 * buffer_coefficent);

                    if (depth > 0)
                        next_buffers = DealWithNextBuffers(next_buffers, node.depth + 1);
                }
                // 最外层如果有多个，只保留能连接入口的那个区域
                if (depth == 0 && next_buffers.Count > 1) 
                {
                    var next_buffer = next_buffers.FindByMin(o => o.Distance(pipe_input.pin)).Clone() as Polyline;
                    PassageWayUtils.ClearListPoly(next_buffers);
                    next_buffers.Add(next_buffer);
                }
            }
            // 生成子节点
            if (next_buffers.Count == 0)
            {
                DealWithLeaveShell(node);
                return node;
            }
            node.childs = new List<BufferTreeNode>();
            foreach (Polyline child_poly in next_buffers)
            {
                var child = GetBufferTree(child_poly, depth + 1, false, node);
                node.childs.Add(child);
            }
            return node;
        }
        void AddInputSegment()
        {
            // 计算入口插头
            var points = PassageWayUtils.GetPolyPoints(room);
            var point = room.GetClosePoint(pipe_input.pin);
            var pre = PassageWayUtils.GetSegIndexOnPolygon(point, points);
            var next = (pre + 1) % points.Count;
            var dir = (points[next] - points[pre]).GetNormal().RotateBy(-Math.PI / 2, Vector3d.ZAxis);
            if (output_coords.Count == 0)
            {
                // 仅输出入口
                var le = pipe_input.pin + dir * room_buffer;
                var line = new Line(pipe_input.pin, le);
                input_seg = line.Buffer(pipe_input.in_buffer);
                output = new PipeOutput();
                output.pipe_id = pipe_input.pipe_id;
                output.skeleton = new List<Polyline>();
                output.shape = input_seg;
            }
            else
            {
                // 合并主要管线和入口
                var main_pipe = PassageWayUtils.BuildPolyline(output_coords);
                var dis = main_pipe.GetClosePoint(pipe_input.pin).DistanceTo(pipe_input.pin);
                var le = pipe_input.pin + dir * (dis + pipe_width);
                var line = new Line(pipe_input.pin, le);
                input_seg = line.Buffer(pipe_input.in_buffer);
                output = new PipeOutput();
                output.pipe_id = pipe_input.pipe_id;
                output.skeleton = new List<Polyline>();
                if(input_seg.Intersects(main_pipe))
                    output.shape = main_pipe.ToNTSPolygon().Union(input_seg.ToNTSPolygon()).ToDbCollection().Cast<Polyline>().First();
                else
                {
                    var ls = line.EndPoint;
                    le = main_pipe.GetClosePoint(ls);
                    var fixed_le = output_coords.OrderBy(o => o.DistanceTo(le)).First();
                    fixed_le += (le - fixed_le).GetNormal() * pipe_width;
                    var dp = fixed_le - le;
                    ls += dp;
                    le += dp;
                    ls += (ls - le).GetNormal() * pipe_input.in_buffer;
                    le += (le - ls).GetNormal() * pipe_width;
                    var line2 = new Line(ls, le);
                    //PassageShowUtils.ShowEntity(line2);
                    var output_shape = new DBObjectCollection();
                    output_shape.Add(main_pipe);
                    output_shape.Add(input_seg);
                    output_shape.Add(line2.Buffer(pipe_width));
                    output.shape = output_shape.UnionPolygons().Cast<Polyline>().First();
                    line2.Dispose();
                }
            }
        }
        List<Polyline> DealWithNextBuffers(List<Polyline> next_buffers,int depth)
        {
            List<Polyline> ret = new List<Polyline>();
            foreach (var next_buffer in next_buffers)
            {
                // 内缩外扩得到real_poly
                var inner_polys = PassageWayUtils.Buffer(next_buffer, -buffer / 4 * buffer_coefficent + 1, 1e-3);
                var real_polys = new List<Polyline>();
                double real_area = 0;
                foreach (var inner_poly in inner_polys)
                {
                    var real_poly = PassageWayUtils.Buffer(inner_poly, buffer / 4 * buffer_coefficent - 1, 1e-3).Cast<Polyline>().First();
                    real_polys.Add(real_poly);
                    real_area += real_poly.Area;
                }
                if (next_buffer.Area - real_area > 5)
                {
                    // 求差
                    var rest_polys = next_buffer.Difference(real_polys.ToCollection()).Cast<Polyline>().ToList();
                    // 删除：1、与多个real_poly中相交的rest_poly   2、不足推荐宽度的rest_poly
                    for (int i = rest_polys.Count - 1; i >= 0; --i)
                    {
                        var smaller_rest = PassageWayUtils.Buffer(rest_polys[i], -buffer_threshold / 2 + 1, 1e-3);
                        if (smaller_rest.Count == 0)
                        {
                            rest_polys[i].Dispose();
                            rest_polys.RemoveAt(i);
                            continue;
                        }
                        int count = 0;
                        foreach (var real_poly in real_polys)
                        {
                            if (rest_polys[i].ToNTSPolygon().Intersects(real_poly.ToNTSPolygon()))
                            {
                                count++;
                                if (count > 1)
                                    break;
                            }
                        }
                        if (count > 1)
                        {
                            rest_polys[i].Dispose();
                            rest_polys.RemoveAt(i);
                        }
                    }
                    // 添加结果
                    real_polys.AddRange(rest_polys);
                }
                ret.AddRange(real_polys.ToCollection().UnionPolygons().Cast<Polyline>());
            }
            return ret;
        }
        void DealWithOutShell(BufferTreeNode node,List<Polyline> next_buffers)
        {
            var coords = PassageWayUtils.GetPolyPoints(node.shell);
            coords = SmoothUtils.SmoothPolygon(coords);
            var first = node.shell.GetClosePoint(pipe_input.pin);
            int index = PassageWayUtils.GetSegIndexOnPolygon(first, coords);
            PassageWayUtils.RearrangePoints(ref coords, index);
            int is_clockwise = 1;
            // 判断入口宽度与推荐间距是否相同，如果相同，判断管道第一段是否不转弯，
            // 不转弯就将最近点所在边设置为最后一条边，且管道的第一条边与入口方向相同。
            if (Math.Abs(pipe_input.in_buffer - buffer / 4) < 1)
            {
                if (Math.Abs(first.DistanceTo(coords[0]) - buffer / 4) < 2)
                {
                    coords.Reverse(1, coords.Count - 1);
                    is_clockwise *= -1;
                }

                else if (Math.Abs(first.DistanceTo(coords[1]) - buffer / 4) < 2)
                    PassageWayUtils.RearrangePoints(ref coords, 1);
            }
            // 如果first所在边仍然是轮廓第一条边，则将长的邻接边作为最后一条边。
            // 且满足轮廓的第一个点不能是凹角顶点。
            if (PassageWayUtils.GetSegIndexOnPolygon(first, coords) == 0)
            {
                var pre_seg = coords.Last() - coords[0];
                var next_seg = coords[2] - coords[1];
                if (next_seg.Length > pre_seg.Length)
                {
                    PassageWayUtils.RearrangePoints(ref coords, 1);
                    coords.Reverse(1, coords.Count - 1);
                    is_clockwise *= -1;
                }
                var cur_seg = coords[1] - coords[0];
                pre_seg = coords[0] - coords.Last();
                bool is_pre_concaveangle = is_clockwise * (cur_seg.CrossProduct(pre_seg).Z) < -1;
                if (is_pre_concaveangle)
                {
                    PassageWayUtils.RearrangePoints(ref coords, 1);
                    coords.Reverse(1, coords.Count - 1);
                    is_clockwise *= -1;
                }
            }
            if (next_buffers.Count != 0)
            {
                // 计算断点last_point、对应的child和child_point
                Point3d last_point = Point3d.Origin;
                double last_dis = -1;
                int last_child_idx = -1;
                Point3d child_point = Point3d.Origin;
                coords.Add(coords.First());
                for (int i = 0; i < next_buffers.Count; ++i)
                {
                    var point = GetLastPointAtoB(coords, next_buffers[i], is_clockwise, node.depth);
                    var dis = GetDistancePtoS(coords, point);
                    if (dis > last_dis)
                    {
                        last_point = point;
                        last_dis = dis;
                        last_child_idx = i;
                        child_point = next_buffers[i].GetClosePoint(last_point);
                    }
                }
                coords.RemoveAt(coords.Count - 1);
                index = PassageWayUtils.GetPointIndex(last_point, coords);
                if (index == -1)
                {
                    var child_coords = PassageWayUtils.GetPolyPoints(next_buffers[last_child_idx]);
                    child_coords = SmoothUtils.SmoothPolygon(child_coords);
                    if (is_clockwise == -1) child_coords.Reverse();
                    var child_index = PassageWayUtils.GetPointIndex(child_point, child_coords);
                    // 计算pre_child_point
                    var pre_child_index = (child_index - 1 + child_coords.Count) % child_coords.Count;
                    var pre_child_point = child_coords[pre_child_index];
                    if (pre_child_point.DistanceTo(child_point) > buffer / 2) 
                        pre_child_point = child_point + (pre_child_point - child_point).GetNormal() * buffer / 2;
                    // 计算pre_point
                    var pre_point = GetLastClosePoint(coords, pre_child_point);
                    index = PassageWayUtils.GetSegIndexOnPolygon(last_point, coords);
                    var next = (index + 1) % coords.Count;
                    PassageWayUtils.RearrangePoints(ref coords, next);
                    coords.Add(pre_point);
                    coords.Insert(0, last_point);
                }
                else
                {
                    var child_coords = PassageWayUtils.GetPolyPoints(next_buffers[last_child_idx]);
                    child_coords = SmoothUtils.SmoothPolygon(child_coords);
                    if (is_clockwise == -1) child_coords.Reverse();
                    var child_index = PassageWayUtils.GetPointIndex(child_point, child_coords);
                    // 计算pre_child_point
                    var pre_child_index = (child_index - 1 + child_coords.Count) % child_coords.Count;
                    if (child_index == -1)
                    {
                        pre_child_index = PassageWayUtils.GetSegIndexOnPolygon(child_point, child_coords);
                    }
                    var pre_child_point = child_coords[pre_child_index];
                    if (pre_child_point.DistanceTo(child_point) > buffer / 2)
                        pre_child_point = child_point + (pre_child_point - child_point).GetNormal() * buffer / 2;
                    // 计算pre_point
                    var pre_point = GetLastClosePoint(coords, pre_child_point);
                    PassageWayUtils.RearrangePoints(ref coords, index);
                    coords.Add(pre_point);
                }
            }
            else
            {
                coords.Add(coords.First());
            }
            coords = SmoothUtils.SmoothPoints(coords);
            node.IsCW = is_clockwise;

            node.SetShell(PassageWayUtils.BuildPolyline(coords));
            output_coords.AddRange(coords);
            output_coords = SmoothUtils.SmoothPoints(output_coords);
        }
        void DealWithDeepShell(BufferTreeNode node)
        {
            if (node.depth >=2)
            {
                // 获取当前节点和其父节点的shell点集
                var parent_coords = PassageWayUtils.GetPolyPoints(node.parent.shell, true);
                var coords = PassageWayUtils.GetPolyPoints(node.shell);
                if (node.parent.IsCW == -1)
                {
                    coords.Reverse();
                    node.IsCW = -1;
                }
                coords = SmoothUtils.SmoothPolygon(coords);
                // 找到外轮廓的连接点：1、距离当前轮廓小于buffer/2  2、靠近外轮廓终点  3、距离外轮廓的上一个点大于buffer/2  3、当前轮廓上的点距离上一个点大于buffer/2*buffer_coefficent
                var last_point = GetLastPointAtoB(parent_coords, node.shell, node.IsCW, node.depth);
                // 找到当前轮廓的连接点
                var point = node.shell.GetClosePoint(last_point);
                var index = PassageWayUtils.GetPointIndex(point, coords);
                // 点位重排
                if (index != -1)
                {
                    PassageWayUtils.RearrangePoints(ref coords, index);
                }
                else
                {
                    index = PassageWayUtils.GetSegIndexOnPolygon(point, coords);
                    var next = (index + 1) % coords.Count;
                    PassageWayUtils.RearrangePoints(ref coords, next);
                    coords.Insert(0, point);
                }
                // 找到当前轮廓的上一个连接点
                var pre_point = coords.Last();
                if (pre_point.DistanceTo(point) > buffer / 2) 
                {
                    pre_point = point + (pre_point - point).GetNormal() * buffer / 2;
                    coords.Add(pre_point);
                }
                // 找到外轮廓的上一个连接点
                var pre_parent_point = GetLastClosePoint(parent_coords, pre_point);
                // 将外轮廓上的两个点加入到当前轮廓
                coords.Insert(0, last_point);
                coords.Add(pre_parent_point);
                // 轮廓点位调整
                coords = SmoothUtils.SmoothPoints(coords);
                if (coords.Count >= 6) 
                {
                    var dp0 = coords[1] - coords[0];
                    var dp1 = coords[2] - coords[1];
                    var dp2 = coords[3] - coords[2];
                    var dp3 = coords[4] - coords[3];
                    if (coords.Count >= 8
                        && dp0.Length <= buffer / 2 + 2                    // 存在连接边
                        && node.IsCW * dp0.CrossProduct(dp1).Z > 10     // 凹角
                        && node.IsCW * dp1.CrossProduct(dp2).Z < -10    // 凸角
                        && node.IsCW * dp2.CrossProduct(dp3).Z > 10     // 凹角
                        && dp1.Length < buffer                          // 第一条边太短
                        || dp1.Length < buffer / 2 * (1 - buffer_coefficent) + 2)                          
                    {
                        coords[2] -= dp1;
                        coords[3] -= dp1;
                    }
                }
                if (coords.Count >= 6)
                {
                    var dp0 = coords[coords.Count - 2] - coords[coords.Count - 1];
                    var dp1 = coords[coords.Count - 3] - coords[coords.Count - 2];
                    var dp2 = coords[coords.Count - 4] - coords[coords.Count - 3];
                    var dp3 = coords[coords.Count - 5] - coords[coords.Count - 4];
                    if (coords.Count >= 8
                        && dp0.Length <= buffer / 2 + 2                    // 存在连接边
                        && node.IsCW * dp1.CrossProduct(dp0).Z > 10     // 凹角
                        && node.IsCW * dp2.CrossProduct(dp1).Z < -10    // 凸角
                        && node.IsCW * dp3.CrossProduct(dp2).Z > 10     // 凹角
                        && dp1.Length < buffer                          // 最后一条边
                        || dp1.Length < buffer / 2 * (1 - buffer_coefficent) + 2)
                    {
                        coords[coords.Count - 4] -= dp1;
                        coords[coords.Count - 3] -= dp1;
                    }
                }
                coords = SmoothUtils.SmoothPoints(coords);
                // 生成轮廓线
                node.SetShell(PassageWayUtils.BuildPolyline(coords));
                // 将当前轮廓加入到整条管道中
                index = PassageWayUtils.GetPointIndex(last_point, output_coords);
                if (node.depth % 2 == 0)
                {
                    coords.Reverse();
                }

                if (index == 0)
                {
                    output_coords.AddRange(coords);
                }
                else if (index != -1)
                {
                    if (node.depth % 2 == 0)
                    {
                        output_coords.InsertRange(index, coords);
                    }
                    else
                    {
                        output_coords.InsertRange(index + 1, coords);
                    }
                }
                else
                {
                    index = PassageWayUtils.GetSegIndexOnPolyline(last_point, output_coords);
                    output_coords.InsertRange(index + 1, coords);
                }
                output_coords = SmoothUtils.SmoothPoints(output_coords);
            }
        }
        void DealWithLeaveShell(BufferTreeNode node)
        {
            if (node.depth >= 2)
            {
                var coords = PassageWayUtils.GetPolyPoints(node.shell, true);
                var shell = PassageWayUtils.BuildPolyline(coords);
                var inner_buffers = PassageWayUtils.Buffer(shell, -buffer / 2);
                if (inner_buffers.Count == 1)
                {
                    var inner_buffer = inner_buffers[0];
                    //PassageShowUtils.ShowEntity(inner_buffer, node.depth % 2 + 3);
                    var inner_coords = PassageWayUtils.GetPolyPoints(inner_buffer);
                    if (inner_coords.Count == 4)
                    {
                        if (node.IsCW == -1) 
                            inner_coords.Reverse(1, inner_coords.Count - 1);
                        if (node.depth % 2 == 0)
                        {
                            var dp = inner_coords[3] - inner_coords[0];
                            // 去除最后两段
                            if (dp.Length < buffer * (buffer_coefficent - 0.5)) 
                                return;
                            // 去除最后一段，倒数第二段均匀分布
                            if (dp.Length < buffer)
                            {
                                inner_coords[3] = inner_coords[0] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                inner_coords[2] = inner_coords[1] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                inner_coords[0] += (inner_coords[0] - inner_coords[1]).GetNormal() * buffer / 2;
                                inner_coords[3] += (inner_coords[3] - inner_coords[2]).GetNormal() * buffer / 2;
                            }
                            else
                            {
                                dp = inner_coords[0] - inner_coords[1];
                                // 去除最后一段，倒数第二段不做处理
                                if (dp.Length < buffer * (buffer_coefficent - 0.5)-1)
                                {
                                    inner_coords[0] += dp.GetNormal() * buffer / 2;
                                    inner_coords[3] += dp.GetNormal() * buffer / 2;
                                }
                                // 最后一段均匀处理
                                else if (dp.Length < buffer / 2 * buffer_coefficent) 
                                {
                                    inner_coords[0] = inner_coords[1] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                    inner_coords[3] = inner_coords[2] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                    inner_coords.Add(inner_coords[0] + (inner_coords[3] - inner_coords[0]).GetNormal() * buffer / 2);
                                    inner_coords.Add(inner_coords.Last() + dp.GetNormal() * ((dp.Length + buffer / 2) / 2));
                                    inner_coords[0] += dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                }
                                else
                                {
                                    inner_coords[0] += dp.GetNormal() * buffer / 2;
                                    inner_coords[3] += dp.GetNormal() * buffer / 2;
                                }
                            }
                            var index = PassageWayUtils.GetPointIndex(inner_coords[0], output_coords);
                            if (index != -1)
                            {
                                output_coords.InsertRange(index + 1, inner_coords);
                                output_coords = SmoothUtils.SmoothPoints(output_coords);
                            }
                        }
                        else
                        {
                            inner_coords.Reverse(1, inner_coords.Count - 1);
                            var dp = inner_coords[1] - inner_coords[0];
                            // 去除最后两段
                            if (dp.Length < buffer * (buffer_coefficent - 0.5)-1)
                                return;
                            // 去除最后一段，倒数第二段均匀分布
                            if (dp.Length < buffer / 2 * buffer_coefficent)
                            {
                                inner_coords[1] = inner_coords[0] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                inner_coords[2] = inner_coords[3] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                inner_coords[0] += (inner_coords[0] - inner_coords[3]).GetNormal() * buffer / 2;
                                inner_coords[1] += (inner_coords[1] - inner_coords[2]).GetNormal() * buffer / 2;
                            }
                            else
                            {
                                dp = inner_coords[0] - inner_coords[3];
                                // 去除最后一段，倒数第二段不做处理
                                if (dp.Length < buffer * (buffer_coefficent - 0.5)-1)
                                {
                                    inner_coords[0] += dp.GetNormal() * buffer / 2;
                                    inner_coords[1] += dp.GetNormal() * buffer / 2;
                                }
                                // 最后一段均匀化
                                else if (dp.Length < buffer / 2 * buffer_coefficent)
                                {
                                    inner_coords[0] = inner_coords[3] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                    inner_coords[1] = inner_coords[2] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                    inner_coords.Insert(1, inner_coords[0] + (inner_coords[1] - inner_coords[0]).GetNormal() * buffer / 2);
                                    inner_coords.Insert(1, inner_coords[1] + dp.GetNormal() * ((dp.Length + buffer / 2) / 2));
                                    inner_coords[0] += dp.GetNormal() * ((dp.Length + buffer / 2) / 2);
                                }
                                else
                                {
                                    inner_coords[0] += dp.GetNormal() * buffer / 2;
                                    inner_coords[1] += dp.GetNormal() * buffer / 2;
                                }
                            }
                            var index = PassageWayUtils.GetPointIndex(inner_coords[0], output_coords);
                            if (index != -1)
                            {
                                inner_coords.RemoveAt(0);
                                output_coords.InsertRange(index, inner_coords);
                                output_coords = SmoothUtils.SmoothPoints(output_coords);
                            }

                        }
                    }
                }
                else
                {
                    if (node.depth % 2 == 1) return;
                    coords.RemoveAt(coords.Count - 1);
                    var dp0 = coords[coords.Count - 2] - coords[coords.Count - 1];
                    var dp1 = coords[coords.Count - 3] - coords[coords.Count - 2];
                    var fisrt_dp = coords[1] - coords[0];
                    if (dp0.Length <= buffer / 2 + 2                    // 存在连接边
                        && node.IsCW * dp1.CrossProduct(dp0).Z > 10     // 凹角
                        && dp1.Length > buffer
                        && fisrt_dp.Length > buffer)
                    {
                        var inner_coords = new List<Point3d>();
                        inner_coords.Add(coords[coords.Count - 2]);
                        inner_coords.Add(coords[coords.Count - 2] + dp0.GetNormal() * (fisrt_dp.Length - buffer));
                        inner_coords.Add(inner_coords[1] + dp1.GetNormal() * buffer / 2);
                        inner_coords.Add(inner_coords[0] + dp1.GetNormal() * buffer / 2);
                        inner_coords.Add(inner_coords.First());
                        var inner_poly = PassageWayUtils.BuildPolyline(inner_coords);
                        coords.Add(coords.First());
                        var shell_poly = PassageWayUtils.BuildPolyline(coords);
                        //PassageShowUtils.ShowEntity(inner_poly);
                        var buffer_inner_poly = PassageWayUtils.Buffer(inner_poly, -1);
                        if(buffer_inner_poly.Count>0)
                        {
                            inner_poly = buffer_inner_poly.First();
                            if (!inner_poly.ToNTSLineString().Intersects(shell_poly.ToNTSLineString()))
                            {
                                inner_coords.RemoveAt(inner_coords.Count - 1);
                                var index = PassageWayUtils.GetPointIndex(inner_coords[0], output_coords);
                                if (index != -1)
                                {
                                    output_coords.InsertRange(index + 1, inner_coords);
                                }
                            }
                        }
                        inner_poly.Dispose();
                        shell_poly.Dispose();
                    }
                }
            }
        }
        /// <summary>
        /// 找到外轮廓的连接点：1、距离当前轮廓小于buffer/2  2、靠近外轮廓终点  3、距离外轮廓的上一个点大于buffer/2  4、当前轮廓上的点距离上一个点大于buffer_threshold
        /// </summary>
        /// <param name="a_coords">多段线点集形式的外轮廓</param>
        /// <param name="b">内轮廓</param>
        /// <param name="isCW">外轮廓方向</param>
        /// <returns></returns>
        Point3d GetLastPointAtoB(List<Point3d> a_coords, Polyline b, int isCW, int depth=5)
        {
            var b_coords = PassageWayUtils.GetPolyPoints(b);
            if (isCW == -1) b_coords.Reverse();
            // 寻找距离b最近的a上的最后一个顶点
            Point3d last_vertex = Point3d.Origin;
            for(int i=0;i<a_coords.Count;++i)
            {
                var close_point = b.GetClosePoint(a_coords[i]);
                // 检查是否满足轮廓之间的距离要求
                if (close_point.DistanceTo(a_coords[i]) < buffer / 2 + 2)
                {
                    if (i == 0) continue;
                    // 计算内轮廓的另一个连接点
                    var pre_index = PassageWayUtils.GetPointIndex(close_point, b_coords);
                    if (pre_index == -1)
                        pre_index = PassageWayUtils.GetSegIndexOnPolygon(close_point, b_coords);
                    else
                        pre_index = (pre_index - 1 + b_coords.Count) % b_coords.Count;
                    // 不能是斜边
                    if (pre_index == -1)
                        continue;
                    // 外轮廓与内轮廓断线同向
                    if (!PassageWayUtils.IsParallel(a_coords[i] - a_coords[i - 1], close_point - b_coords[pre_index]))
                        continue;
                    var child_dis = close_point.DistanceTo(b_coords[pre_index]);
                    // 条件4
                    if (child_dis < buffer_threshold - 2) 
                        continue;
                    // 条件3
                    var parent_dis = a_coords[i].DistanceTo(a_coords[i - 1]);
                    if (parent_dis < buffer / 2 - 2 && parent_dis < child_dis - 2)
                        continue;
                    last_vertex = a_coords[i];
                }
            }
            double last_vertex_dis = GetDistancePtoS(a_coords, last_vertex);
            // 寻找距离b最近的a的最后一个边上的点
            Point3d last_point = Point3d.Origin;
            double last_point_dis = double.MinValue;
            for(int i=0;i<a_coords.Count-1;++i)
            {
                for(int j=0;j<b_coords.Count;++j)
                {
                    var line = new Line(a_coords[i], a_coords[i + 1]);
                    var point = line.GetClosestPointTo(b_coords[j], false);
                    line.Dispose();
                    if (point.DistanceTo(b_coords[j]) < buffer / 2 + 2)  
                    {
                        var b_pre = b_coords[(j - 1+b_coords.Count) % b_coords.Count];
                        if (!PassageWayUtils.IsParallel(point - a_coords[i], b_coords[j] - b_pre)   // 外轮廓与内轮廓断线同向
                            || point.DistanceTo(a_coords[i]) < buffer / 2 - 2                          // 条件3
                            || b_coords[j].DistanceTo(b_pre) < buffer_threshold - 2)       // 条件4
                            continue;
                        var dis = GetDistancePtoS(a_coords, point);
                        if (dis > last_point_dis)
                        {
                            last_point_dis = dis;
                            last_point = point;
                        }
                    }
                }
            }
            return last_vertex_dis > last_point_dis ? last_vertex : last_point;
        }

        Point3d GetLastPointAToByB(List<Point3d> a_coords,List<Point3d> b_coords)
        {
            // 寻找距离b最近的a的最后一个边上的点
            Point3d last_point = Point3d.Origin;
            double last_point_dis = double.MinValue;
            for (int i = 0; i < a_coords.Count - 1; ++i)
            {
                for (int j = 0; j < b_coords.Count; ++j)
                {
                    var line = new Line(a_coords[i], a_coords[i + 1]);
                    var point = line.GetClosestPointTo(b_coords[j], false);
                    line.Dispose();
                    if (point.DistanceTo(b_coords[j]) < buffer / 2 + 10)
                    {
                        var next = (j + 1) % b_coords.Count;
                        if (point.DistanceTo(a_coords[i]) < buffer / 2 + 10)  
                            continue;
                        if (!PassageWayUtils.IsParallel(point - b_coords[j], b_coords[j] - b_coords[next]))
                            continue;
                        var dis = GetDistancePtoS(a_coords, point);
                        if (dis > last_point_dis)
                        {
                            last_point_dis = dis;
                            last_point = point;
                        }
                    }
                }
            }
            return last_point;
        }
        double GetDistancePtoS(List<Point3d> coords,Point3d p)
        {
            double dis = 0;
            var index = PassageWayUtils.GetSegIndex2(p,coords);
            if (index == -1) return -1;
            for (int i = 0; i < index; i++)
            {
                dis += (coords[i + 1] - coords[i]).Length;
            }
            dis += (p - coords[index]).Length;
            return dis;
        }
        /// <summary>
        /// 寻找poly上距离p最近的最后一个点
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        Point3d GetLastClosePoint(List<Point3d> coords,Point3d p)
        {
            coords.Add(coords.First());

            double min_dis = double.MaxValue;
            Point3d ret = Point3d.Origin;
            for(int i=coords.Last()==coords.First()?coords.Count-2:coords.Count-1;i>=0;--i)
            {
                if(coords[i].DistanceTo(p)<min_dis-0.5)
                {
                    min_dis = coords[i].DistanceTo(p);
                    ret = coords[i];
                }    
            }
            var road_dis = GetDistancePtoS(coords, ret);
            for(int i = coords.Count - 2; i >= 0; --i)
            {
                var line = new Line(coords[i], coords[i + 1]);
                var point = line.GetClosestPointTo(p, false);
                line.Dispose();
                var dis = point.DistanceTo(p);
                if (dis < min_dis - 1)
                {
                    min_dis = dis;
                    ret = point;
                    road_dis = GetDistancePtoS(coords, ret);
                }
                else if (dis < min_dis + 1) 
                {
                    min_dis = dis;
                    if (GetDistancePtoS(coords, point) > road_dis + 1)
                    {
                        ret = point;
                        road_dis = GetDistancePtoS(coords, ret);
                    }

                }
            }
            coords.RemoveAt(coords.Count - 1);
            return ret;
        }

        public void Dispose()
        {
            PassageWayUtils.ClearListPoly(skeleton);
            PassageWayUtils.ClearListPoly(inner_shape);
            if (buffer_tree != null)
                buffer_tree.Dispose();
        }
    }
}
