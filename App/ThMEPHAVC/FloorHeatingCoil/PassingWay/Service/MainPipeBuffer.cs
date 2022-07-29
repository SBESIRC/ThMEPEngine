using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPHVAC.FloorHeatingCoil
{
    public class MainPipeBuffer
    {
        // 输入数据
        List<Polyline> raw_skeletons;
        List<Line> equispaced_lines;
        List<double> equispaced_buffers;
        PipeInput main_pipe_input;
        double buffer;
        bool main_has_output;
        // 中间数据
        List<List<Point3d>> skeletons;
        MPolygon raw_buffer = null;
        // 输出数据
        public PipeOutput output = new PipeOutput();
        public MainPipeBuffer(
            List<Polyline> skeletons,
            List<Line> equispaced_lines,
            List<double> equispaced_buffers,
            PipeInput main_pipe_input,
            double buffer,
            bool main_has_output)
        {
            this.raw_skeletons = skeletons;
            this.equispaced_lines = equispaced_lines;
            this.equispaced_buffers = equispaced_buffers;
            this.main_pipe_input = main_pipe_input;
            this.buffer = buffer;
            this.main_has_output = main_has_output;
            this.output = new PipeOutput();
            this.output.pipe_id = main_pipe_input.pipe_id;
        }
        
        // 测试用构造函数
        public MainPipeBuffer(Polyline shell,Polyline hole,int buffer)
        {
            output = new PipeOutput();
            output.shape = BaseMergeHoleAndShell(hole, shell, buffer);
        }
        public void Calculate()
        {
            MergeSkeleton();
            BufferMainWay();
            ConnectHoles();
        }
        void MergeSkeleton()
        {
            skeletons = new List<List<Point3d>>();
            foreach (var poly in raw_skeletons)
                skeletons.Add(SmoothUtils.SmoothPoints(poly.GetPoints().ToList()));
            if (!main_has_output && skeletons.Count > 1)
                skeletons.RemoveAt(1);

            while (true)
            {
                bool merge = false;
                for (int i = 0; i < skeletons.Count; ++i)
                {
                    for (int j = 0; j < skeletons.Count; ++j)
                    {
                        if (j != i && skeletons[i].Last().DistanceTo(skeletons[j].First()) < 1)
                        {
                            skeletons[i].AddRange(skeletons[j].GetRange(1, skeletons[j].Count - 1));
                            skeletons.RemoveAt(j);
                            merge = true;
                            break;
                        }
                    }
                    if (merge)
                        break;
                }
                if (!merge)
                    break;
            }
            output.skeleton = new List<Polyline>();
            for (int i = 0; i < skeletons.Count; ++i)
            {
                skeletons[i] = SmoothUtils.SmoothPoints(skeletons[i]);
                output.skeleton.Add(PassageWayUtils.BuildPolyline(skeletons[i]));
            }
        }
        void BufferMainWay()
        {
            DBObjectCollection ret = new DBObjectCollection();
            foreach (var points in skeletons)
            {
                List<double> buff_list = new List<double>();
                for (int i = 0; i < points.Count - 1; ++i)
                {
                    var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[i], points[i + 1]);
                    var p = points[i] + (points[i + 1] - points[i]) / 2;
                    double buff = buffer / 4;
                    for (int j = 0; j < equispaced_lines.Count; ++j)
                    {
                        var p0 = equispaced_lines[j].StartPoint;
                        var p1 = equispaced_lines[j].EndPoint;
                        var line_dir = PassageWayUtils.GetDirBetweenTwoPoint(p0, p1);
                        if (PassageWayUtils.PointOnSegment(p, p0, p1) && dir % 2 == line_dir % 2)
                        {
                            buff = equispaced_buffers[j];
                            break;
                        }
                    }
                    if (main_has_output)
                    {
                        if (i == points.Count - 2 && points[i + 1].DistanceTo(main_pipe_input.pout) < 1)
                            buff = main_pipe_input.out_buffer;
                    }
                    buff_list.Add(buff);
                }
                var main_seg = new BufferPoly(points, buff_list);
                ret.Add(main_seg.BufferWithHole());
            }
            raw_buffer = ret.UnionPolygons(true).Cast<MPolygon>().First();
        }
        void ConnectHoles()
        {
            var nts_polygon = raw_buffer.ToNTSPolygon();
            var shell = nts_polygon.Shell.ToDbPolyline();
            //PassageShowUtils.ShowEntity(shell,0);
            if (nts_polygon.Holes.Count() == 0)
            {

                var points = PreProcess(shell, true);
                shell = PassageWayUtils.BuildPolyline(points);
                output.shape = shell;
                return;
            }
            List<Polyline> holes = new List<Polyline>();
            foreach (var hole in nts_polygon.Holes)
            {
                holes.Add(hole.ToDbPolyline());
                //PassageShowUtils.ShowEntity(holes.Last());
            }

            foreach (var hole in holes)
            {
                shell=BaseMergeHoleAndShell(hole, shell, buffer);
            }
            output.shape = shell;
        }
        List<Point3d> PreProcess(Polyline poly, bool first_equal_last = false)
        {
            if (first_equal_last)
                return SmoothUtils.SmoothPoints(PassageWayUtils.GetPolyPoints(poly, true));
            return SmoothUtils.SmoothPolygon(PassageWayUtils.GetPolyPoints(poly));
        }

        List<Point3d> CheckPointPair(Point3d a, Point3d b, Polyline shell, double buffer)
        {
            List<Point3d> ret = new List<Point3d>();
            var dir = PassageWayUtils.GetDirBetweenTwoPoint(a, b);
            bool on_hline = dir % 2 == 1;
            var pa_on_shell = IntersectUtils.GetClosedPointOnShell(a, shell, on_hline);
            var pb_on_shell = IntersectUtils.GetClosedPointOnShell(b, shell, on_hline);
            if (pa_on_shell.DistanceTo(a) < buffer + 10)
                ret.Add(pa_on_shell);
            if (pb_on_shell.DistanceTo(b) < buffer + 10)
                ret.Add(pb_on_shell);
            return ret;
        }

        List<Point3d> MergeHoleAndShell(
            List<Point3d> shell_points, List<Point3d> hole_points,
            Point3d hole_start, Point3d hole_end,
            Point3d shell_start, Point3d shell_end)
        {
            hole_points.Add(hole_points.First());
            int hcur_index = PassageWayUtils.GetPointIndex(hole_start, hole_points, 2);
            if (hcur_index == -1) IntersectUtils.InsertPoint(hole_start, ref hole_points, 2);
            int hnext_index = PassageWayUtils.GetPointIndex(hole_end, hole_points, 2);
            if (hnext_index == -1) IntersectUtils.InsertPoint(hole_end, ref hole_points, 2);
            hcur_index = PassageWayUtils.GetPointIndex(hole_start, hole_points, 2);
            hnext_index = PassageWayUtils.GetPointIndex(hole_end, hole_points, 2);
            hole_points.RemoveAt(hole_points.Count - 1);

            int scur_index = PassageWayUtils.GetPointIndex(shell_start, shell_points, 2);
            if (scur_index == -1) IntersectUtils.InsertPoint(shell_start, ref shell_points, 2);
            int snext_index = PassageWayUtils.GetPointIndex(shell_end, shell_points, 2);
            if (snext_index == -1) IntersectUtils.InsertPoint(shell_end, ref shell_points, 2);
            scur_index = PassageWayUtils.GetPointIndex(shell_start, shell_points, 2);
            snext_index = PassageWayUtils.GetPointIndex(shell_end, shell_points, 2);
            PassageWayUtils.RearrangePoints(ref hole_points, hnext_index);
            hole_points.Reverse();
            shell_points.InsertRange(snext_index, hole_points);
            shell_points = SmoothUtils.SmoothPoints(shell_points);
            return shell_points;
        }

        Polyline BaseMergeHoleAndShell(Polyline hole, Polyline shell, double buffer)
        {
            var env = hole.ToNTSPolygon().EnvelopeInternal;
            if (env.Width < buffer || env.Height < buffer) return shell.Clone() as Polyline;
            var shell_points = PreProcess(shell, true);
            var hole_points = PreProcess(hole, false);
            // 优先选择长为300的边
            for (int j = 0; j < hole_points.Count; ++j)
            {
                var next = (j + 1) % hole_points.Count;
                var nnext = (next + 1) % hole_points.Count;

                var next_dir = PassageWayUtils.GetDirBetweenTwoPoint(hole_points[next], hole_points[nnext]);
                var dp = hole_points[next] - hole_points[j];
                // 找到洞的出口
                if (dp.Length <= 300 + 10 && dp.Length >= 300 - 10)
                {
                    var ret = CheckPointPair(hole_points[j], hole_points[next], shell, 300);
                    // 两个点都在内层
                    if (ret.Count == 0) continue;
                    // 两个点可以与外层直接连接
                    if (ret.Count == 2)
                    {
                        shell_points = MergeHoleAndShell(shell_points, hole_points, hole_points[j], hole_points[next], ret[0], ret[1]);
                        return PassageWayUtils.BuildPolyline(shell_points);
                    }
                    // 有一个点太远
                    else
                    {
                        // 找前一段线
                        var pre = (j - 1 + hole_points.Count) % hole_points.Count;
                        if (hole_points[pre].DistanceTo(hole_points[j]) >= 300)
                        {
                            var pre_dir = PassageWayUtils.GetDirBetweenTwoPoint(hole_points[pre], hole_points[j]);
                            var point_pre = hole_points[j] - Vector3d.XAxis.RotateBy(Math.PI / 2 * pre_dir, Vector3d.ZAxis) * 300;
                            ret = CheckPointPair(point_pre, hole_points[j], shell, 300);
                            // 两个点可以与外层直接连接
                            if (ret.Count == 2)
                            {
                                shell_points = MergeHoleAndShell(shell_points, hole_points, point_pre, hole_points[j], ret[0], ret[1]);
                                return PassageWayUtils.BuildPolyline(shell_points);
                            }
                        }
                        // 找后一段线
                        if (hole_points[nnext].DistanceTo(hole_points[next]) >= 300)
                        {
                            var point_next = hole_points[next] + Vector3d.XAxis.RotateBy(Math.PI / 2 * next_dir, Vector3d.ZAxis) * 300;
                            ret = CheckPointPair(hole_points[next], point_next, shell, 300);
                            // 两个点可以与外层直接连接
                            if (ret.Count == 2)
                            {
                                shell_points = MergeHoleAndShell(shell_points, hole_points, hole_points[next], point_next, ret[0], ret[1]);
                                return PassageWayUtils.BuildPolyline(shell_points);
                            }
                        }
                    }
                }
            }
            // 选择最短外边
            double min_length = double.MaxValue;
            int cur = -1;
            for (int j = 0; j < hole_points.Count; ++j)
            {
                var next = (j + 1) % hole_points.Count;
                var dis = hole_points[next].DistanceTo(hole_points[j]);
                if (dis >= 200 && dis < min_length)
                {
                    var ret = CheckPointPair(hole_points[j], hole_points[next], shell, buffer);
                    if (ret.Count == 2)
                    {
                        min_length = hole_points[next].DistanceTo(hole_points[j]);
                        cur = j;
                    }
                }
            }
            if (cur == -1) return shell.Clone() as Polyline;
            var pcur = hole_points[cur];
            var pnext = hole_points[(cur + 1) % hole_points.Count];
            if (min_length < buffer)
            {
                var ret = CheckPointPair(pcur, pnext, shell, buffer);
                shell_points = MergeHoleAndShell(shell_points, hole_points, pcur, pnext, ret[0], ret[1]);
                return PassageWayUtils.BuildPolyline(shell_points);
            }
            else
            {
                pnext = pcur + (pnext - pcur).GetNormal() * buffer;
                var ret = CheckPointPair(pcur, pnext, shell, buffer);
                if (ret.Count == 2)
                {
                    shell_points = MergeHoleAndShell(shell_points, hole_points, pcur, pnext, ret[0], ret[1]);
                    return PassageWayUtils.BuildPolyline(shell_points);
                }
                else
                {
                    pcur = pnext + (pcur - pnext).GetNormal() * buffer;
                    ret = CheckPointPair(pcur, pnext, shell, buffer);
                    if (ret.Count == 2)
                    {
                        shell_points = MergeHoleAndShell(shell_points, hole_points, pcur, pnext, ret[0], ret[1]);
                        return PassageWayUtils.BuildPolyline(shell_points);
                    }
                    else
                        return shell.Clone() as Polyline;
                }
            }
        }
    }
}
