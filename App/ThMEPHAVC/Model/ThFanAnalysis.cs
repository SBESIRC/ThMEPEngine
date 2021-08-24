using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;    
using ThCADCore.NTS;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.CAD
{
    class LineCompare : IEqualityComparer<Line>
    {
        public Tolerance tor;
        public LineCompare(Tolerance tor)
        {
            this.tor = tor;
        }
        public bool Equals(Line x, Line y)
        {
            return (x.StartPoint.IsEqualTo(y.StartPoint, tor) && x.EndPoint.IsEqualTo(y.EndPoint, tor)) ||
                   (x.StartPoint.IsEqualTo(y.EndPoint, tor) && x.EndPoint.IsEqualTo(y.StartPoint, tor));
        }
        public int GetHashCode(Line obj)
        {
            return 0;
        }
    }
    public class TextAlignLine
    {
        public Line l;
        public bool is_in;
        public string duct_size;
        public TextAlignLine(Line l, bool is_in, string duct_size)
        {
            this.l = l;
            this.is_in = is_in;
            this.duct_size = duct_size;
        }
    }
    public class ThFanAnalysis
    {
        public ThDbModelFan fan;
        public Duct_InParam param;
        public Tolerance point_tor;
        public Point3d move_srt_p;
        public Point3d fan_break_p;
        public HashSet<Line> in_lines;
        public HashSet<Line> out_lines;
        public List<TextAlignLine> text_alignment;
        public DBObjectCollection bypass;
        public List<Fan_duct_Info> center_lines;    // 不直接用Line是因为src和dst的shrink是不同时间获得的
        public DBObjectCollection out_center_line;
        public List<Line_Info> reducings;
        public List<Special_graph_Info> special_shapes_info;
        public ThVTee vt;
        public Point3d in_vt_pos;
        public Point3d out_vt_pos;
        private Line last_line;
        private ThCADCoreNTSSpatialIndex spatial_index;
        public ThFanAnalysis(ThDbModelFan fan, 
                             Duct_InParam param,
                             DBObjectCollection bypass,
                             DBObjectCollection center_line, 
                             DBObjectCollection wall_lines)
        {
            Init(fan, bypass, param);
            Move_to_zero(fan.FanInletBasePoint, fan.FanOutletBasePoint, center_line, wall_lines, out Point3d inlet_p, out Point3d outlet_p);
            Update_search_point(inlet_p, outlet_p, param, ref center_line, out Point3d in_search_point, out Point3d out_search_point, 
                                                         out Line in_start_line, out Line out_start_line);
            spatial_index = new ThCADCoreNTSSpatialIndex(center_line);
            Get_duct_info(in_search_point, in_start_line, in_lines);
            text_alignment.Add(new TextAlignLine(last_line, true, param.in_duct_size));
            Get_duct_info(out_search_point, out_start_line, out_lines);
            text_alignment.Add(new TextAlignLine(last_line, false, param.out_duct_size));
            Cut_center_line(fan.scenario, wall_lines);
            Re_construct_center_line(ref in_start_line, ref out_start_line);
            Get_duct_info(in_search_point, in_start_line, in_lines);
            Get_duct_info(out_search_point, out_start_line, out_lines);
            Get_special_shape_info(in_search_point, in_start_line, in_lines, param.in_duct_size);
            Get_special_shape_info(out_search_point, out_start_line, out_lines, param.out_duct_size);
            Collect_lines();
            Shrink_duct();
            Get_vt_elbow_pos(in_search_point, out_search_point);
            if (bypass.Count == 0 && param.bypass_size != null)
                vt = new ThVTee(in_vt_pos, out_vt_pos, param.bypass_size);
            Move_to_org();
        }
        private void Init(ThDbModelFan fan, DBObjectCollection bypass, Duct_InParam param)
        {
            this.fan = fan;
            this.param = param;
            this.bypass = bypass;
            move_srt_p = fan.FanInletBasePoint;
            point_tor = new Tolerance(1.5, 1.5);
            var comp = new LineCompare(point_tor);
            in_lines = new HashSet<Line>(comp);
            out_lines = new HashSet<Line>(comp);
            text_alignment = new List<TextAlignLine>();
            reducings = new List<Line_Info>();
            center_lines = new List<Fan_duct_Info>();
            out_center_line = new DBObjectCollection();
            special_shapes_info = new List<Special_graph_Info>();
        }
        private void Re_construct_center_line(ref Line in_start_line, ref Line out_start_line)
        {
            var lines = new DBObjectCollection();
            foreach (var l in in_lines)
                lines.Add(l);
            foreach (var l in out_lines)
                lines.Add(l);
            lines = ThMEPHVACLineProc.Pre_proc(lines);
            Adjust_start_line(in_start_line, out_start_line, lines);
            spatial_index = new ThCADCoreNTSSpatialIndex(lines);
            // 将起始线更新为线集中的线，否则在空间索引中找不到
            Update_start_line(ref in_start_line, lines);
            Update_start_line(ref out_start_line, lines);
            in_lines.Clear();
            out_lines.Clear();
        }
        private void Adjust_start_line(Line in_start_line, Line out_start_line, DBObjectCollection lines)
        {
            var i_srt_l = new Line();
            var o_srt_l = new Line();
            var i_sp = in_start_line.StartPoint;
            var o_sp = out_start_line.StartPoint;
            foreach (Line l in lines)
            {
                if (i_sp.IsEqualTo(l.StartPoint, point_tor) || i_sp.IsEqualTo(l.EndPoint, point_tor))
                {
                    if (!(i_sp.IsEqualTo(l.StartPoint, point_tor) && i_sp.IsEqualTo(l.EndPoint, point_tor)))
                        i_srt_l = l;
                }
                if (o_sp.IsEqualTo(l.StartPoint, point_tor) || o_sp.IsEqualTo(l.EndPoint, point_tor))
                {
                    if (!(o_sp.IsEqualTo(l.StartPoint, point_tor) && o_sp.IsEqualTo(l.EndPoint, point_tor)))
                        o_srt_l = l;
                }
            }
            if (i_srt_l.Length > 0)
            {
                lines.Remove(i_srt_l);
                var p = i_sp.IsEqualTo(i_srt_l.StartPoint, point_tor) ? i_srt_l.EndPoint : i_srt_l.StartPoint;
                lines.Add(new Line(i_sp, p));
            }
            if (o_srt_l.Length > 0)
            {
                lines.Remove(o_srt_l);
                var p = o_sp.IsEqualTo(o_srt_l.StartPoint, point_tor) ? o_srt_l.EndPoint : o_srt_l.StartPoint;
                lines.Add(new Line(o_sp, p));
            }
        }
        private void Move_to_org()
        {
            var dis_mat = Matrix3d.Displacement(-move_srt_p.GetAsVector());
            foreach (Line l in out_center_line)
                l.TransformBy(dis_mat);
            fan_break_p.TransformBy(dis_mat);
        }
        private void Move_to_zero(Point3d fan_inlet_p,
                                 Point3d fan_outlet_p,
                                 DBObjectCollection center_line,
                                 DBObjectCollection wall_lines,
                                 out Point3d inlet_p,
                                 out Point3d outlet_p)
        {
            var dis_mat = Matrix3d.Displacement(-move_srt_p.GetAsVector());
            foreach (Line l in bypass)
                l.TransformBy(dis_mat);
            foreach (Line l in center_line)
                l.TransformBy(dis_mat);
            foreach (Line l in wall_lines)
                l.TransformBy(dis_mat);
            inlet_p = fan_inlet_p.TransformBy(dis_mat);
            outlet_p = fan_outlet_p.TransformBy(dis_mat);
        }
        private void Get_vt_elbow_pos(Point3d in_search_point, Point3d out_search_point)
        {
            if (bypass.Count == 0)
            {
                in_vt_pos = Record_vt_elbow_pos(in_search_point);
                out_vt_pos = Record_vt_elbow_pos(out_search_point);
            }
        }
        private Point3d Record_vt_elbow_pos(Point3d search_point)
        {
            foreach (var l in center_lines)
            {
                if (l.sp.IsEqualTo(search_point, point_tor))
                {
                    var dir_vec = (l.ep - l.sp).GetNormal();
                    var line = new Line(l.sp + (dir_vec * l.src_shrink), l.ep - (dir_vec * l.dst_shrink));
                    var mid_p = ThMEPHVACService.Get_mid_point(line);
                    var dis = search_point.DistanceTo(mid_p);
                    if (dis > 2000)
                        dis = 2000;
                    return search_point + (dir_vec * dis);
                }
            }
            throw new NotImplementedException("Start point is not in the line set");
        }
        private void Collect_lines()
        {
            foreach (var l in in_lines)
            {
                if (Is_bypass(l))
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.bypass_size));
                else
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.in_duct_size));
            }
            foreach (var l in out_lines)
            {
                if (Is_bypass(l))
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.bypass_size));
                else
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.out_duct_size));
            }
        }
        private void Shrink_duct()
        {
            foreach (var shape in special_shapes_info)
            {
                switch (shape.every_port_width.Count)
                {
                    case 2: Shrink_elbow_duct(shape); break;
                    case 3: Shrink_tee_duct(shape); break;
                    case 4: Shrink_cross_duct(shape); break;
                    default: throw new NotImplementedException();
                }
            }
        }
        private void Shrink_cross_duct(Special_graph_Info info)
        {
            ThDuctPortsShapeService.Get_cross_shrink(info, out int o_outter_idx, out int o_inner_idx, out int o_collinear_idx,
                                out double in_shrink, out double o_inner_shrink, out double o_outter_shrink, out double o_collinear_shrink);
            var idx = Search_idx(info.lines[0]);
            if (idx >= 0)
                center_lines[idx].dst_shrink = in_shrink;
            idx = Search_idx(info.lines[o_outter_idx]);
            if (idx >= 0)
                center_lines[idx].src_shrink = o_outter_shrink;
            idx = Search_idx(info.lines[o_inner_idx]);
            if (idx >= 0)
                center_lines[idx].src_shrink = o_inner_shrink;
            idx = Search_idx(info.lines[o_collinear_idx]);
            if (idx >= 0)
                center_lines[idx].src_shrink = o_collinear_shrink;
        }
        private void Shrink_tee_duct(Special_graph_Info info)
        {
            ThDuctPortsShapeService.Get_tee_shrink(info, out int branch_idx, out int other_idx,
                                          out double in_shrink, out double branch_shrink, out double other_shrink);
            var idx = Search_idx(info.lines[0]);
            if (idx >= 0)
                center_lines[idx].dst_shrink = in_shrink;
            idx = Search_idx(info.lines[branch_idx]);
            if (idx >= 0)
                center_lines[idx].src_shrink = branch_shrink;
            idx = Search_idx(info.lines[other_idx]);
            if (idx >= 0)
                center_lines[idx].src_shrink = other_shrink;
        }
        private void Shrink_elbow_duct(Special_graph_Info info)
        {
            double open_angle = ThMEPHVACService.Get_elbow_open_angle(info);
            double shrink_len = ThDuctPortsShapeService.Get_elbow_shrink(open_angle, info.every_port_width[0], 0, info.K);
            var idx = Search_idx(info.lines[0]);
            if (idx >= 0)
                center_lines[idx].dst_shrink = shrink_len;
            idx = Search_idx(info.lines[1]);
            if (idx >= 0)
                center_lines[idx].src_shrink = shrink_len;
        }
        private void Get_special_shape_info(Point3d start_point, Line start_line, HashSet<Line> line_set, string duct_size)
        {
            var lines = new DBObjectCollection();
            foreach (var l in line_set)
                lines.Add(l);
            spatial_index = new ThCADCoreNTSSpatialIndex(lines);
            var search_p = start_point.IsEqualTo(start_line.StartPoint, point_tor) ? start_line.EndPoint : start_line.StartPoint;
            Update_start_line(ref start_line, lines);
            Do_search_special_shape(search_p, start_line, duct_size);
        }
        private void Update_start_line(ref Line start_line, DBObjectCollection lines)
        {
            foreach (Line l in lines)
            {
                if (start_line.StartPoint.IsEqualTo(l.StartPoint, point_tor))
                {
                    start_line = l;
                    return;
                }
            }
        }
        private void Do_search_special_shape(Point3d search_point, Line current_line, string duct_size)
        {
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                return;
            }
            foreach (Line l in res)
            {
                var step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                Do_search_special_shape(step_p, l, duct_size);
            }
            if (res.Count >= 1)
            {
                Record_shape_parameter(search_point, current_line, res, duct_size);
            }
        }
        private void Record_shape_parameter(Point3d center_point, Line in_line, DBObjectCollection out_lines, string duct_size)
        {
            var lines = new List<Line>();
            var shape_port_widths = new List<double>();
            var tar_point = center_point.IsEqualTo(in_line.StartPoint, point_tor) ? in_line.EndPoint : in_line.StartPoint;
            lines.Add(new Line(center_point, tar_point));
            var duct_width = ThMEPHVACService.Get_width(duct_size);
            var bypass_width = ThMEPHVACService.Get_width(param.bypass_size);
            var width = Is_bypass(in_line) ? bypass_width : duct_width;
            shape_port_widths.Add(width);
            foreach (Line l in out_lines)
            {
                string size = duct_size;
                width = Is_bypass(l) ? bypass_width : duct_width;
                shape_port_widths.Add(width);
                tar_point = center_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                lines.Add(new Line(center_point, tar_point));
            }
            special_shapes_info.Add(new Special_graph_Info(lines, shape_port_widths));
        }
        public void Cut_center_line(string scenario, DBObjectCollection wall_lines)
        {
            var index = new ThCADCoreNTSSpatialIndex(wall_lines);
            if (scenario.Contains("排") && !scenario.Contains("补"))
            {
                Do_adjust(param.in_duct_size, wall_lines, in_lines, index);
                // bypass不需要裁剪，直接加到中心线中
                foreach (Line l in bypass)
                    in_lines.Add(l);
            }
            else
            {
                Do_adjust(param.out_duct_size, wall_lines, out_lines, index);
                // bypass不需要裁剪，直接加到中心线中
                foreach (Line l in bypass)
                    out_lines.Add(l);
            }
        }
        private void Do_adjust(string duct_size,
                               DBObjectCollection wall_lines, 
                               HashSet<Line> center_lines, 
                               ThCADCoreNTSSpatialIndex index)
        {
            fan_break_p = Point3d.Origin;
            out_center_line = new DBObjectCollection();
            var cross_line = new Line();
            foreach (var l in center_lines)
            {
                if (!ThMEPHVACService.Is_in_polyline(l.StartPoint, wall_lines) && 
                    !ThMEPHVACService.Is_in_polyline(l.EndPoint, wall_lines))
                    out_center_line.Add(l);
                else
                {
                    var pl = ThMEPHVACService.Get_line_extend(l, 1);
                    var res = index.SelectCrossingPolygon(pl);
                    if (res.Count > 0)
                    {
                        var line = res[0] as Line;
                        fan_break_p = ThMEPHVACService.Intersect_point(l, line);
                        if (fan_break_p.IsEqualTo(Point3d.Origin))
                            return;
                        cross_line = l;
                        break;
                    }
                }
            }
            Update_break_point(wall_lines, center_lines, duct_size);
            if (!fan_break_p.IsEqualTo(Point3d.Origin))
                Update_center_line(center_lines, cross_line, wall_lines);
        }
        private void Update_break_point(DBObjectCollection wall_lines, HashSet<Line> center_lines, string duct_size)
        {
            if (ThMEPHVACService.Is_out_polyline(bypass, wall_lines))
            {
                // type 2 更新打断点为旁通和中心线的交点
                var lines = new DBObjectCollection();
                foreach (var l in center_lines)
                    lines.Add(l);
                var p = ThMEPHVACService.Line_set_with_one_intersection(bypass, lines, out Line bypass_cross, out Line center_cross);
                if (!p.IsEqualTo(Point3d.Origin, point_tor))
                {
                    Update_break_point_by_tee(bypass_cross, center_cross, p, duct_size);
                }
            }
        }
        private void Update_break_point_by_tee(Line bypass_cross, Line center_cross, Point3d break_p, string duct_size)
        {
            double dis;
            var duct_width = ThMEPHVACService.Get_width(duct_size);
            var bypass_width = ThMEPHVACService.Get_width(param.bypass_size);
            var dir_vec = ThMEPHVACService.Get_edge_direction(center_cross);
            if (ThMEPHVACService.Is_connected(bypass_cross, center_cross, point_tor))
            {
                // bypass collinear with other
                dis = (duct_width - duct_width) * 0.5 + duct_width + 50;
            }
            else
            {
                // bypass vertical with other
                dis = bypass_width * 0.5 + 100;
            }
            fan_break_p = break_p + (dir_vec * dis);
        }
        private void Update_center_line(HashSet<Line> center_lines, Line cross_line, DBObjectCollection wall_lines)
        {
            foreach (Line l in out_center_line)
                center_lines.Remove(l);
            center_lines.Remove(cross_line);
            var p = ThMEPHVACService.Is_in_polyline(cross_line.StartPoint, wall_lines) ?
                    cross_line.StartPoint : cross_line.EndPoint;
            center_lines.Add(new Line(p, fan_break_p));
            var other_p = p.IsEqualTo(cross_line.StartPoint, point_tor) ?
                    cross_line.EndPoint : cross_line.StartPoint;
            out_center_line.Add(new Line(fan_break_p, other_p));
        }
        private void Get_duct_info(Point3d start_point, Line start_line, HashSet<Line> lines)
        {
            var search_point = start_point.IsEqualTo(start_line.StartPoint, point_tor) ? start_line.EndPoint : start_line.StartPoint;
            Do_search_duct(search_point, start_line, lines);
            lines.Add(new Line(start_point, search_point));
        }
        private void Do_search_duct(Point3d search_point, Line current_line, HashSet<Line> lines)
        {
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                var other_p = search_point.IsEqualTo(current_line.StartPoint, point_tor) ? current_line.EndPoint : current_line.StartPoint;
                var line = new Line(other_p, search_point); // 空间索引出来的线可能不是风的流动方向
                lines.Add(line);
                last_line = line;
                return;
            }
            foreach (Line l in res)
            {
                var step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                Do_search_duct(step_p, l, lines);
                var line = new Line(search_point, step_p); // 空间索引出来的线可能不是风的流动方向
                lines.Add(line);
            }
        }
        private DBObjectCollection Detect_cross_line(Point3d search_point, Line current_line)
        {
            var poly = new Polyline();
            poly.CreatePolygon(search_point.ToPoint2D(), 4, 10);
            var res = spatial_index.SelectCrossingPolygon(poly);
            res.Remove(current_line);
            return res;
        }
        private void Update_search_point(Point3d fan_inlet_p,
                                         Point3d fan_outlet_p,
                                         Duct_InParam info, 
                                         ref DBObjectCollection center_line,
                                         out Point3d i_srt_p, 
                                         out Point3d o_srt_p,
                                         out Line i_srt_line,
                                         out Line o_srt_line)
        {
            i_srt_p = ThMEPHVACService.Round_point(fan_inlet_p, 6);
            o_srt_p = ThMEPHVACService.Round_point(fan_outlet_p, 6);
            center_line = Get_start_line(i_srt_p, o_srt_p, center_line, out i_srt_line, out o_srt_line);
            if (Math.Abs(i_srt_line.Length) < 1e-3 || Math.Abs(o_srt_line.Length) < 1e-3)
                return;
            Update_proc("进", ref i_srt_line, info, center_line, ref i_srt_p);
            Update_proc("出", ref o_srt_line, info, center_line, ref o_srt_p);
        }
        private void Update_proc(string special_name,
                                 ref Line start_line,
                                 Duct_InParam info, 
                                 DBObjectCollection lines,
                                 ref Point3d srt_p)
        {
            double shrink_dis = 0;
            ThDuctPortsDrawService.Get_fan_dyn_block_properity(fan.Data, fan.Name, out double in_width, out double out_width);
            double duct_width = 0;
            if (special_name.Contains("进"))
                shrink_dis = Get_shrink_dis(info, true, in_width, out duct_width);
            else if (special_name.Contains("出"))
                shrink_dis = Get_shrink_dis(info, false, out_width, out duct_width);
            if (shrink_dis < 0)
                return;
            var dir_vec = ThMEPHVACService.Get_edge_direction(start_line);
            srt_p = start_line.StartPoint + (dir_vec * shrink_dis);
            var reducing = new Line(start_line.StartPoint, srt_p);
            if (special_name.Contains("进"))
                reducings.Add(ThDuctPortsReDrawFactory.Create_reducing(reducing, in_width, duct_width, true));
            else if (special_name.Contains("出"))
                reducings.Add(ThDuctPortsReDrawFactory.Create_reducing(reducing, out_width, duct_width, true));
            lines.Remove(start_line);
            start_line = new Line(srt_p, start_line.EndPoint);
            lines.Add(start_line);
        }
        private double Get_shrink_dis(Duct_InParam info, bool is_in, double width, out double duct_width)
        {
            var duct_size = is_in ? info.in_duct_size : info.out_duct_size;
            duct_width = ThMEPHVACService.Get_width(duct_size);
            if (Math.Abs(duct_width - width) < 1e-3)
                return -1;
            var big_width = Math.Max(duct_width, width);
            var small_width = Math.Min(duct_width, width);
            return ThMEPHVACService.Get_reducing_len(big_width, small_width);
        }
        private DBObjectCollection Get_start_line(Point3d i_srt_p, 
                                                  Point3d o_srt_p, 
                                                  DBObjectCollection lines,
                                                  out Line i_srt_line,
                                                  out Line o_srt_line)
        {
            i_srt_line = new Line();
            o_srt_line = new Line();
            var new_lines = new DBObjectCollection();
            foreach (Line l in lines)
            {
                if (i_srt_p.IsEqualTo(l.StartPoint, point_tor) || i_srt_p.IsEqualTo(l.EndPoint, point_tor))
                {
                    var other_p = i_srt_p.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                    i_srt_line = new Line(i_srt_p, other_p);
                    new_lines.Add(i_srt_line);
                }
                else if (o_srt_p.IsEqualTo(l.StartPoint, point_tor) || o_srt_p.IsEqualTo(l.EndPoint, point_tor))
                {
                    var other_p = o_srt_p.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                    o_srt_line = new Line(o_srt_p, other_p);
                    new_lines.Add(o_srt_line);
                }
                else
                    new_lines.Add(l);
            }
            return new_lines;
        }
        private bool Is_bypass(Line line)
        {
            foreach (Line l in bypass)
            {
                if (ThMEPHVACService.Is_same_line(line, l, point_tor))
                    return true;
            }
            return false;
        }
        private int Search_idx(Line l)
        {
            for (int i = 0; i < center_lines.Count; ++i)
            {
                var duct = center_lines[i];
                if (ThMEPHVACService.Is_same_line(l, duct.sp, duct.ep, point_tor))
                    return i;
            }
            return -1;
        }
    }
}
