using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using ThCADCore.NTS;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public enum Tee_Type
    {
        BRANCH_COLLINEAR_WITH_OTTER = 0,
        BRANCH_VERTICAL_WITH_OTTER = 1
    };
    public class Port_Info
    {
        public double air_vloume { get; set; }
    }
    public class Endline_Info
    {
        public List<Port_Info> ports;
        public ThDuctEdge<ThDuctVertex> direct_edge;
        public Endline_Info(ThDuctEdge<ThDuctVertex> direct_edge_)
        {
            direct_edge = direct_edge_;
            ports = new List<Port_Info>();
        }
    };
    public class Merged_endline_Info
    {
        public double total_len;
        public double in_port_width;
        public List<Endline_Info> segments;
        public Merged_endline_Info(double total_len_, List<Endline_Info> segments_, double in_port_width_)
        {
            segments = segments_;
            total_len = total_len_;
            
            in_port_width = in_port_width_;
        }
    };
    public class Special_graph_Info 
    {
        //图形的中心点为lines[0].startpoint
        public double K { get; set; }
        public List<Line> lines { get; set; } //lines[0]为in_line 其余为out_lines
        public List<double> every_port_width { get; set; }
        public Special_graph_Info(List<Line> lines_, List<double> every_port_width_)
        {
            K = 0.7;
            lines = lines_;
            every_port_width = every_port_width_;
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

        // 用于内部传参
        private string scenario;
        private Line start_line;
        private double len_count;
        private double in_speed;
        private double air_volumn;
        private Tolerance point_tor;
        private bool is_first;
        private bool endline_enable;
        private HashSet<Line> line_set;
        private List<Endline_Info> lines_ptr;
        private Queue<double> endline_in_air_volume;
        private ThCADCoreNTSSpatialIndex spatial_index;

        public ThDuctPortsAnalysis(DBObjectCollection center_lines_, Point3d start_point_, ThDuctPortsParam in_param)
        {
            Init_param(center_lines_, in_param);
            Get_start_line(center_lines_, start_point_, out Point3d search_point);
            if (!start_point_.IsEqualTo(start_line.StartPoint, point_tor) &&
                !start_point_.IsEqualTo(start_line.EndPoint, point_tor))
                return;
            Get_merged_endline(center_lines_, search_point, start_line);
            var dis_res = new ThDuctResourceDistribute(merged_endlines, in_param.port_num);
            dis_res.Distrib_total_air_volumn(merged_endlines, air_volumn, in_param.port_num);
            
            Reset_flag();
            Set_main_duct_volumn(search_point, start_line);
            Reset_flag();
            Get_endline_in_air_volume();
            Get_merged_endline_in_port_width(center_lines_, search_point, start_line);
            Reset_flag();
            Search_special_shape_info(search_point, start_line);   
        }

        private void Get_endline_in_air_volume()
        {
            foreach (var endline in merged_endlines)
            {
                var seg = endline.segments[endline.segments.Count - 1];
                endline_in_air_volume.Enqueue(seg.ports[seg.ports.Count - 1].air_vloume);
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
                    if (Is_same_line(l, seg.direct_edge.Source.Position, seg.direct_edge.Target.Position))
                        return new Pair_coor (i, j);
                }
            }
            return null;
        }
        public int Search_main_duct_idx(Line l)
        {
            for (int i = 0; i < main_ducts.Count; ++i)
                if (Is_same_line(l, main_ducts[i].Source.Position, main_ducts[i].Target.Position))
                    return i;
            return -1;
        }
        private void Get_merged_endline_in_port_width(DBObjectCollection center_lines_, Point3d search_point, Line start_line)
        {
            if (center_lines_.Count == 1)
                merged_endlines[0].in_port_width = ui_duct_width;
            else
                Set_merged_endline_in_port_width(search_point, start_line);
        }
        
        private void Get_merged_endline(DBObjectCollection center_lines_, Point3d search_point, Line start_line)
        {
            if (center_lines_.Count == 1)
            {
                Line l = center_lines_[0] as Line;
                List<Endline_Info> list = new List<Endline_Info>();
                ThDuctEdge<ThDuctVertex> edge = new ThDuctEdge<ThDuctVertex>(new ThDuctVertex(l.StartPoint), new ThDuctVertex(l.EndPoint));
                list.Add(new Endline_Info(edge));
                Merged_endline_Info info = new Merged_endline_Info(l.Length, list, ui_duct_width);
                merged_endlines.Add(info);
            }
            else
                Count_endline_len(search_point, start_line);
        }
        private void Init_param(DBObjectCollection center_lines_, ThDuctPortsParam in_param)
        {
            len_count = 0;
            scenario = in_param.scenario;
            air_volumn = in_param.air_volumn;
            endline_enable = false;
            line_set = new HashSet<Line>();
            endline_in_air_volume = new Queue<double>();
            main_ducts = new List<ThDuctEdge<ThDuctVertex>>();
            merged_endlines = new List<Merged_endline_Info>();
            special_shapes_info = new List<Special_graph_Info>();
            spatial_index = new ThCADCoreNTSSpatialIndex(center_lines_);
            point_tor = new Tolerance(1.5, 1.5);
            in_speed = in_param.air_speed;
            string[] s = in_param.in_duct_size.Split('x');
            ui_duct_width = double.Parse(s[0]);
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

        private void Search_special_shape_info(Point3d search_point, Line current_line)
        {
            if (!line_set.Add(current_line))
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
                Point3d step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
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

        private double Set_main_duct_volumn(Point3d search_point, Line current_line)
        {
            if (!line_set.Add(current_line))
                return 0;
            var res = Detect_cross_line(search_point, current_line);
            if (res.Count == 0)
            {
                line_set.Add(current_line);
                endline_enable = true;
                var info = Search_endline(current_line);
                return info.ports[info.ports.Count - 1].air_vloume;
            }
            double air_volumn = 0;
            foreach (Line l in res)
            {
                Point3d step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                air_volumn += Set_main_duct_volumn(step_p, l);
                if (endline_enable)
                {
                    if (res.Count > 1)
                        endline_enable = false;
                    else
                        air_volumn = Get_cur_duct_volumn(current_line, air_volumn);
                }
            }
            if (!endline_enable)
                main_ducts.Add(Create_directed_edge_by_line(current_line, search_point, 0, 0, air_volumn));
            return air_volumn;
        }
        private void Record_shape_parameter(Point3d center_point, Line in_line, DBObjectCollection out_lines)
        {
            List<Line> lines = new List<Line>();
            List<double> shape_port_widths = new List<double>();
            Point3d tar_point = center_point.IsEqualTo(in_line.StartPoint, point_tor) ? in_line.EndPoint : in_line.StartPoint;
            lines.Add(new Line(center_point, tar_point));
            shape_port_widths.Add(Get_special_shape_port_width(in_line, ui_duct_width));
            is_first = false;
            double in_width = shape_port_widths[0];
            foreach (Line l in out_lines)
            {
                shape_port_widths.Add(Get_special_shape_port_width(l, in_width));
                tar_point = center_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                lines.Add(new Line(center_point, tar_point));
            }
            special_shapes_info.Add(new Special_graph_Info(lines, shape_port_widths));
        }
        private double Get_special_shape_port_width(Line current_line, double width_limit)
        {
            string duct_size = string.Empty;
            var info = Search_endline(current_line);
            if (info != null)
            {
                var speed = (main_ducts.Count == 0) ? in_speed : 0;
                return ((main_ducts.Count == 0) && is_first) ? ui_duct_width : 
                    ThDuctPortsService.Calc_duct_width(info.ports[info.ports.Count - 1].air_vloume, width_limit, speed, scenario, ref duct_size);
            }
            else
            {
                return ui_duct_width;
            }
        }
        private double Get_cur_duct_volumn(Line current_line, double air_volumn)
        {
            var info = Search_endline(current_line);
            if (info != null)
                return info.ports[info.ports.Count - 1].air_vloume;
            else
                return main_ducts[main_ducts.Count - 1].AirVolume + air_volumn;
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
                double width = ui_duct_width;
                Point3d step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
                Set_merged_endline_in_port_width(step_p, l);
                if (endline_enable)
                {
                    if (res.Count > 1)
                    {
                        var merged_endline_idx = Search_endline_idx(l);
                        width = ThDuctPortsService.Calc_duct_width(endline_in_air_volume.Dequeue(), width, scenario);
                        merged_endlines[merged_endline_idx.i].in_port_width = width;
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
                Record_endline_info(res, current_line, search_point);
                return;
            }

            foreach (Line l in res)
            {
                Point3d step_p = search_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
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
        private void Record_endline_info(DBObjectCollection res, Line current_line, Point3d search_point)
        {
            endline_enable = true;
            lines_ptr = new List<Endline_Info>();
            lines_ptr.Add(new Endline_Info(Create_directed_edge_by_line(current_line, search_point, 0, 0, 0)));
            len_count = current_line.Length;
            line_set.Add(current_line);
        }
        private void Record_merged_endline_info(DBObjectCollection res, Line current_line, Point3d search_point)
        {
            if (endline_enable)
            {
                if (res.Count == 1)
                {
                    lines_ptr.Add(new Endline_Info(Create_directed_edge_by_line(current_line, search_point, 0, 0, 0)));
                    len_count += current_line.Length;
                }
                if (res.Count > 1 || Is_same_line(start_line, current_line))
                {
                    merged_endlines.Add(new Merged_endline_Info(len_count, lines_ptr, ui_duct_width));
                    len_count = 0;
                    endline_enable = false;
                }
            }
        }
        private ThDuctEdge<ThDuctVertex> Create_directed_edge_by_line(Line l, Point3d end_point, double src_shrink, double tar_shrink, double air_volumn)
        {
            var source_point = end_point.IsEqualTo(l.StartPoint, point_tor) ? l.EndPoint : l.StartPoint;
            ThDuctVertex source = new ThDuctVertex(source_point);
            ThDuctVertex target = new ThDuctVertex(end_point);
            var edge = new ThDuctEdge<ThDuctVertex>(source, target);
            edge.SourceShrink = src_shrink;
            edge.TargetShrink = tar_shrink;
            edge.AirVolume = air_volumn;
            return edge;
        }
        private bool Is_same_line(Line l1, Line l2)
        {
            Point3d sp1 = l1.StartPoint;
            Point3d ep1 = l1.EndPoint;
            Point3d sp2 = l2.StartPoint;
            Point3d ep2 = l2.EndPoint;
            if ((sp1.IsEqualTo(sp2, point_tor) && ep1.IsEqualTo(ep2, point_tor)) ||
                (sp1.IsEqualTo(ep2, point_tor) && ep1.IsEqualTo(sp1, point_tor)))
            {
                return true;
            }
            return false;
        }
        private bool Is_same_line(Line l1, Point3d sp, Point3d ep)
        {
            Point3d sp1 = l1.StartPoint;
            Point3d ep1 = l1.EndPoint;
            if ((sp1.IsEqualTo(sp, point_tor) && ep1.IsEqualTo(ep, point_tor)) ||
                (sp1.IsEqualTo(ep, point_tor) && ep1.IsEqualTo(sp, point_tor)))
            {
                return true;
            }
            return false;
        }
        private Endline_Info Search_endline(Line l)
        {
            foreach (var info in merged_endlines)
            {
                foreach (var seg in info.segments)
                    if (Is_same_line(l, seg.direct_edge.Source.Position, seg.direct_edge.Target.Position))
                        return seg;
            }
            return null;
        }
        private ThDuctEdge<ThDuctVertex> Search_main_duct(Line l)
        {
            foreach (var info in main_ducts)
            {
                if (Is_same_line(l, info.Source.Position, info.Target.Position))
                    return info;
            }
            return null;
        }
    }
}