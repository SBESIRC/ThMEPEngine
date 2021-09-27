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
        public bool is_room;
        public string duct_size;
        public TextAlignLine(Line l, bool is_room, string duct_size)
        {
            this.l = l;
            this.is_room = is_room;
            this.duct_size = duct_size;
        }
    }
    public class ThFanAnalysis
    {
        public Vector3d start_dir_vec;
        public ThDbModelFan fan;
        public Duct_InParam param;
        public Tolerance point_tor;
        public Point3d move_srt_p;
        public Point3d fan_break_p;
        public HashSet<Line> room_lines;
        public HashSet<Line> not_room_lines;
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
        private bool is_axis;
        private bool is_exhaust;
        private double type3_sep_dis;
        private ThCADCoreNTSSpatialIndex spatial_index;
        public ThFanAnalysis(double type3_sep_dis,
                             ThDbModelFan fan, 
                             Duct_InParam param,
                             DBObjectCollection bypass,
                             DBObjectCollection center_line, 
                             DBObjectCollection wall_lines)
        {
            Init(type3_sep_dis, fan, bypass, param);
            Move_to_zero(fan.FanInletBasePoint, fan.FanOutletBasePoint, center_line, wall_lines, out Point3d room_p, out Point3d not_room_p);
            Update_search_point(room_p, not_room_p, param, ref center_line, out Point3d i_room_p, out Point3d i_not_room_p, 
                                                                            out Line room_line, out Line not_room_line);
            start_dir_vec = ThMEPHVACService.Get_edge_direction(room_line);
            spatial_index = new ThCADCoreNTSSpatialIndex(center_line);
            Get_duct_info(i_room_p, room_line, room_lines);
            Get_duct_info(i_not_room_p, not_room_line, not_room_lines);
            Cut_center_line(wall_lines);
            Re_construct_center_line(ref room_line, ref not_room_line);
            Get_duct_info(i_room_p, room_line, room_lines);
            text_alignment.Add(new TextAlignLine(last_line, true, param.room_duct_size));
            Get_duct_info(i_not_room_p, not_room_line, not_room_lines);
            text_alignment.Add(new TextAlignLine(last_line, false, param.other_duct_size));
            Get_special_shape_info(i_room_p, room_line, room_lines, param.room_duct_size);
            Get_special_shape_info(i_not_room_p, not_room_line, not_room_lines, param.other_duct_size);
            Collect_lines();
            Shrink_duct();
            Merge_bypass();
            if (bypass.Count == 0 && param.bypass_size != null)
            {
                Get_vt_elbow_pos(i_room_p, i_not_room_p);
                vt = new ThVTee(in_vt_pos, out_vt_pos, param.bypass_size);
            }
            Move_to_org();
        }
        private void Init(double type3_sep_dis, ThDbModelFan fan, DBObjectCollection bypass, Duct_InParam param)
        {
            this.fan = fan;
            this.param = param;
            this.bypass = bypass;
            this.type3_sep_dis = type3_sep_dis;
            is_axis = (fan.Name.Contains("轴流风机"));
            is_exhaust = fan.is_exhaust;
            move_srt_p = is_exhaust ? fan.FanInletBasePoint : fan.FanOutletBasePoint;
            point_tor = new Tolerance(1.5, 1.5);
            var comp = new LineCompare(point_tor);
            room_lines = new HashSet<Line>(comp);
            not_room_lines = new HashSet<Line>(comp);
            text_alignment = new List<TextAlignLine>();
            reducings = new List<Line_Info>();
            center_lines = new List<Fan_duct_Info>();
            out_center_line = new DBObjectCollection();
            special_shapes_info = new List<Special_graph_Info>();
        }
        private void Merge_bypass()
        {
            if (param.bypass_pattern == "RBType3")
            {
                var sep_info1 = new Fan_duct_Info();
                var sep_info2 = new Fan_duct_Info();
                foreach (var info1 in center_lines)
                {
                    foreach (var info2 in center_lines)
                    {
                        if (info1.Equals(info2))
                            continue;
                        var dis = ThMEPHVACService.Get_line_dis(info1.sp, info1.ep, info2.sp, info2.ep);
                        if (Math.Abs(dis - type3_sep_dis) < 1e-3)
                        {
                            sep_info1 = info1;
                            sep_info2 = info2;
                            break;
                        }
                    }
                    if (!String.IsNullOrEmpty(sep_info1.size))
                        break;
                }
                if (String.IsNullOrEmpty(sep_info1.size) || String.IsNullOrEmpty(sep_info2.size))
                    throw new NotImplementedException("风机进出口旁通未找到打断的旁通");
                Update_center_line(sep_info1, sep_info2);
            }
        }
        private void Update_center_line(Fan_duct_Info info1, Fan_duct_Info info2)
        {
            ThMEPHVACService.Get_longest_dis(info1.sp, info1.ep, info2.sp, info2.ep, out Point3d p1, out Point3d p2);
            var new_duct = new Fan_duct_Info(p1, p2, param.bypass_size, info1.src_shrink, info2.src_shrink);
            center_lines.Remove(info1);
            center_lines.Remove(info2);
            center_lines.Add(new_duct);
        }
        private void Do_add_inner_duct(Line start_line, Point3d srt_p, string duct_size)
        {
            var dir_vec = ThMEPHVACService.Get_edge_direction(start_line);
            var height = ThMEPHVACService.Get_height(duct_size);
            var sp = srt_p - (dir_vec * height);
            center_lines.Add(new Fan_duct_Info(sp, srt_p, duct_size));
        }
        private void Re_construct_center_line(ref Line in_start_line, ref Line out_start_line)
        {
            var lines = new DBObjectCollection();
            foreach (var l in room_lines)
                lines.Add(l);
            foreach (var l in not_room_lines)
                lines.Add(l);
            lines = ThMEPHVACLineProc.Pre_proc(lines);
            Adjust_start_line(in_start_line, out_start_line, lines);
            spatial_index = new ThCADCoreNTSSpatialIndex(lines);
            // 将起始线更新为线集中的线，否则在空间索引中找不到
            Update_start_line(ref in_start_line, lines);
            Update_start_line(ref out_start_line, lines);
            room_lines.Clear();
            not_room_lines.Clear();
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
            var dis_mat = Matrix3d.Displacement(move_srt_p.GetAsVector());
            foreach (Line l in out_center_line)
                l.TransformBy(dis_mat);
            fan_break_p = fan_break_p.TransformBy(dis_mat);
            // Move for FPM
            dis_mat = Matrix3d.Displacement(-fan_break_p.GetAsVector());
            foreach (Line l in out_center_line)
                l.TransformBy(dis_mat);
        }
        private void Move_to_zero(Point3d fan_inlet_p,
                                  Point3d fan_outlet_p,
                                  DBObjectCollection center_line,
                                  DBObjectCollection wall_lines,
                                  out Point3d room_p,
                                  out Point3d not_room_p)
        {
            var dis_mat = Matrix3d.Displacement(-move_srt_p.GetAsVector());
            foreach (Line l in bypass)
                l.TransformBy(dis_mat);
            foreach (Line l in center_line)
                l.TransformBy(dis_mat);
            foreach (Line l in wall_lines)
                l.TransformBy(dis_mat);
            if (!is_exhaust)
            {
                room_p = fan_outlet_p.TransformBy(dis_mat);
                not_room_p = fan_inlet_p.TransformBy(dis_mat);
            }
            else
            {
                room_p = fan_inlet_p.TransformBy(dis_mat);
                not_room_p = fan_outlet_p.TransformBy(dis_mat);
            }
        }
        private void Get_vt_elbow_pos(Point3d in_search_point, Point3d out_search_point)
        {
            in_vt_pos = Record_vt_elbow_pos(in_search_point);
            out_vt_pos = Record_vt_elbow_pos(out_search_point);
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
            foreach (var l in room_lines)
            {
                if (Is_bypass(l))
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.bypass_size));
                else
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.room_duct_size));
            }
            foreach (var l in not_room_lines)
            {
                if (Is_bypass(l))
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.bypass_size));
                else
                    center_lines.Add(new Fan_duct_Info(l.StartPoint, l.EndPoint, param.other_duct_size));
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
        public void Cut_center_line(DBObjectCollection wall_lines)
        {
            var index = new ThCADCoreNTSSpatialIndex(wall_lines);
            Do_adjust(param.room_duct_size, wall_lines, room_lines, index);
            // bypass不需要裁剪，直接加到中心线中
            foreach (Line l in bypass)
                room_lines.Add(l);
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
                    var e_l = ThMEPHVACService.Extend_line(l, 1);
                    var pl = ThMEPHVACService.Get_line_extend(e_l, 1);
                    var res = index.SelectCrossingPolygon(pl);
                    if (res.Count > 0)
                    {
                        var line = res[0] as Line;
                        fan_break_p = ThMEPHVACService.Intersect_point(e_l, line);
                        if (fan_break_p.IsEqualTo(Point3d.Origin))
                            continue;
                        cross_line = l;
                        break;
                    }
                }
            }
            if (fan_break_p.IsEqualTo(Point3d.Origin))
                return;
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
        private void Update_search_point(Point3d room_p,
                                         Point3d not_room_p,
                                         Duct_InParam info, 
                                         ref DBObjectCollection center_line,
                                         out Point3d i_room_p, 
                                         out Point3d i_not_room_p,
                                         out Line room_line,
                                         out Line not_room_line)
        {
            i_room_p = ThMEPHVACService.Round_point(room_p, 6);
            i_not_room_p = ThMEPHVACService.Round_point(not_room_p, 6);
            center_line = Get_start_line(i_room_p, i_not_room_p, center_line, out room_line, out not_room_line);
            
            if (is_exhaust)
            {
                if (fan.IntakeForm.Contains("上进") || fan.IntakeForm.Contains("下进") ||
                    fan.IntakeForm.Contains("上出") || fan.IntakeForm.Contains("下出"))
                {
                    Update_in_start_info(center_line, param.room_duct_size, ref room_line, ref i_room_p);
                    Do_add_inner_duct(room_line, i_room_p, param.room_duct_size);
                }
                else
                {
                    Update_out_start_info(true, fan.fan_in_width, info, center_line, ref room_line, ref i_room_p);
                }
                Update_out_start_info(false, fan.fan_out_width, info, center_line, ref not_room_line, ref i_not_room_p);
            }
            else
            {
                // 非排风时room_p -> fan_outlet_p not_room_p -> fan_inlet_p
                if (fan.IntakeForm.Contains("上进") || fan.IntakeForm.Contains("下进"))
                {
                    // 进风口加上翻，出风口变径
                    Update_in_start_info(center_line, param.other_duct_size, ref not_room_line, ref i_not_room_p);
                    Do_add_inner_duct(not_room_line, i_not_room_p, param.other_duct_size);
                    Update_out_start_info(true, fan.fan_out_width, info, center_line, ref room_line, ref i_room_p);
                }
                else if (fan.IntakeForm.Contains("上出") || fan.IntakeForm.Contains("下出"))
                {
                    // 出风口加上翻，进风口变径
                    Update_in_start_info(center_line, param.room_duct_size, ref room_line, ref i_room_p);
                    Do_add_inner_duct(room_line, i_room_p, param.room_duct_size);
                    Update_out_start_info(false, fan.fan_in_width, info, center_line, ref not_room_line, ref i_not_room_p);
                }
                else
                {
                    // 两边变径
                    Update_out_start_info(true, fan.fan_out_width, info, center_line, ref room_line, ref i_room_p);
                    Update_out_start_info(false, fan.fan_in_width, info, center_line, ref not_room_line, ref i_not_room_p);
                }
            }            
        }
        private void Update_in_start_info(DBObjectCollection lines, string duct_size, ref Line start_line, ref Point3d srt_p)
        {
            var shrink_len = ThMEPHVACService.Get_height(duct_size);
            var dir_vec = ThMEPHVACService.Get_edge_direction(start_line);
            srt_p += (shrink_len * dir_vec * 0.5);
            lines.Remove(start_line);
            start_line = new Line(srt_p, start_line.EndPoint);
            lines.Add(start_line);
        }
        private void Update_out_start_info(bool is_in,
                                           double fan_width,
                                           Duct_InParam info, 
                                           DBObjectCollection lines,
                                           ref Line start_line,
                                           ref Point3d srt_p)
        {
            double shrink_dis = (is_in) ? Get_shrink_dis(info.room_duct_size, fan_width, out double duct_width) :
                                          Get_shrink_dis(info.other_duct_size, fan_width, out duct_width);
            if (shrink_dis < 0)
                return;
            var dir_vec = ThMEPHVACService.Get_edge_direction(start_line);
            srt_p = start_line.StartPoint + (dir_vec * shrink_dis);
            var hose_len = (fan.scenario == "消防补风" || fan.scenario == "消防排烟" || fan.scenario == "消防加压送风") ? 0 : 200;
            if (shrink_dis < hose_len)
            {
                srt_p += (dir_vec * hose_len);//需要添加软接距离来消除变径反向
            }
            var reducing = new Line(start_line.StartPoint + (dir_vec * hose_len), srt_p);
            if (reducing.Length < 200)
            {
                reducing = new Line(reducing.StartPoint, reducing.StartPoint + (200 * dir_vec));
                srt_p = reducing.EndPoint;
            }
            reducings.Add(ThDuctPortsReDrawFactory.Create_reducing(reducing, fan_width, duct_width, is_axis));
            lines.Remove(start_line);
            start_line = new Line(srt_p, start_line.EndPoint);
            lines.Add(start_line);
        }
        private double Get_shrink_dis(string duct_size, double fan_width, out double duct_width)
        {
            duct_width = ThMEPHVACService.Get_width(duct_size);
            var big_width = Math.Max(duct_width, fan_width);
            var small_width = Math.Min(duct_width, fan_width);
            return ThMEPHVACService.Get_reducing_len(big_width, small_width);
        }
        private DBObjectCollection Get_start_line(Point3d i_room_p, 
                                                  Point3d i_not_room_p, 
                                                  DBObjectCollection lines,
                                                  out Line room_line,
                                                  out Line not_room_line)
        {
            room_line = new Line();
            not_room_line = new Line();
            var new_lines = new DBObjectCollection();
            var start_p_tor = new Tolerance(200, 200);
            foreach (Line l in lines)
            {
                if (i_room_p.IsEqualTo(l.StartPoint, start_p_tor) || i_room_p.IsEqualTo(l.EndPoint, start_p_tor))
                {
                    var other_p = i_room_p.IsEqualTo(l.StartPoint, start_p_tor) ? l.EndPoint : l.StartPoint;
                    room_line = new Line(i_room_p, other_p);
                    new_lines.Add(room_line);
                }
                else if (i_not_room_p.IsEqualTo(l.StartPoint, start_p_tor) || i_not_room_p.IsEqualTo(l.EndPoint, start_p_tor))
                {
                    var other_p = i_not_room_p.IsEqualTo(l.StartPoint, start_p_tor) ? l.EndPoint : l.StartPoint;
                    not_room_line = new Line(i_not_room_p, other_p);
                    new_lines.Add(not_room_line);
                }
                else
                    new_lines.Add(l);
            }
            if (Math.Abs(room_line.Length) < 1e-3 || Math.Abs(not_room_line.Length) < 1e-3)
                throw new NotImplementedException("未找到与风机进出口相连的中心线");
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
