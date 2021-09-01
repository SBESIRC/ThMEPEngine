using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using ThCADCore.NTS;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class Port_Info
    {
        public bool have_r;
        public bool have_l;
        public Point3d position;
        public double air_volume;        
        public Port_Info() { }
        public Port_Info(double air_volume_, Point3d position_)
        {
            air_volume = air_volume_;
            position = position_;
            have_r = true;
            have_l = true;
        }
    };
    public class Endline_Info
    {
        public bool distrib_enable;
        public List<Port_Info> ports;
        public ThDuctEdge<ThDuctVertex> direct_edge;
        public Endline_Info(ThDuctEdge<ThDuctVertex> direct_edge_)
        {
            distrib_enable = true;
            direct_edge = direct_edge_;
            ports = new List<Port_Info>();
        }
    };
    public class Merged_endline_Info
    {
        public string in_size_info;
        public List<Endline_Info> segments;
        public Merged_endline_Info(List<Endline_Info> segments_, string in_size_info_)
        {
            segments = segments_;
            in_size_info = in_size_info_;
        }
    };
    public class Pair_coor
    {
        public readonly int i;
        public readonly int j;
        public Pair_coor(int i_, int j_)
        {
            i = i_;
            j = j_;
        }
    };
    public class ThDuctPortsAnalysis
    {
        public double ui_duct_width { get; set; }
        public List<Merged_endline_Info> merged_endlines { get; set; }
        public List<ThDuctEdge<ThDuctVertex>> main_ducts { get; set; }
        public List<Special_graph_Info> special_shapes_info { get; set; }
        public Point3d start_point { get; set; }
        public bool is_recreate;
        // 用于内部传参
        private double in_speed;
        private double air_volumn;
        private bool is_first;
        private bool endline_enable;
        private string ui_duct_size;
        private Line start_line;
        private Tolerance point_tor;
        private HashSet<Line> line_set;
        private List<Endline_Info> lines_ptr;
        private Queue<double> endline_in_air_volume;
        private ThCADCoreNTSSpatialIndex spatial_index;

        public ThDuctPortsAnalysis(DBObjectCollection center_lines_,
                                   DBObjectCollection exclude_lines_,
                                   Point3d start_point_, 
                                   ThMEPHVACParam in_param)
        {
            Init_param(center_lines_, in_param, start_point_);
            Get_start_line(center_lines_, start_point_, out Point3d search_point);
            if (!start_point_.IsEqualTo(start_line.StartPoint, point_tor) &&
                !start_point_.IsEqualTo(start_line.EndPoint, point_tor))
                return;
            Get_merged_endline(center_lines_, search_point, start_line);//预建立图结构
            Search_undistrib_line(exclude_lines_);//设置不布置风口的管段
            Remove_endline_end_seg(center_lines_);
            Reset_flag();
            merged_endlines.Clear();
            Get_merged_endline(center_lines_, search_point, start_line);//建立排除掉不布风口的图结构
            Search_undistrib_line(exclude_lines_);//设置不布置风口的管段
        }
        private void Init_param(DBObjectCollection center_lines_, 
                                ThMEPHVACParam in_param, 
                                Point3d start_point_)
        {
            endline_enable = false;
            is_recreate = in_param.is_redraw;
            air_volumn = in_param.air_volume;
            ui_duct_size = in_param.in_duct_size;
            in_speed = in_param.air_speed;
            start_point = start_point_;
            point_tor = new Tolerance(1.5, 1.5);
            ui_duct_width = ThMEPHVACService.Get_width(in_param.in_duct_size);
            line_set = new HashSet<Line>();
            endline_in_air_volume = new Queue<double>();
            main_ducts = new List<ThDuctEdge<ThDuctVertex>>();
            merged_endlines = new List<Merged_endline_Info>();
            special_shapes_info = new List<Special_graph_Info>();
            spatial_index = new ThCADCoreNTSSpatialIndex(center_lines_);
        }
        public void Do_anay(int port_num,
                            ThDuctPortsModifyPort modifyer,
                            DBObjectCollection center_lines)
        {
            if (is_recreate)
            {
                Get_start_line(modifyer.center_line, Point3d.Origin, out Point3d search_point, out Line start_l);
                Set_duct_info(search_point, start_l, modifyer);
                Set_special_shape_info(search_point);
            }
            else
            {
                Get_start_line(center_lines, Point3d.Origin, out Point3d search_point);
                Set_duct_air_volume(port_num, search_point, center_lines);
                Set_special_shape_info(search_point);
            }
        }
        private void Set_duct_air_volume(int port_num, Point3d search_point, DBObjectCollection center_lines)
        {
            _ = new ThDuctResourceDistribute(merged_endlines, air_volumn, port_num);
            Reset_flag();
            Set_main_duct_volume(search_point, start_line);
            Reset_flag();
            Get_endline_in_air_volume();
            Get_merged_endline_in_port_width(center_lines, search_point, start_line);
        }
        private void Remove_endline_end_seg(DBObjectCollection center_lines_)
        {
            foreach (var info in merged_endlines)
            {
                if (!info.segments[0].distrib_enable)
                {
                    var end_seg = info.segments[0];
                    while (!end_seg.distrib_enable)
                    {
                        Remove_center_line(info, center_lines_);
                        info.segments.RemoveAt(0);
                        if (info.segments.Count == 0)
                            break;
                        end_seg = info.segments[0];
                    }
                }
            }
            spatial_index = new ThCADCoreNTSSpatialIndex(center_lines_);
        }
        private void Remove_center_line(Merged_endline_Info info, DBObjectCollection center_lines_)
        {
            int idx = 0;
            var edge = info.segments[0].direct_edge;
            var line = new Line(edge.Source.Position, edge.Target.Position);
            for (int i = 0; i < center_lines_.Count; ++i)
            {
                Line l = center_lines_[i] as Line;
                if (ThMEPHVACService.Is_same_line(line, l, point_tor))
                {
                    idx = i;
                    break;
                }
            }
            center_lines_.RemoveAt(idx);
        }
        private void Search_undistrib_line(DBObjectCollection exclude_lines_)
        {
            foreach (var info in merged_endlines)
                foreach (var edge in info.segments)
                {
                    if (Is_exclude(edge.direct_edge, exclude_lines_))
                        edge.distrib_enable = false;
                }
        }
        private void Get_endline_in_air_volume()
        {
            foreach (var endline in merged_endlines)
            {
                var seg = endline.segments[endline.segments.Count - 1];
                endline_in_air_volume.Enqueue(Get_endline_air_volume(seg));
            }
        }
        private void Reset_flag()
        {
            line_set.Clear();
            is_first = true;
            endline_enable = false;
        }
        public Pair_coor Search_endline_idx(Line l)
        {
            for (int i = 0; i < merged_endlines.Count; ++i)
            {
                var info = merged_endlines[i];
                for (int j = 0; j < info.segments.Count; ++j)
                {
                    var seg = info.segments[j];
                    if (ThMEPHVACService.Is_same_line(l, seg.direct_edge.Source.Position, seg.direct_edge.Target.Position, point_tor))
                        return new Pair_coor (i, j);
                }
            }
            return null;
        }
        public int Search_main_duct_idx(Line l)
        {
            for (int i = 0; i < main_ducts.Count; ++i)
                if (ThMEPHVACService.Is_same_line(l, main_ducts[i].Source.Position, 
                                                       main_ducts[i].Target.Position, point_tor))
                    return i;
            return -1;
        }
        private void Get_merged_endline_in_port_width(DBObjectCollection center_lines_, Point3d search_point, Line start_line)
        {
            if (center_lines_.Count == 1)
                merged_endlines[0].in_size_info = ui_duct_size;
            else
                Set_merged_endline_in_port_width(search_point, start_line);
        }
        private void Get_merged_endline(DBObjectCollection center_lines_, Point3d search_point, Line start_line)
        {
            if (center_lines_.Count == 1)
            {
                var list = new List<Endline_Info>();
                var edge = new ThDuctEdge<ThDuctVertex>(new ThDuctVertex(start_point), new ThDuctVertex(search_point));
                list.Add(new Endline_Info(edge));
                var info = new Merged_endline_Info(list, ui_duct_size);
                merged_endlines.Add(info);
            }
            else
                Count_endline_len(search_point, start_line);
        }
        private void Get_start_line(DBObjectCollection center_lines_, Point3d start_point, out Point3d search_point)
        {
            start_line = new Line();
            search_point = Point3d.Origin;
            foreach (Line l in center_lines_)
            {
                if (start_point.IsEqualTo(l.StartPoint, point_tor) || start_point.IsEqualTo(l.EndPoint, point_tor))
                {
                    start_line = l;
                    search_point = start_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                }
            }
        }
        private void Get_start_line(DBObjectCollection center_lines, 
                                   Point3d start_point, 
                                   out Point3d search_point, 
                                   out Line start_line)
        {
            start_line = new Line();
            search_point = Point3d.Origin;
            foreach (Line l in center_lines)
            {
                if (start_point.IsEqualTo(l.StartPoint, point_tor) || start_point.IsEqualTo(l.EndPoint, point_tor))
                {
                    start_line = l;
                    search_point = start_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                }
            }
        }
        private void Set_special_shape_info(Point3d search_point)
        {
            Reset_flag();
            Search_special_shape_info(search_point, start_line);
        }
        private void Search_special_shape_info(Point3d search_point, Line current_line)
        {
            if (line_set.Contains(current_line))
                return ;
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                line_set.Add(current_line);
                endline_enable = true;
                return ;
            }
            foreach (Line l in res)
            {
                var step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                Search_special_shape_info(step_p, l);
                if (endline_enable)
                {
                    if (res.Count > 1)
                        endline_enable = false;
                }
            }
            if (!endline_enable)
                Record_shape_parameter(search_point, current_line, res);
        }
        private void Set_duct_info(Point3d search_point, 
                                  Line current_line, 
                                  ThDuctPortsModifyPort modifyer)
        {
            Distrib_endline_volume(modifyer);
            Reset_flag();
            Set_main_duct_volume(search_point, current_line, modifyer.ducts);
            Distrib_port_volume(modifyer.avg_air_volume);
            Distrib_port_pos(modifyer.ducts);
        }
        private void Distrib_port_pos(List<Duct_Info> ducts)
        {
            foreach (var endline in merged_endlines)
            {
                foreach (var seg in endline.segments)
                {
                    var line = new Line(seg.direct_edge.Source.Position, seg.direct_edge.Target.Position);
                    var info = Search_duct_idx(line, ducts);
                    for (int i = 0; i < seg.ports.Count; ++i)
                    {
                        int idx = seg.ports.Count - 1 - i;
                        seg.ports[i].position = info.port_pos[idx];
                    }
                }
            }
        }
        private Duct_Info Search_duct_idx(Line line, List<Duct_Info> ducts)
        {
            foreach (var info in ducts)
            {
                var l = new Line(info.sp, info.ep);
                if (ThMEPHVACService.Is_same_line(line, l, point_tor))
                    return info;
            }
            throw new NotImplementedException();
        }
        private void Distrib_port_volume(double avg_air_volume)
        {
            foreach (var endline in merged_endlines)
            {
                string size_info = ui_duct_size;
                var in_air_volume = endline.segments[endline.segments.Count - 1].direct_edge.AirVolume;
                ThMEPHVACService.Calc_duct_width(false, 0, in_air_volume, ref size_info);
                if (merged_endlines.Count > 1)
                    endline.in_size_info = size_info;
                for (int i = endline.segments.Count - 1; i >= 0; --i)
                {
                    var seg = endline.segments[i];
                    for (int j = seg.ports.Count - 1; j >= 0; --j)
                    {
                        var port = seg.ports[j];
                        port.air_volume = in_air_volume;
                        in_air_volume -= avg_air_volume;
                    }
                }
            }
        }
        private void Distrib_endline_volume(ThDuctPortsModifyPort modifyer)
        {
            var ducts = modifyer.ducts;
            foreach (var info in merged_endlines)
            {
                foreach (var seg in info.segments)
                {
                    var cur_line = new Line(seg.direct_edge.Source.Position, seg.direct_edge.Target.Position);
                    int idx = Get_duct_idx(cur_line, ducts);
                    int port_num = ducts[idx].port_num;
                    for (int i = 0; i < port_num; ++i)
                        seg.ports.Add(new Port_Info());
                    seg.direct_edge.AirVolume = ducts[idx].air_volume;
                }
            }
        }
        private void Set_main_duct_volume(Point3d search_point, Line current_line, List<Duct_Info> ducts)
        {
            if (!line_set.Add(current_line))
                return;
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                line_set.Add(current_line);
                endline_enable = true;
                return;
            }
            foreach (Line l in res)
            {
                var step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                Set_main_duct_volume(step_p, l, ducts);
                if (endline_enable)
                {
                    if (res.Count > 1)
                        endline_enable = false;
                }
            }
            if (!endline_enable)
                main_ducts.Add(Create_directed_edge_by_line(current_line, search_point, 0, 0, Get_duct_air_volume(current_line, ducts)));
        }
        private int Get_duct_idx(Line line, List<Duct_Info> ducts)
        {
            for (int i = 0; i < ducts.Count; ++i)
            {
                var l = new Line(ducts[i].sp, ducts[i].ep);
                if (ThMEPHVACService.Is_same_line(line, l, point_tor))
                    return i;
            }
            throw new NotImplementedException();
        }
        private double Get_duct_air_volume(Line line, List<Duct_Info> ducts)
        {
            int idx = Get_duct_idx(line, ducts);
            return ducts[idx].air_volume;
        }
        private double Set_main_duct_volume(Point3d search_point, Line current_line)
        {
            if (!line_set.Add(current_line))
                return 0;
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                line_set.Add(current_line);
                endline_enable = true;
                return Get_endline_air_volume(Search_endline(current_line));
            }
            double air_volume = 0;
            foreach (Line l in res)
            {
                var step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                air_volume += Set_main_duct_volume(step_p, l);
                if (endline_enable)
                {
                    if (res.Count > 1)
                        endline_enable = false;
                    else
                        air_volume = Get_cur_duct_volume(current_line, air_volume);
                }
            }
            if (!endline_enable)
                main_ducts.Add(Create_directed_edge_by_line(current_line, search_point, 0, 0, air_volume));
            return air_volume;
        }
        private void Record_shape_parameter(Point3d center_point, Line in_line, DBObjectCollection out_lines)
        {
            string duct_size = ui_duct_size;
            var lines = new List<Line>();
            var shape_port_widths = new List<double>();
            var tar_point = center_point.IsEqualTo(in_line.StartPoint, point_tor) ? in_line.EndPoint : in_line.StartPoint;
            lines.Add(new Line(center_point, tar_point));
            shape_port_widths.Add(Get_special_shape_port_width(in_line, ref duct_size));
            is_first = false;
            foreach (Line l in out_lines)
            {
                string size = duct_size;
                shape_port_widths.Add(Get_special_shape_port_width(l, ref size));
                tar_point = center_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                lines.Add(new Line(center_point, tar_point));
            }
            special_shapes_info.Add(new Special_graph_Info(lines, shape_port_widths));
        }
        private double Get_special_shape_port_width(Line current_line, ref string duct_size)
        {
            var seg = Search_endline(current_line);
            if (seg != null)
            {
                var speed = (main_ducts.Count == 0) ? in_speed : 0;
                return ((main_ducts.Count == 0) && is_first) ? ui_duct_width : 
                         ThMEPHVACService.Calc_duct_width(is_first, speed, Get_endline_air_volume(seg), ref duct_size);
            }
            else
                return ui_duct_width;
        }
        private double Get_cur_duct_volume(Line current_line, double air_volume)
        {
            var seg = Search_endline(current_line);
            if (seg != null)
                return Get_endline_air_volume(seg);
            else
                return main_ducts[main_ducts.Count - 1].AirVolume + air_volume;
        }
        private void Set_merged_endline_in_port_width(Point3d search_point, Line current_line)
        {
            if (!line_set.Add(current_line))
                return;
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                endline_enable = true;
                line_set.Add(current_line);
                return;
            }
            foreach (Line l in res)
            {
                var size_info = ui_duct_size;
                var step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                Set_merged_endline_in_port_width(step_p, l);
                if (endline_enable)
                {
                    if (res.Count > 1)
                    {
                        var merged_endline_idx = Search_endline_idx(l);
                        ThMEPHVACService.Calc_duct_width(false, 0, endline_in_air_volume.Dequeue(), ref size_info);
                        merged_endlines[merged_endline_idx.i].in_size_info = size_info;
                        endline_enable = false;
                    }
                }
            }
        }
        private void Count_endline_len(Point3d search_point, Line current_line)
        {
            if (!line_set.Add(current_line))
                return;
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                Record_endline_info(current_line, search_point);
                return;
            }
            foreach (Line l in res)
            {
                var step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                Count_endline_len(step_p, l);
                Record_merged_endline_info(res, current_line, search_point);
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
        private void Record_endline_info(Line current_line, Point3d search_point)
        {
            endline_enable = true;
            lines_ptr = new List<Endline_Info>
            {
                new Endline_Info(Create_directed_edge_by_line(current_line, search_point, 0, 0, 0))
            };
            line_set.Add(current_line);
        }
        private void Record_merged_endline_info(DBObjectCollection res, Line current_line, Point3d search_point)
        {
            if (endline_enable)
            {
                if (res.Count == 1)
                {
                    lines_ptr.Add(new Endline_Info(Create_directed_edge_by_line(current_line, search_point, 0, 0, 0)));
                }
                if (res.Count > 1 || ThMEPHVACService.Is_same_line(start_line, current_line, point_tor))
                {
                    merged_endlines.Add(new Merged_endline_Info(lines_ptr, ui_duct_size));
                    endline_enable = false;
                }
            }
        }
        private ThDuctEdge<ThDuctVertex> Create_directed_edge_by_line(Line l, Point3d end_point, double src_shrink, double tar_shrink, double air_volumn)
        {
            var source_point = end_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
            var source = new ThDuctVertex(source_point);
            var target = new ThDuctVertex(end_point);
            var edge = new ThDuctEdge<ThDuctVertex>(source, target)
            {
                SourceShrink = src_shrink,
                TargetShrink = tar_shrink,
                AirVolume = air_volumn
            };
            return edge;
        }
        private bool Is_exclude(ThDuctEdge<ThDuctVertex> edge, DBObjectCollection exclude_lines)
        {
            var cur_line = new Line(edge.Source.Position, edge.Target.Position);
            foreach (Line l in exclude_lines)
            {
                if (ThMEPHVACService.Is_same_line(cur_line, l, point_tor))
                    return true;
            }
            return false;
        }
        
        private Endline_Info Search_endline(Line l)
        {
            foreach (var info in merged_endlines)
            {
                foreach (var seg in info.segments)
                    if (ThMEPHVACService.Is_same_line(l, seg.direct_edge.Source.Position, 
                                                         seg.direct_edge.Target.Position, point_tor))
                        return seg;
            }
            return null;
        }
        private double Get_endline_air_volume(Endline_Info info)
        {
            if (info.ports.Count == 0)
                return info.direct_edge.AirVolume;
            else
                return info.ports[info.ports.Count - 1].air_volume;
        }
    }
}