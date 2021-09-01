using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsJudger
    {
        public List<Point2d> dir_align_points;
        public List<Point2d> ver_align_points;
        private double align_limit;
        private Point3d start_pos;
        private List<Line> v_grid_set;
        private List<Line> h_grid_set;
        private List<Line> crossing_v_grid_set; 
        private List<Line> crossing_h_grid_set;
        private ThCADCoreNTSSpatialIndex grid_spatial_index;
        public ThDuctPortsJudger(Point3d start_pos_,
                                 bool is_recreate,
                                 List<Merged_endline_Info> endline, 
                                 List<Endline_seg_Info> endline_segs)
        {
            Init(start_pos_);
            if (!is_recreate)
            {
                var grids = Get_grid_lines();
                Move_to_org(grids);
                var grid_lines = Filter_h_v_grid_line(grids);
                grids.Clear();
                if (grid_lines.Count > 0)
                {
                    grid_spatial_index = new ThCADCoreNTSSpatialIndex(grid_lines);
                    Seperate_v_and_h_grid(grid_lines, v_grid_set, h_grid_set);
                    Get_crossing_grid(endline);
                    Adjust_port_by_wall(endline_segs);
                    Adjust_end_endline_width(endline_segs);
                }
            }
        }
        private void Init(Point3d start_pos_)
        {   
            align_limit = 500;
            start_pos = start_pos_;
            dir_align_points = new List<Point2d>();
            ver_align_points = new List<Point2d>();
            v_grid_set = new List<Line>();
            h_grid_set = new List<Line>();
            crossing_v_grid_set = new List<Line>();
            crossing_h_grid_set = new List<Line>();
        }
        private DBObjectCollection Filter_h_v_grid_line(DBObjectCollection grid_lines)
        {
            var lins = new DBObjectCollection();
            for (int i = 0; i < grid_lines.Count; ++i)
            {
                var l = grid_lines[i] as Line;
                if (ThMEPHVACService.Is_vertical(l) || ThMEPHVACService.Is_horizontal(l))
                    lins.Add(l);
            }
            return lins;
        }
        private void Move_to_org(DBObjectCollection grid_lines)
        {
            var mat = Matrix3d.Displacement(-start_pos.GetAsVector());
            foreach (Line l in grid_lines)
            {
                l.TransformBy(mat);
            }
        }
        private void Adjust_end_endline_width(List<Endline_seg_Info> endline_segs)
        {
            foreach (var merged_endline in endline_segs)
            {
                var sizes = Less_duct_size(4000, merged_endline.segs);
                Modify_sizes(sizes);
                Set_sizes(4000, sizes, merged_endline.segs);
            }
        }
        private void Set_sizes(double threshold, List<string> sizes, List<Duct_ports_Info> merged_endline)
        {
            int count = 0;
            foreach (var duct_info in merged_endline)
            {
                foreach (var port_info in duct_info.ports_info)
                {
                    if (port_info.air_volume <= threshold)
                    {
                        duct_info.duct_size = sizes[count++];
                        string[] s = duct_info.duct_size.Split('x');
                        duct_info.width = Double.Parse(s[0]);
                    }
                }
            }
        }
        private void Modify_sizes(List<string> sizes)
        {
            for (int i = 0; i < sizes.Count - 1; ++i)
            {
                if (sizes[i] != sizes[i + 1])
                {
                    for (int j = i + 1; j < sizes.Count; ++j)
                    {
                        sizes[j] = sizes[i + 1];
                    }
                    break;
                }
            }
        }
        private List<string> Less_duct_size(double threshold, List<Duct_ports_Info> merged_endline)
        {
            var sizes = new List<string>();
            foreach (var duct_info in merged_endline)
            {
                foreach (var port_info in duct_info.ports_info)
                {
                    if (port_info.air_volume <= threshold)
                    {
                        sizes.Add(duct_info.duct_size);
                    }
                }
            }
            return sizes;
        }
        private void Adjust_port_by_wall(List<Endline_seg_Info> endline_segs)
        {
            if (v_grid_set.Count == 0 && h_grid_set.Count == 0)
                return;
            foreach (var merged_endline in endline_segs)
            {
                var dir_align_point = Search_align_port(merged_endline.segs, out Point2d dir_wall_point, crossing_h_grid_set, crossing_v_grid_set);
                Update_port_pos(dir_align_point, merged_endline.segs);
                dir_align_points.Add(dir_wall_point);
                Search_align_port(merged_endline.segs, out Point2d ver_wall_point, crossing_v_grid_set, crossing_h_grid_set);
                ver_align_points.Add(ver_wall_point);
            }
        }
        private void Update_port_pos(Point2d align_point, List<Duct_ports_Info> merged_endline)
        {
            if (align_point.IsEqualTo(Point2d.Origin, new Tolerance(1e-3, 1e-3)))
                return;
            var align_base_point = new Point3d(align_point.X, align_point.Y, 0);
            foreach (var port_seg in merged_endline)
            {
                for (int i = 0; i < port_seg.ports_info.Count; ++i)
                {
                    var pos = port_seg.ports_info[i].position;
                    double dis = align_base_point.DistanceTo(pos);
                    var dir_vec = (pos - align_base_point).GetNormal();
                    
                    double align_dis = (dis < 100) ? 0 : ThMEPHVACService.Align_distance(dis, 100);
                    port_seg.ports_info[i].position = align_base_point + dir_vec * align_dis;
                }
            }
        }
        private Point2d Search_align_port(List<Duct_ports_Info> merged_endline, 
                                          out Point2d wall_point, 
                                          List<Line> grid_set1,
                                          List<Line> grid_set2)
        {
            double min_dis = Double.MaxValue;
            var align_point = new Point2d();
            wall_point = Point2d.Origin;
            foreach (var port_seg in merged_endline)
            {
                if (ThMEPHVACService.Is_vertical(port_seg.l))
                {
                    if (grid_set1.Count == 0)
                        return Point2d.Origin;
                    double cur_min_dis = Do_search_align_port(port_seg.ports_info, grid_set1, out Point2d position, out Point2d wall_p);
                    Update_align_wall_info(cur_min_dis, ref min_dis, position, ref align_point, wall_p, ref wall_point);
                }
                else if (ThMEPHVACService.Is_horizontal(port_seg.l))
                {
                    if (grid_set2.Count == 0)
                        return Point2d.Origin;
                    double cur_min_dis = Do_search_align_port(port_seg.ports_info, grid_set2, out Point2d position, out Point2d wall_p);
                    Update_align_wall_info(cur_min_dis, ref min_dis, position, ref align_point, wall_p, ref wall_point);
                }
            }
            return align_point;
        }
        private void Update_align_wall_info(double cur_min_dis,
                                            ref double min_dis,
                                            Point2d position,
                                            ref Point2d align_point,
                                            Point2d wall_p,
                                            ref Point2d wall_point)
        {
            if (cur_min_dis >= align_limit && min_dis >= cur_min_dis)
            {
                min_dis = cur_min_dis;
                align_point = position;
                wall_point = wall_p;
            }
        }
        private double Do_search_align_port( List<Port_Info> ports_info, 
                                             List<Line> crossing_grid_set,
                                             out Point2d position,
                                             out Point2d wall_point)
        {
            double min_dis = Double.MaxValue;
            var min_p = new Point2d();
            var align_wall = new Line();
            foreach (Line l in crossing_grid_set)
            {
                foreach (var info in ports_info)
                {
                    double cur_dis = ThMEPHVACService.Align_distance(ThMEPHVACService.Point_to_line(info.position.ToPoint2D(), l), 100);
                    if (cur_dis >= align_limit &&  cur_dis <= min_dis)
                    {
                        min_p = info.position.ToPoint2D();
                        min_dis = cur_dis;
                        align_wall = l;
                    }
                }
            }
            var mirror_p = ThMEPHVACService.Get_mirror_point(min_p, align_wall);
            var mid_point = ThMEPHVACService.Get_mid_point(new Line(new Point3d (min_p.X, min_p.Y, 0) , new Point3d(mirror_p.X, mirror_p.Y, 0)));
            var dir_vec = (min_p - mirror_p).GetNormal();
            position = mid_point.ToPoint2D() + dir_vec * min_dis;
            wall_point = mid_point.ToPoint2D();
            return min_dis;
        }
        private void Get_crossing_grid(List<Merged_endline_Info> endlines)
        {
            foreach (var merged_endline in endlines)
            {
                var poly_line = Create_poly_line(merged_endline);
                var crossing_lines = grid_spatial_index.SelectCrossingPolygon(poly_line);
                Search_poly_border(merged_endline.segments, out Point2d top, out Point2d left, out Point2d right, out Point2d bottom);
                Search_poly_fence(top, left, right, bottom, crossing_lines);
                Seperate_v_and_h_grid(crossing_lines, crossing_v_grid_set, crossing_h_grid_set);
            }
        }

        private void Search_poly_fence(Point2d top, Point2d left, Point2d right, Point2d bottom, DBObjectCollection crossing_lines)
        {
            Add_top_fence(top, h_grid_set, crossing_lines);
            Add_left_fence(left, v_grid_set, crossing_lines);
            Add_right_fence(right, v_grid_set, crossing_lines);
            Add_bottom_fence(bottom, h_grid_set, crossing_lines);
        }
        private void Add_top_fence(Point2d p, List<Line> grid_set, DBObjectCollection crossing_lines)
        {
            var top_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = ThMEPHVACService.Point_to_line(p, l);
                if (l.StartPoint.Y > p.Y && dis < min_dis && ThMEPHVACService.Is_in_mirror_range(p, l))
                {
                    min_dis = dis;
                    top_line = l;
                }
            }
            if (top_line.StartPoint.DistanceTo(top_line.EndPoint) > 1e-3)
                crossing_lines.Add(top_line);
        }
        private void Add_right_fence(Point2d p, List<Line> grid_set, DBObjectCollection crossing_lines)
        {
            var right_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = ThMEPHVACService.Point_to_line(p, l);
                if (l.StartPoint.X > p.X && dis < min_dis && ThMEPHVACService.Is_in_mirror_range(p, l))
                {
                    min_dis = dis;
                    right_line = l;
                }
            }
            if (right_line.StartPoint.DistanceTo(right_line.EndPoint) > 1e-3)
                crossing_lines.Add(right_line);
        }
        private void Add_left_fence(Point2d p, List<Line> grid_set, DBObjectCollection crossing_lines)
        {
            var left_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = ThMEPHVACService.Point_to_line(p, l);
                if (l.StartPoint.X < p.X && dis < min_dis && ThMEPHVACService.Is_in_mirror_range(p, l))
                {
                    min_dis = dis;
                    left_line = l;
                }
            }
            if (left_line.StartPoint.DistanceTo(left_line.EndPoint) > 1e-3)
                crossing_lines.Add(left_line);
        }
        private void Add_bottom_fence(Point2d p, List<Line> grid_set, DBObjectCollection crossing_lines)
        {
            var bottom_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = ThMEPHVACService.Point_to_line(p, l);
                if (l.StartPoint.Y < p.Y && dis < min_dis && ThMEPHVACService.Is_in_mirror_range(p, l))
                {
                    min_dis = dis;
                    bottom_line = l;
                }
            }
            if (bottom_line.StartPoint.DistanceTo(bottom_line.EndPoint) > 1e-3)
                crossing_lines.Add(bottom_line);
        }
        
        private void Search_poly_border(List<Endline_Info> segs, out Point2d top, out Point2d left, out Point2d right, out Point2d bottom)
        {
            var lines = new DBObjectCollection();
            foreach (var seg in segs)
            {
                var l = new Line(seg.direct_edge.Source.Position, seg.direct_edge.Target.Position);
                lines.Add(l);
            }
            ThMEPHVACService.Search_poly_border(lines, out top, out left, out right, out bottom);
        }
        private void Seperate_v_and_h_grid(DBObjectCollection crossing_lines, List<Line> v_set, List<Line> h_set)
        {
            foreach (Line l in crossing_lines)
            {
                if (ThMEPHVACService.Is_vertical(l))
                    v_set.Add(l);
                else if (ThMEPHVACService.Is_horizontal(l))
                    h_set.Add(l);
            }
        }
        private Point3dCollection Create_poly_line(Merged_endline_Info merged_line)
        {
            var polygon = new Point3dCollection();
            foreach (var line in merged_line.segments)
            {
                polygon.Add(line.direct_edge.Target.Position);
            }
            polygon.Add(merged_line.segments[merged_line.segments.Count - 1].direct_edge.Source.Position);
            if (polygon.Count == 2)
            {
                Vector3d dir_vec = (polygon[1] - polygon[0]).GetNormal();
                Vector3d vertical_vec = dir_vec.RotateBy(Math.PI * 0.5, Vector3d.ZAxis);
                polygon.Add(polygon[1] + vertical_vec);
            }
            return polygon;
        }
        private DBObjectCollection Get_grid_lines()
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThAXISLineRecognitionEngine();
                engine.Recognize(acadDatabase.Database, new Point3dCollection());
                return engine.Elements.Select(o => o.Outline).ToCollection();
            }
        }
    }
}