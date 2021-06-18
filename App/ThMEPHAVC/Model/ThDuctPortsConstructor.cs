using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class Duct_ports_Info
    {
        public Line l;
        public double width;
        public string duct_size;
        public Point3d start_point;
        public List<Point3d> ports_position;
        public Duct_ports_Info(Line l_, double width_, List<Point3d> ports_position_, string duct_size_)
        {
            l = l_;
            width = width_;
            duct_size = duct_size_;
            start_point = l_.StartPoint;
            ports_position = ports_position_;
        }
    }
    public class ThDuctPortsConstructor
    {
        private bool have_main;
        private double air_speed;
        private string ui_duct_size;
        private readonly string scenario;
        public List<Point3d> ports_position_ptr;
        public List<Special_graph_Info> endline_elbow;
        public List<List<Duct_ports_Info>> endline_segs;
        public ThDuctPortsConstructor(ThDuctPortsAnalysis anay_res, ThDuctPortsParam in_param)
        {
            have_main = anay_res.main_ducts.Count != 0;
            scenario = in_param.scenario;
            air_speed = in_param.air_speed;
            ui_duct_size = in_param.in_duct_size;
            endline_elbow = new List<Special_graph_Info>();
            endline_segs = new List<List<Duct_ports_Info>>();
            Shrink_ducts(anay_res);
            Rearrange_endlines(anay_res.merged_endlines);
        }
        private void Rearrange_endlines(List<Merged_endline_Info> merged_endlines)
        {
            foreach (var end_lines_info in merged_endlines)
            {
                int max_duct_idx = end_lines_info.segments.Count - 1;
                double duct_width_limit = end_lines_info.in_port_width;
                for (int i = max_duct_idx; i >= 0; --i)
                {
                    bool is_first = i == max_duct_idx;
                    Get_end_line_neighbor(i, end_lines_info.segments, out Endline_Info cur_info, out Endline_Info pre_info, out Endline_Info next_info);
                    endline_segs.Add(Break_duct_by_port(cur_info, pre_info, next_info, is_first, ref duct_width_limit));
                    Record_elbow_info(cur_info, next_info, duct_width_limit, is_first);
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

        private List<Duct_ports_Info> Break_duct_by_port(Endline_Info cur_info,
                                                         Endline_Info pre_info,
                                                         Endline_Info next_info,
                                                         bool is_first,
                                                         ref double duct_width_limit)
        {
            var port_seg = new List<Duct_ports_Info>();
            Line proc_line = Get_real_proc_line(cur_info, pre_info, next_info, duct_width_limit, is_first);
            Seperate_line_by_port(ref duct_width_limit, cur_info, next_info, proc_line, port_seg, is_first);
            return port_seg;
        }

        private void Seperate_line_by_port(ref double duct_width_limit,
                                           Endline_Info cur_info,
                                           Endline_Info next_info,
                                           Line proc_line,
                                           List<Duct_ports_Info> port_seg,
                                           bool is_first)
        {
            Point3d flag_p = proc_line.StartPoint;
            Point3d cur_p = flag_p;
            Vector3d dir_vec = Get_edge_direction(cur_info.direct_edge);
            string cur_duct_size = ui_duct_size;
            double port_step = proc_line.Length / (cur_info.ports.Count + 1);
            double pre_width = Get_start_duct_width(duct_width_limit, cur_info, is_first, ref cur_duct_size);
            string next_duct_size = cur_duct_size;            
            ports_position_ptr = new List<Point3d>();
            for (int i = cur_info.ports.Count - 1; i > 0; --i)
            {
                cur_p += dir_vec * port_step;
                ports_position_ptr.Add(cur_p);
                double cur_width = ThDuctPortsService.Calc_directed_duct_width(cur_info.ports[i].air_vloume, cur_info.ports[i - 1].air_vloume, pre_width, ref next_duct_size);
                if (cur_duct_size != next_duct_size)
                {
                    if (pre_width > cur_width)
                    {
                        Record_reducing(flag_p, cur_p, pre_width, cur_duct_size, port_seg);
                        Update_status(port_step, dir_vec, cur_p, cur_width, next_duct_size, ref flag_p, ref pre_width, ref cur_duct_size);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            double next_duct_start_width = (next_info == null) ? 0 : ThDuctPortsService.Calc_duct_width(Get_endline_air_volume(next_info), pre_width, 0, scenario, ref next_duct_size);
            if (Math.Abs(next_duct_start_width) > 1e-3 && next_duct_start_width < pre_width)
                Endline_elbow_reducing(flag_p, cur_p, dir_vec, port_step, pre_width, next_duct_start_width, cur_duct_size, next_duct_size, proc_line, port_seg);
            else if (port_seg.Count == 0)
                Total_direct_duct(flag_p, cur_p, dir_vec, port_step, pre_width, cur_duct_size, proc_line, port_seg, cur_info.ports.Count);
            else
            {
                var last_seg = port_seg[port_seg.Count - 1];
                if (last_seg.width > pre_width)
                    End_direct_duct_with_reducing(flag_p, cur_p, dir_vec, port_step, pre_width, cur_duct_size, proc_line, port_seg);
                else if (next_info == null)
                    Has_reducing_end_with_direct_duct(cur_p, dir_vec, port_step, last_seg, proc_line);
            }
            duct_width_limit = next_duct_start_width;
        }
        private double Get_start_duct_width(double duct_width_limit,
                                            Endline_Info cur_info,
                                            bool is_first,
                                            ref string cur_duct_size)
        {
            double speed = (is_first && !have_main) ? air_speed : 0;
            return (is_first && !have_main) ? duct_width_limit : ThDuctPortsService.Calc_duct_width(Get_endline_air_volume(cur_info), duct_width_limit, speed, scenario, ref cur_duct_size);
        }
        private void Record_reducing(Point3d flag_p, Point3d cur_p, double width, string duct_size, List<Duct_ports_Info> port_seg)
        {
            Line l = new Line(flag_p, cur_p);
            port_seg.Add(new Duct_ports_Info(l, width, ports_position_ptr, duct_size));
        }
        private void Update_status(double port_step, 
                                   Vector3d dir_vec, 
                                   Point3d cur_p, 
                                   double cur_width, 
                                   string next_duct_size,
                                   ref Point3d flag_p, 
                                   ref double pre_width,
                                   ref string cur_duct_size)
        {
            double reducing_len = (port_step > 1000) ? 1000 : port_step;
            ports_position_ptr = new List<Point3d>();
            flag_p = cur_p + (dir_vec * reducing_len); // reducing endpoint
            pre_width = cur_width;
            cur_duct_size = next_duct_size;
        }
        private void Endline_elbow_reducing(Point3d flag_p,
                                            Point3d cur_p,
                                            Vector3d dir_vec,
                                            double port_step,
                                            double cur_width,
                                            double next_duct_start_width,
                                            string cur_duct_size,
                                            string next_duct_size,
                                            Line proc_line,
                                            List<Duct_ports_Info> port_seg)
        {
            // 变径+弯头
            cur_p += dir_vec * port_step;
            double dis = proc_line.EndPoint.DistanceTo(cur_p);
            double reducing_len = (dis > 1000) ? 1000 : dis;
            Line l = new Line(flag_p, cur_p);
            ports_position_ptr.Add(cur_p);
            port_seg.Add(new Duct_ports_Info(l, cur_width, ports_position_ptr, cur_duct_size));
            Line reducing = (dis > 1000) ? new Line(cur_p + (dir_vec * reducing_len), proc_line.EndPoint)
                                         : new Line(proc_line.EndPoint - dir_vec, proc_line.EndPoint);
            ports_position_ptr = new List<Point3d>();
            port_seg.Add(new Duct_ports_Info(reducing, next_duct_start_width, ports_position_ptr, next_duct_size));
        }
        private void Total_direct_duct( Point3d flag_p,
                                        Point3d cur_p,
                                        Vector3d dir_vec,
                                        double port_step,
                                        double cur_width,
                                        string cur_duct_size,
                                        Line proc_line,
                                        List<Duct_ports_Info> port_seg,
                                        int port_num)
        {
            // 完全直管段
            Line l = new Line(flag_p, proc_line.EndPoint);
            if (port_num == 0)
                ports_position_ptr = new List<Point3d>();
            else
                ports_position_ptr.Add(cur_p + dir_vec * port_step);
            port_seg.Add(new Duct_ports_Info(l, cur_width, ports_position_ptr, cur_duct_size));
        }
        private void Has_reducing_end_with_direct_duct(Point3d cur_p,
                                                       Vector3d dir_vec,
                                                       double port_step,
                                                       Duct_ports_Info last_seg,
                                                       Line proc_line)
        {
            // 有过变径，最后是直管段
            last_seg.ports_position.Add(cur_p + dir_vec * port_step);
            last_seg.l = new Line(last_seg.l.StartPoint, proc_line.EndPoint);
        }
        private void End_direct_duct_with_reducing(Point3d flag_p,
                                                    Point3d cur_p,
                                                    Vector3d dir_vec,
                                                    double port_step,
                                                    double cur_width,
                                                    string cur_duct_size,
                                                    Line proc_line,
                                                    List<Duct_ports_Info> port_seg)
        {
            // 直管段最后一段带变径
            cur_p += dir_vec * port_step;
            ports_position_ptr.Add(cur_p);
            Line reducing = new Line(flag_p, proc_line.EndPoint);
            port_seg.Add(new Duct_ports_Info(reducing, cur_width, ports_position_ptr, cur_duct_size));
        }
        private Line Get_real_proc_line(Endline_Info cur_info,
                                        Endline_Info pre_info,
                                        Endline_Info next_info,
                                        double duct_width_limit,
                                        bool is_first)
        {
            Get_pre_and_next_duct_angle(cur_info, pre_info, next_info, out double pre_elbow_open_angle, out double next_elbow_open_angle);
            string duct_size = String.Empty;
            double speed = (is_first && !have_main) ? air_speed : 0;
            double end_endline_width = Get_end_width(cur_info, duct_width_limit, speed);
            double next_duct_start_width = 0;
            if (next_info != null)
            {
                double air_volumn = Get_endline_air_volume(next_info);
                next_duct_start_width = ThDuctPortsService.Calc_duct_width(air_volumn, end_endline_width, 0, scenario, ref duct_size);
            }

            double cur_duct_width = ThDuctPortsService.Calc_duct_width(Get_endline_air_volume(cur_info), duct_width_limit, speed, scenario, ref duct_size);
            double src_shrink = (Math.Abs(pre_elbow_open_angle) < 1e-3) ?
                                 cur_info.direct_edge.SourceShrink :
                                 Get_elbow_shrink(pre_elbow_open_angle, cur_duct_width, 0, 0.7);
            double dst_shrink = (Math.Abs(next_elbow_open_angle) < 1e-3) ?
                                 cur_info.direct_edge.TargetShrink :
                                 Get_elbow_shrink(next_elbow_open_angle, next_duct_start_width, 0, 0.7);
            Point3d srt_p = cur_info.direct_edge.Source.Position;
            Point3d end_p = cur_info.direct_edge.Target.Position;
            Vector3d dir_vec = (end_p - srt_p).GetNormal();
            Point3d proc_line_srt_p = srt_p + dir_vec * src_shrink;
            Point3d proc_line_end_p = end_p - dir_vec * dst_shrink;
            return new Line(proc_line_srt_p, proc_line_end_p);
        }
        private double Get_end_width(Endline_Info cur_info, double duct_width_limit, double air_speed)
        {
            string duct_size = String.Empty;
            double width = duct_width_limit;
            foreach (var port_info in cur_info.ports)
                width = ThDuctPortsService.Calc_duct_width(Get_endline_air_volume(cur_info), width, air_speed, scenario, ref duct_size);
            return width;
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
                                       double duct_limit,
                                       bool is_first)
        {
            if (next_info != null)
            {
                double speed = (is_first && !have_main) ? air_speed : 0;
                string duct_size = String.Empty;
                Line l1 = new Line(cur_info.direct_edge.Target.Position, cur_info.direct_edge.Source.Position);
                Line l2 = new Line(next_info.direct_edge.Source.Position, next_info.direct_edge.Target.Position);
                List<Line> lines = new List<Line> { l1, l2 };
                double next_width = ThDuctPortsService.Calc_duct_width(Get_endline_air_volume(next_info), duct_limit, speed, scenario, ref duct_size);
                List<double> every_port_width = new List<double> { duct_limit, next_width };
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
            in_shrink = o_outter_width + 50;
            o_collinear_shrink = small_width * 0.5 + 100;
            o_inner_shrink = (in_width + o_inner_width) * 0.5 + 50;
            o_outter_shrink = (in_width + o_outter_width) * 0.5 + 50;
        }

        private void Seperate_cross_outter(Special_graph_Info info, out int o_outter_idx, out int o_inner_idx, out int o_collinear_idx)
        {
            Line in_line = info.lines[0];
            o_outter_idx = o_inner_idx = o_collinear_idx = 0;
            Vector3d in_line_vec = (in_line.EndPoint - in_line.StartPoint).GetNormal();
            for (int i = 1; i < info.lines.Count; ++i)
            {
                Line outter = info.lines[i];
                Vector3d out_line_vec = (outter.EndPoint - outter.StartPoint).GetNormal();
                if (Is_vertical(in_line_vec, out_line_vec))
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
            Vector3d o1_vec = (o1_line.EndPoint - o1_line.StartPoint).GetNormal();
            Vector3d in_vec = (i_line.EndPoint - i_line.StartPoint).GetNormal();
            if (type == Tee_Type.BRANCH_VERTICAL_WITH_OTTER)
            {
                if (Is_vertical(o1_vec, in_vec))
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
            Vector3d v1 = (outter1.EndPoint - outter1.StartPoint).GetNormal();
            Vector3d v2 = (outter2.EndPoint - outter2.StartPoint).GetNormal();
            if (Is_vertical(v1, v2))
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
                double shrink_len = Get_elbow_shrink(open_angle, elbow_width, 0, info.K);
                Duct_tar_shrink(anay_res, shrink_len, info.lines[0]);
                Duct_src_shrink(anay_res, shrink_len, info, 1);
            }
            else
            {
                if (in_width > out_width)
                {
                    double shrink_len = Get_elbow_shrink(open_angle, elbow_width, reducing_len, info.K);
                    Duct_tar_shrink(anay_res, shrink_len, info.lines[0]);
                    shrink_len = Get_elbow_shrink(open_angle, elbow_width, 0, info.K);
                    Duct_src_shrink(anay_res, shrink_len, info, 1);
                }
                else
                {
                    double shrink_len = Get_elbow_shrink(open_angle, elbow_width, 0, info.K);
                    Duct_tar_shrink(anay_res, shrink_len, info.lines[0]);
                    shrink_len = Get_elbow_shrink(open_angle, elbow_width, reducing_len, info.K);
                    Duct_src_shrink(anay_res, shrink_len, info, 1);
                }
            }
        }
        private double Get_elbow_shrink(double open_angle, double width, double reducing_len, double K)
        {
            if (open_angle > 0.5 * Math.PI)
            {
                Point2d center_point = new Point2d(-0.7 * width, -Math.Abs(0.7 * width * Math.Tan(0.5 * (Math.PI - open_angle))));
                return Math.Abs(center_point.Y) + reducing_len + 50;
            }
            else if (Math.Abs(open_angle - 0.5 * Math.PI) < 1e-3)
                return K * (width + reducing_len) + 50;
            else if (open_angle > 0 && open_angle < 0.5 * Math.PI)
                throw new NotImplementedException();
            else
                return 0;
        }
        private double Get_elbow_open_angle(Special_graph_Info info)
        {
            Line l1 = info.lines[0];
            Line l2 = info.lines[1];
            Vector2d v1 = (l1.EndPoint.ToPoint2D() - l1.StartPoint.ToPoint2D()).GetNormal();
            Vector2d v2 = (l2.EndPoint.ToPoint2D() - l2.StartPoint.ToPoint2D()).GetNormal();
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
        private bool Is_vertical(Vector3d v1, Vector3d v2)
        {
            return Math.Abs(v1.DotProduct(v2)) < 1e-1 ? true : false;
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
                return info.ports[info.ports.Count - 1].air_vloume;
        }
    }
}