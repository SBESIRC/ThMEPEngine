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
        // inner structure
        BufferTreeNode buffer_tree = null;
        List<Polyline> skeleton = new List<Polyline>();             // 临时骨架线
        List<Polyline> inner_shape = new List<Polyline>();          // 最内层轮廓
        Polyline input_seg = null;                                  // 入口轮廓
        double adjust_dist = 1000;                                  // 避免轮廓尖角
        double smaller_last_dist = 0.8;                             // 最后一段内缩阈值倍数

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
            GetSkeleton(buffer_tree);
        }
        BufferTreeNode GetBufferTree(Polyline poly, int depth,bool flag = true)
        {
            var points = SmoothUtils.SmoothPolygon(PassageWayUtils.GetPolyPoints(poly));
            points.Add(points.First());
            poly = PassageWayUtils.BuildPolyline(points);
            BufferTreeNode node = new BufferTreeNode(poly, depth);
            var next_buffers = PassageWayUtils.Buffer(poly, flag ? -room_buffer : -buffer / 2);
            next_buffers = DealWithNextBuffers(next_buffers);
            if (next_buffers.Count == 0) return node;
            node.childs = new List<BufferTreeNode>();
            foreach (Polyline child_poly in next_buffers)
            {
                var child = GetBufferTree(child_poly, depth + 1, false);
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
        List<Polyline> DealWithNextBuffers(List<Polyline> next_buffers)
        {
            List<Polyline> ret = new List<Polyline>();
            foreach (var next_buffer in next_buffers)
            {
                // 内缩外扩得到real_poly
                var inner_polys = PassageWayUtils.Buffer(next_buffer, -buffer / 4 + 20);
                var real_polys = new List<Polyline>();
                foreach (var inner_poly in inner_polys)
                {
                    var real_poly = PassageWayUtils.Buffer(inner_poly, buffer / 4 - 20).Cast<Polyline>().First();
                    real_polys.Add(real_poly);
                }
                // 求差
                var rest_polys = next_buffer.Difference(real_polys.ToCollection()).Cast<Polyline>().ToList();
                // 删除：1、与多个real_poly中相交的rest_poly   2、不足推荐宽度的rest_poly
                for (int i = rest_polys.Count - 1; i >= 0; --i)
                {
                    var smaller_rest = PassageWayUtils.Buffer(rest_polys[i], -buffer / 4 + 30);
                    if(smaller_rest.Count==0)
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
                ret.AddRange(real_polys.ToCollection().UnionPolygons().Cast<Polyline>());
            }
            return ret;
        }
        void DealWithShell(BufferTreeNode node)
        {
            if (node.depth == 0) return;
            if (node.depth == 1)
            {
                var coords = PassageWayUtils.GetPolyPoints(node.shell);
                coords = SmoothUtils.SmoothPolygon(coords);
                var first = node.shell.GetClosePoint(pipe_input.pin);
                int index = PassageWayUtils.GetSegIndexOnPolygon(first, coords);
                var pre = (index + coords.Count - 1) % coords.Count;
                var next = (index + 1) % coords.Count;
                if ((coords[pre] - coords[index]).CrossProduct(coords[next] - coords[index]).Z < 0)
                    index = next;
                PassageWayUtils.RearrangePoints(ref coords, index);
                var p0 = coords[0];
                var p1 = coords.Last();
                coords.Add(p0 + (p1 - p0).GetNormal() * buffer);
                coords.Insert(0, p0 + (p1 - p0).GetNormal() * buffer / 2);
                PassageShowUtils.ShowPoints(coords);
                //PassageShowUtils.ShowEntity(PassageWayUtils.BuildPolyline(coords), 3);
                node.SetShell(PassageWayUtils.BuildPolyline(coords));
            }
            //else if (node.depth <= 3)   
            //{
            //    var coords = PassageWayUtils.GetPolyPoints(node.shell);
            //    var point = coords.FindByMin(o => o.DistanceTo(node.parent.shell.StartPoint));
            //    var index = PassageWayUtils.GetPointIndex(point, coords);
            //    PassageWayUtils.RearrangePoints(ref coords, index);
            //    var pre = (index - 1+coords.Count) % coords.Count;
            //    var next = (index + 1) % coords.Count;
            //    var dir = node.parent.shell.StartPoint - node.parent.shell.EndPoint;
            //    var pre_dir = coords[pre] - point;
            //    var next_dir = coords[next] - point;
            //    Point3d last_point = Point3d.Origin;
            //    PassageShowUtils.ShowPoint(point);
            //    if (PassageWayUtils.IsParallel(dir, pre_dir))
            //    {
            //        last_point = point + pre_dir.GetNormal() * buffer / 2;
            //        coords.Reverse(1, coords.Count - 1);
            //        coords.Add(last_point);
            //    }
            //    else
            //    {
            //        last_point = point + next_dir.GetNormal() * buffer / 2;
            //        coords.Add(last_point);
            //    }
            //    //coords.Insert(0, node.parent.shell.StartPoint);
            //    //coords.Add(node.parent.shell.EndPoint);
            //    if (node.depth % 2 == 0)
            //        coords.Reverse();
            //    node.SetShell(PassageWayUtils.BuildPolyline(coords));

            //}
            PassageShowUtils.ShowEntity(node.shell, ((node.depth+1) / 2) % 7 + 1);
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
