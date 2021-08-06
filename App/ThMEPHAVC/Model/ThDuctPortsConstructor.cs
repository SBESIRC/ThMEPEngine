using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class Duct_ports_Info
    {
        public Line l { set; get; }
        public double width { set; get; }
        public string duct_size { set; get; }
        public Point3d start_point { set; get; }
        public List<Port_Info> ports_info { set; get; }
        public Duct_ports_Info(Line l_, double width_, List<Port_Info> ports_info_, string duct_size_)
        {
            l = l_;
            width = width_;
            duct_size = duct_size_;
            ports_info = ports_info_;
            start_point = l_.StartPoint;
        }
    }
    public class Endline_seg_Info
    {
        public bool is_in;
        public List<Duct_ports_Info> segs;
        public Endline_seg_Info()
        {
            is_in = false;
            segs = new List<Duct_ports_Info> ();
        }
    }
    public class ThDuctPortsConstructor
    {
        private bool have_main;
        private bool is_recreate;
        private double ui_air_speed;
        public List<Port_Info> ports_position_ptr { set; get; }
        public List<Special_graph_Info> endline_elbow { set; get; }
        public List<Endline_seg_Info> endline_segs { set; get; }
        public ThDuctPortsConstructor(ThDuctPortsAnalysis anay_res, DuctPortsParam in_param)
        {
            ui_air_speed = in_param.air_speed;
            is_recreate = anay_res.is_recreate;
            have_main = anay_res.main_ducts.Count != 0;
            endline_elbow = new List<Special_graph_Info>();
            endline_segs = new List<Endline_seg_Info>();
            Shrink_ducts(anay_res);
            Rearrange_endlines(anay_res.merged_endlines);
        }
        public void Update_side_port(string port_range,
                                     Dictionary<Point3d, Side_Port_Info> side_port_info)
        {
            if (port_range.Contains("侧"))
            {
                foreach (var endline in endline_segs)
                {
                    foreach (var seg in endline.segs)
                    {
                        foreach (var port in seg.ports_info)
                        {
                            var p = ThDuctPortsService.Round_point(port.position, 6);
                            if (side_port_info.ContainsKey(p))
                            {
                                var side_port = side_port_info[p];
                                if (side_port.port_handles.Count == 1)
                                {
                                    port.have_l = side_port.is_left;
                                    port.have_r = !side_port.is_left;
                                }
                            }
                        }
                    }
                }
            }
        }
        private void Rearrange_endlines(List<Merged_endline_Info> merged_endlines)
        {
            foreach (var end_lines_info in merged_endlines)
            {
                int max_duct_idx = end_lines_info.segments.Count - 1;
                string duct_size_info = end_lines_info.in_size_info;
                for (int i = max_duct_idx; i >= 0; --i)
                {
                    bool is_last = (i == 0);
                    bool is_first = (i == max_duct_idx && !have_main);
                    Get_end_line_neighbor(i, end_lines_info.segments, out Endline_Info cur_info, out Endline_Info pre_info, out Endline_Info next_info);
                    endline_segs.Add(Break_duct_by_port(is_last, cur_info, pre_info, next_info, is_first, ref duct_size_info));
                    endline_segs[endline_segs.Count - 1].is_in = (i == max_duct_idx);
                    Record_elbow_info(cur_info, next_info, duct_size_info);
                }
            }
        }
        private void Get_end_line_neighbor(int idx,
                                           List<Endline_Info> segments,
                                           out Endline_Info cur_info,
                                           out Endline_Info pre_info,
                                           out Endline_Info next_info)
        {
            int floor = 0;
            int ceiling = segments.Count - 1;
            pre_info = next_info = null;
            int det_idx = idx + 1;
            if (det_idx > floor && det_idx <= ceiling)
                pre_info = segments[det_idx];
            det_idx = idx - 1;
            if (det_idx >= floor && det_idx < ceiling)
                next_info = segments[det_idx];
            cur_info = segments[idx];
        }

        private Endline_seg_Info Break_duct_by_port(bool is_last,
                                                    Endline_Info cur_info,
                                                    Endline_Info pre_info,
                                                    Endline_Info next_info,
                                                    bool is_first,
                                                    ref string duct_size_info)
        {
            var port_seg = new Endline_seg_Info();
            var proc_line = Get_real_proc_line(cur_info, pre_info, next_info, is_first, ref duct_size_info);
            Get_seg_info(is_last, cur_info.ports.Count, proc_line, out double step, out Queue<Point3d> positions);
            Seperate_line_by_port(step, cur_info, next_info, proc_line, port_seg.segs, is_first, positions, ref duct_size_info);
            return port_seg;
        }
        private static void Get_seg_info(bool is_last, int port_num, Line proc_line, out double step, out Queue<Point3d> positions)
        {
            var srt_p = proc_line.StartPoint;
            var end_p = proc_line.EndPoint;
            var len = srt_p.DistanceTo(end_p);
            step = Math.Ceiling(len / 100) * 100.0 / port_num;
            var dir_vec = (end_p - srt_p).GetNormal();
            positions = new Queue<Point3d>();
            Point3d cur_p = srt_p + dir_vec * (0.5 * step);
            for (int i = 0; i < port_num; ++i)
            {
                positions.Enqueue(cur_p);
                cur_p += step * dir_vec;
            }
            if (is_last)
            {
                var q = new Queue<Point3d>();
                cur_p -= step * dir_vec;
                var end_dis_vec = (end_p - 400 * dir_vec) - cur_p;
                foreach (var p in positions)
                    q.Enqueue(p + end_dis_vec);
                positions.Clear();
                positions = q;
            }
        }
        private void Seperate_line_by_port(double step,
                                           Endline_Info cur_info,
                                           Endline_Info next_info,
                                           Line proc_line,
                                           List<Duct_ports_Info> port_seg,
                                           bool is_first,
                                           Queue<Point3d> positions,
                                           ref string duct_size_info)
        {
            Point3d flag_p = proc_line.StartPoint;
            Point3d cur_p = flag_p;
            var dir_vec = ThDuctPortsService.Get_edge_direction(proc_line);
            double pre_width = Get_start_duct_width(is_first, cur_info, ref duct_size_info);
            string next_duct_size = duct_size_info;
            double reducing_len = (step > 1000) ? 1000 : step;
            var dis_vec = dir_vec * (0.5 * step - reducing_len * 0.5);//变径布置在变径风口中间
            ports_position_ptr = new List<Port_Info>();
            for (int i = cur_info.ports.Count - 1; i > 0; --i)
            {
                cur_p = is_recreate ? cur_info.ports[i].position : positions.Dequeue();
                ports_position_ptr.Add(new Port_Info(cur_info.ports[i].air_volume, cur_p));
                double cur_width = Get_duct_width(false, cur_info.ports[i - 1].air_volume, ref next_duct_size);
                if (duct_size_info != next_duct_size)
                {
                    if (pre_width > cur_width)
                    {
                        if (is_recreate)
                            cur_p = Create_port_fix_step(i, cur_info.ports);
                        else
                            cur_p += dis_vec;
                        Record_duct(flag_p, cur_p, pre_width, duct_size_info, port_seg);
                        Update_status(reducing_len, dir_vec, cur_p, cur_width, next_duct_size, ref flag_p, ref pre_width, ref duct_size_info);
                    }
                    else if (pre_width < cur_width)
                        throw new NotImplementedException();
                }
            }
            if (cur_info.ports.Count == 0)
                Total_direct_duct(flag_p, cur_p, cur_info, pre_width, duct_size_info, proc_line, port_seg);
            else
            {
                cur_p = is_recreate ? cur_info.ports[0].position : positions.Dequeue();
                double next_duct_start_width = (next_info == null) ? 0 : Get_duct_width(false, Get_endline_air_volume(next_info), ref next_duct_size);
                if (next_info != null && duct_size_info != next_duct_size)
                    Endline_elbow_reducing(flag_p, cur_p, cur_info, pre_width, next_duct_start_width, duct_size_info, next_duct_size, proc_line, port_seg);
                else if (port_seg.Count == 0)
                    Total_direct_duct(flag_p, cur_p, cur_info, pre_width, duct_size_info, proc_line, port_seg);
                else
                {
                    var last_seg = port_seg[port_seg.Count - 1];
                    if (last_seg.width > pre_width)
                        End_direct_duct_with_reducing(flag_p, cur_p, cur_info, pre_width, duct_size_info, proc_line, port_seg);
                    else if (next_info == null)
                        Has_reducing_end_with_direct_duct(cur_p, cur_info, last_seg, proc_line);
                }
            }
        }

        private Point3d Create_port_fix_step(int idx, List<Port_Info> ports)
        {
            var cur_p = ports[idx].position;
            var next_p = ports[idx - 1].position;
            var dir_vec = (next_p - cur_p).GetNormal();
            var mid_p = ThDuctPortsService.Get_mid_point(cur_p, next_p);
            var dis = cur_p.DistanceTo(next_p);
            dis = dis > 1000 ? 1000 : dis;
            return mid_p - dir_vec * dis * 0.5;
        }

        private double Get_start_duct_width(bool is_first, 
                                            Endline_Info cur_info,
                                            ref string cur_duct_size)
        {
            double air_volume = Get_endline_air_volume(cur_info);
            return Get_duct_width(is_first, air_volume, ref cur_duct_size);
        }
        private void Record_duct(Point3d flag_p, Point3d cur_p, double width, string duct_size, List<Duct_ports_Info> port_seg)
        {
            Line l = new Line(flag_p, cur_p);
            if (l.Length > 0)
                port_seg.Add(new Duct_ports_Info(l, width, ports_position_ptr, duct_size));
        }
        private void Update_status(double reducing_len, 
                                   Vector3d dir_vec, 
                                   Point3d cur_p, 
                                   double cur_width, 
                                   string next_duct_size,
                                   ref Point3d flag_p, 
                                   ref double pre_width,
                                   ref string cur_duct_size)
        {
            ports_position_ptr = new List<Port_Info>();
            flag_p = cur_p + (dir_vec * reducing_len); // reducing endpoint
            pre_width = cur_width;
            cur_duct_size = next_duct_size;
        }
        private void Endline_elbow_reducing(Point3d flag_p,
                                            Point3d cur_p,
                                            Endline_Info cur_info,
                                            double cur_width,
                                            double next_duct_start_width,
                                            string cur_duct_size,
                                            string next_duct_size,
                                            Line proc_line,
                                            List<Duct_ports_Info> port_seg)
        {
            // 变径+弯头
            double last_air_volume = (cur_info.ports.Count == 0) ? 0 : cur_info.ports[0].air_volume;
            Vector3d dir_vec = Get_edge_direction(cur_info.direct_edge);
            double dis = proc_line.EndPoint.DistanceTo(cur_p);
            double reducing_len = (dis > 1000) ? 1000 : dis;
            Line l = new Line(flag_p, proc_line.EndPoint - (reducing_len + 1) * dir_vec);
            ports_position_ptr.Add(new Port_Info(last_air_volume, cur_p));
            port_seg.Add(new Duct_ports_Info(l, cur_width, ports_position_ptr, cur_duct_size));
            Line reducing = new Line(proc_line.EndPoint - dir_vec, proc_line.EndPoint);
            ports_position_ptr = new List<Port_Info>();
            port_seg.Add(new Duct_ports_Info(reducing, next_duct_start_width, ports_position_ptr, next_duct_size));
        }
        private void Total_direct_duct( Point3d flag_p,
                                        Point3d cur_p,
                                        Endline_Info cur_info,
                                        double cur_width,
                                        string cur_duct_size,
                                        Line proc_line,
                                        List<Duct_ports_Info> port_seg)
        {
            // 完全直管段
            double last_air_volume = Get_endline_air_volume(cur_info);
            int port_num = cur_info.ports.Count;
            Line l = new Line(flag_p, proc_line.EndPoint);
            if (port_num == 0)
                ports_position_ptr = new List<Port_Info>();
            else
                ports_position_ptr.Add(new Port_Info(last_air_volume, cur_p));
            port_seg.Add(new Duct_ports_Info(l, cur_width, ports_position_ptr, cur_duct_size));
        }
        private void Has_reducing_end_with_direct_duct(Point3d cur_p,
                                                       Endline_Info cur_info,
                                                       Duct_ports_Info last_seg,
                                                       Line proc_line)
        {
            // 有过变径，最后是直管段
            double last_air_volume = (cur_info.ports.Count == 0) ? 0 : cur_info.ports[0].air_volume;
            last_seg.ports_info.Add(new Port_Info(last_air_volume, cur_p));
            last_seg.l = new Line(last_seg.l.StartPoint, proc_line.EndPoint);
        }
        private void End_direct_duct_with_reducing( Point3d flag_p,
                                                    Point3d cur_p,
                                                    Endline_Info cur_info,
                                                    double cur_width,
                                                    string cur_duct_size,
                                                    Line proc_line,
                                                    List<Duct_ports_Info> port_seg)
        {
            // 直管段最后一段带变径
            double last_air_volume = (cur_info.ports.Count == 0) ? 0 : cur_info.ports[0].air_volume;
            ports_position_ptr.Add(new Port_Info(last_air_volume, cur_p));
            Line reducing = new Line(flag_p, proc_line.EndPoint);
            port_seg.Add(new Duct_ports_Info(reducing, cur_width, ports_position_ptr, cur_duct_size));
        }
        private Line Get_real_proc_line(Endline_Info cur_info,
                                        Endline_Info pre_info,
                                        Endline_Info next_info,
                                        bool is_first,
                                        ref string duct_size_info)
        {
            Get_pre_and_next_duct_angle(cur_info, pre_info, next_info, out double pre_elbow_open_angle, out double next_elbow_open_angle);
            double next_duct_start_width = 0;
            if (next_info != null)
            {
                string end_size = Get_end_width(is_first, cur_info, duct_size_info);
                double air_volume = Get_endline_air_volume(next_info);
                next_duct_start_width = Get_duct_width(false, air_volume, ref end_size);
            }
            double cur_air_volume = Get_endline_air_volume(cur_info);
            double cur_duct_width = Get_duct_width(is_first, cur_air_volume, ref duct_size_info);
            double src_shrink = (Math.Abs(pre_elbow_open_angle) < 1e-3) ? cur_info.direct_edge.SourceShrink :
                                 ThDuctPortsShapeService.Get_elbow_shrink(pre_elbow_open_angle, cur_duct_width, 0, 0.7);
            double dst_shrink = (Math.Abs(next_elbow_open_angle) < 1e-3) ? cur_info.direct_edge.TargetShrink :
                                 ThDuctPortsShapeService.Get_elbow_shrink(next_elbow_open_angle, next_duct_start_width, 0, 0.7);
            return Get_shrink_line(cur_info, src_shrink, dst_shrink);
        }
        private Line Get_shrink_line(Endline_Info cur_info, double src_shrink, double dst_shrink)
        {
            Point3d srt_p = cur_info.direct_edge.Source.Position;
            Point3d end_p = cur_info.direct_edge.Target.Position;
            Vector3d dir_vec = (end_p - srt_p).GetNormal();
            Point3d proc_line_srt_p = srt_p + dir_vec * src_shrink;
            Point3d proc_line_end_p = end_p - dir_vec * dst_shrink;
            return new Line(proc_line_srt_p, proc_line_end_p);
        }
        private string Get_end_width(bool is_first, Endline_Info cur_info, string duct_size_info)
        {
            string duct_size = duct_size_info;
            foreach (var port_info in cur_info.ports)
            {
                Get_duct_width(is_first, port_info.air_volume, ref duct_size);
                is_first = false;
            }
            return duct_size;
        }
        private void Get_pre_and_next_duct_angle(Endline_Info cur_info,
                                                 Endline_Info pre_info,
                                                 Endline_Info next_info,
                                                 out double pre_elbow_open_angle,
                                                 out double next_elbow_open_angle)
        {
            pre_elbow_open_angle = next_elbow_open_angle = 0;
            Vector3d dir_vec = Get_edge_direction(cur_info.direct_edge);
            if (pre_info != null)
            {
                Vector3d pre_dir_vec = -Get_edge_direction(pre_info.direct_edge);
                pre_elbow_open_angle = pre_dir_vec.GetAngleTo(dir_vec);
            }
            if (next_info != null)
            {
                Vector3d next_dir_vec = Get_edge_direction(next_info.direct_edge);
                next_elbow_open_angle = next_dir_vec.GetAngleTo(-dir_vec);
            }
        }
        private void Record_elbow_info(Endline_Info cur_info,
                                       Endline_Info next_info,
                                       string duct_size)
        {
            if (next_info != null)
            {
                double next_air_volume = Get_endline_air_volume(next_info);
                double cur_air_volume = cur_info.ports.Count > 0 ? cur_info.ports[0].air_volume : next_air_volume;
                Line l1 = new Line(cur_info.direct_edge.Target.Position, cur_info.direct_edge.Source.Position);
                Line l2 = new Line(next_info.direct_edge.Source.Position, next_info.direct_edge.Target.Position);
                List<Line> lines = new List<Line> { l1, l2 };
                double cur_width = ThDuctPortsService.Calc_duct_width(false, ui_air_speed, cur_air_volume, ref duct_size);
                double next_width = ThDuctPortsService.Calc_duct_width(false, ui_air_speed, next_air_volume, ref duct_size);
                List<double> every_port_width = new List<double> { cur_width, next_width };
                endline_elbow.Add(new Special_graph_Info(lines, every_port_width));
            }
        }
        private void Shrink_ducts(ThDuctPortsAnalysis anay_res)
        {
            foreach (var shape_info in anay_res.special_shapes_info)
            {
                switch (shape_info.every_port_width.Count)
                {
                    case 2: Shrink_elbow_duct(anay_res, shape_info); break;
                    case 3: Shrink_tee_duct(anay_res, shape_info); break;
                    case 4: Shrink_cross_duct(anay_res, shape_info); break;
                    default: throw new NotImplementedException();
                }
            }
        }

        private void Shrink_cross_duct(ThDuctPortsAnalysis anay_res, Special_graph_Info info)
        {
            Seperate_cross_outter(info, out int o_outter_idx, out int o_inner_idx, out int o_collinear_idx);
            Get_cross_port_shrink(info, o_outter_idx, o_inner_idx,
                                  out double in_shrink, out double o_inner_shrink, out double o_outter_shrink, out double o_collinear_shrink);
            Duct_tar_shrink(anay_res, in_shrink, info.lines[0]);
            Duct_src_shrink(anay_res, o_inner_shrink, info, o_inner_idx);
            Duct_src_shrink(anay_res, o_outter_shrink, info, o_outter_idx);
            Duct_src_shrink(anay_res, o_collinear_shrink, info, o_collinear_idx);
        }

        private void Get_cross_port_shrink(Special_graph_Info info,
                                           int o_outter_idx, int o_inner_idx,
                                           out double in_shrink, out double o_inner_shrink, out double o_outter_shrink, out double o_collinear_shrink)
        {
            double in_width = info.every_port_width[0];
            double o_inner_width = info.every_port_width[o_inner_idx];
            double o_outter_width = info.every_port_width[o_outter_idx];
            double small_width = o_inner_width > o_outter_width ? o_inner_width : o_outter_width;
            in_shrink = small_width + 50;
            o_collinear_shrink = small_width * 0.5 + 100;
            o_inner_shrink = (in_width + o_inner_width) * 0.5 + 50;
            o_outter_shrink = (in_width + o_outter_width) * 0.5 + 50;
        }

        private void Seperate_cross_outter(Special_graph_Info info, out int o_outter_idx, out int o_inner_idx, out int o_collinear_idx)
        {
            Line in_line = info.lines[0];
            o_outter_idx = o_inner_idx = o_collinear_idx = 0;
            Vector3d in_line_vec = ThDuctPortsService.Get_edge_direction(in_line);
            for (int i = 1; i < info.lines.Count; ++i)
            {
                Line outter = info.lines[i];
                Vector3d out_line_vec = ThDuctPortsService.Get_edge_direction(outter);
                if (ThDuctPortsService.Is_vertical(in_line_vec, out_line_vec))
                {
                    if (in_line_vec.CrossProduct(out_line_vec).Z > 0)
                        o_outter_idx = i;
                    else
                        o_inner_idx = i;
                }
                else
                    o_collinear_idx = i;
            }
        }

        private void Shrink_tee_duct(ThDuctPortsAnalysis anay_res, Special_graph_Info info)
        {
            Tee_Type type = Get_tee_type(info.lines[1], info.lines[2]);
            Seperate_tee_outter(info, type, out int branch_idx, out int other_idx);
            Get_tee_port_shrink(info, type, branch_idx, other_idx,
                                out double in_shrink, out double branch_shrink, out double other_shrink);

            Duct_tar_shrink(anay_res, in_shrink, info.lines[0]);
            Duct_src_shrink(anay_res, branch_shrink, info, branch_idx);
            Duct_src_shrink(anay_res, other_shrink, info, other_idx);
        }

        private void Get_tee_port_shrink(Special_graph_Info info, Tee_Type type, int branch_idx, int other_idx,
                                         out double in_shrink, out double branch_shrink, out double other_shrink)
        {
            double in_width = info.every_port_width[0];
            double branch_width = info.every_port_width[branch_idx];
            double other_width = info.every_port_width[other_idx];

            if (type == Tee_Type.BRANCH_VERTICAL_WITH_OTTER)
            {
                in_shrink = branch_width + 50;
                other_shrink = branch_width * 0.5 + 100;
                branch_shrink = (in_width + branch_width) * 0.5 + 50;
            }
            else
            {
                double max_branch = (branch_width > other_width) ? branch_width : other_width;
                in_shrink = max_branch + 50;
                other_shrink = (in_width - other_width) * 0.5 + other_width + 50;
                branch_shrink = (in_width - branch_width) * 0.5 + branch_width + 50;
            }
        }

        private void Seperate_tee_outter(Special_graph_Info info, Tee_Type type, out int branch_idx, out int other_idx)
        {
            Line i_line = info.lines[0];
            Line o1_line = info.lines[1];
            Vector3d o1_vec = ThDuctPortsService.Get_edge_direction(o1_line);
            Vector3d in_vec = ThDuctPortsService.Get_edge_direction(i_line);
            if (type == Tee_Type.BRANCH_VERTICAL_WITH_OTTER)
            {
                if (ThDuctPortsService.Is_vertical(o1_vec, in_vec))
                {
                    branch_idx = 1; other_idx = 2;
                }
                else
                {
                    branch_idx = 2; other_idx = 1;
                }
            }
            else
            {
                if (Math.Abs(in_vec.CrossProduct(o1_vec).Z) > 0)
                {
                    branch_idx = 1; other_idx = 2;
                }
                else
                {
                    branch_idx = 2; other_idx = 1;
                }
            }
        }

        private Tee_Type Get_tee_type(Line outter1, Line outter2)
        {
            Vector3d v1 = ThDuctPortsService.Get_edge_direction(outter1);
            Vector3d v2 = ThDuctPortsService.Get_edge_direction(outter2);
            if (ThDuctPortsService.Is_vertical(v1, v2))
                return Tee_Type.BRANCH_VERTICAL_WITH_OTTER;
            else
                return Tee_Type.BRANCH_COLLINEAR_WITH_OTTER;
        }

        private void Shrink_elbow_duct(ThDuctPortsAnalysis anay_res, Special_graph_Info info)
        {
            double in_width = info.every_port_width[0];
            double out_width = info.every_port_width[1];
            double elbow_width = in_width < out_width ? in_width : out_width;
            double reducing_width = in_width > out_width ? in_width : out_width;
            double reducing_len = 0.5 * (reducing_width - elbow_width) / Math.Tan(20 * Math.PI / 180);
            double open_angle = Get_elbow_open_angle(info);
            if (Math.Abs(in_width - out_width) < 1e-3)
            {
                double shrink_len = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, elbow_width, 0, info.K);
                Duct_tar_shrink(anay_res, shrink_len, info.lines[0]);
                Duct_src_shrink(anay_res, shrink_len, info, 1);
            }
            else
            {
                if (in_width > out_width)
                {
                    double shrink_len = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, elbow_width, reducing_len, info.K);
                    Duct_tar_shrink(anay_res, shrink_len, info.lines[0]);
                    shrink_len = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, elbow_width, 0, info.K);
                    Duct_src_shrink(anay_res, shrink_len, info, 1);
                }
                else
                {
                    double shrink_len = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, elbow_width, 0, info.K);
                    Duct_tar_shrink(anay_res, shrink_len, info.lines[0]);
                    shrink_len = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, elbow_width, reducing_len, info.K);
                    Duct_src_shrink(anay_res, shrink_len, info, 1);
                }
            }
        }
        private double Get_elbow_open_angle(Special_graph_Info info)
        {
            Line l1 = info.lines[0];
            Line l2 = info.lines[1];
            Vector2d v1 = ThDuctPortsService.Get_2D_edge_direction(l1);
            Vector2d v2 = ThDuctPortsService.Get_2D_edge_direction(l2);
            return v1.GetAngleTo(v2);
        }
        private void Duct_tar_shrink(ThDuctPortsAnalysis anay_res, double width, Line current_line)
        {
            var main_duct_idx = anay_res.Search_main_duct_idx(current_line);
            if (main_duct_idx != -1)
                anay_res.main_ducts[main_duct_idx].TargetShrink = width;
            var endline_idx = anay_res.Search_endline_idx(current_line);
            if (endline_idx != null)
                anay_res.merged_endlines[endline_idx.i].segments[endline_idx.j].direct_edge.TargetShrink = width;
        }
        private void Duct_src_shrink(ThDuctPortsAnalysis anay_res, double width, Special_graph_Info info, int idx)
        {
            if (idx == 0)
                return;
            Line current_line = info.lines[idx];
            var main_duct_idx = anay_res.Search_main_duct_idx(current_line);
            if (main_duct_idx != -1)
                anay_res.main_ducts[main_duct_idx].SourceShrink = width;
            var endline_idx = anay_res.Search_endline_idx(current_line);
            if (endline_idx != null)
                anay_res.merged_endlines[endline_idx.i].segments[endline_idx.j].direct_edge.SourceShrink = width;
        }
        private Vector3d Get_edge_direction(ThDuctEdge<ThDuctVertex> direct_edge)
        {
            Point3d srt_p = direct_edge.Source.Position;
            Point3d end_p = direct_edge.Target.Position;
            return (end_p - srt_p).GetNormal();
        }
        private double Get_endline_air_volume(Endline_Info info)
        {
            if (info.ports.Count == 0)
                return info.direct_edge.AirVolume;
            else
                return info.ports[info.ports.Count - 1].air_volume;
        }
        private double Get_duct_width(bool is_first, double air_volume, ref string duct_size)
        {
            return ThDuctPortsService.Calc_duct_width(is_first, ui_air_speed, air_volume, ref duct_size);
        }
    }
}