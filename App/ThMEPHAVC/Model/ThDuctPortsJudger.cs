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
        private double align_limit;
        public List<Point2d> dir_align_points;
        public List<Point2d> ver_align_points;
        private List<Line> v_grid_set;
        private List<Line> h_grid_set;
        private List<Line> crossing_v_grid_set; 
        private List<Line> crossing_h_grid_set;
        private ThCADCoreNTSSpatialIndex grid_spatial_index;
        
        public ThDuctPortsJudger(List<Merged_endline_Info> endline, List<List<Duct_ports_Info>> endline_segs)
        {
            align_limit = 500;
            dir_align_points = new List<Point2d>();
            ver_align_points = new List<Point2d>();
            v_grid_set = new List<Line>();
            h_grid_set = new List<Line>();
            crossing_v_grid_set = new List<Line>();
            crossing_h_grid_set = new List<Line>();

            Inner_shrink(endline_segs);
            var grid_lines = Get_grid_lines();
            if (grid_lines.Count > 0)
            {
                grid_spatial_index = new ThCADCoreNTSSpatialIndex(grid_lines);
                Seperate_v_and_h_grid(grid_lines, v_grid_set, h_grid_set);
                Get_crossing_grid(endline);
                Adjust_port_by_wall(endline_segs);
            }
        }

        private void Adjust_port_by_wall(List<List<Duct_ports_Info>> endline_segs)
        {
            if (v_grid_set.Count == 0 && h_grid_set.Count == 0)
                return;
            foreach (var merged_endline in endline_segs)
            {
                var dir_align_point = Search_align_port(merged_endline, out Point2d dir_wall_point, crossing_h_grid_set, crossing_v_grid_set);
                Update_port_pos(dir_align_point, merged_endline);
                dir_align_points.Add(dir_wall_point);
                Search_align_port(merged_endline, out Point2d ver_wall_point, crossing_v_grid_set, crossing_h_grid_set);
                ver_align_points.Add(ver_wall_point);
            }
        }

        private void Update_port_pos(Point2d align_point, List<Duct_ports_Info> merged_endline)
        {
            Vector3d vec = Get_edge_direction(merged_endline[0].l);
            if (!Is_vertical(vec) && !Is_horizontal(vec))
                return;
            Point3d align_base_point = new Point3d(align_point.X, align_point.Y, 0);
            foreach (var port_seg in merged_endline)
            {
                for (int i = 0; i < port_seg.ports_position.Count; ++i)
                {
                    Point3d pos = port_seg.ports_position[i];
                    double dis = align_base_point.DistanceTo(pos);
                    Vector3d dir_vec = (pos - align_base_point).GetNormal();
                    
                    double align_dis = (dis < 100) ? 0 : Align_distance(dis, 100);
                    port_seg.ports_position[i] = align_base_point + dir_vec * align_dis;
                }
            }
        }
        private Point2d Search_align_port(List<Duct_ports_Info> merged_endline, 
                                          out Point2d wall_point, 
                                          List<Line> grid_set1,
                                          List<Line> grid_set2)
        {
            double min_dis = Double.MaxValue;
            Point2d align_point = new Point2d();
            wall_point = Point2d.Origin;
            foreach (var port_seg in merged_endline)
            {
                Vector3d dir_vec = Get_edge_direction(port_seg.l);
                if (Is_vertical(dir_vec))
                {
                    double cur_min_dis = Do_search_align_port(port_seg.ports_position, grid_set1, out Point2d position, out Point2d wall_p);
                    Update_align_wall_info(cur_min_dis, ref min_dis, position, ref align_point, wall_p, ref wall_point);
                }
                else
                {
                    double cur_min_dis = Do_search_align_port(port_seg.ports_position, grid_set2, out Point2d position, out Point2d wall_p);
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
            if (cur_min_dis >= align_limit && min_dis > cur_min_dis)
            {
                min_dis = cur_min_dis;
                align_point = position;
                wall_point = wall_p;
            }
        }
        private double Do_search_align_port(List<Point3d> ports_position, 
                                             List<Line> crossing_grid_set,
                                             out Point2d position,
                                             out Point2d wall_point)
        {
            double min_dis = Double.MaxValue;
            Point2d min_p = new Point2d ();
            Line align_wall = new Line();
            foreach (Line l in crossing_grid_set)
            {
                foreach (Point3d p in ports_position)
                {
                    double cur_dis = Align_distance(Point_to_line(p.ToPoint2D(), l), 100);
                    if (cur_dis >= align_limit &&  cur_dis < min_dis)
                    {
                        min_p = p.ToPoint2D();
                        min_dis = cur_dis;
                        align_wall = l;
                    }
                }
            }
            Point2d mirror_p = Get_mirror_point(min_p, align_wall);
            Point3d mid_point = Get_mid_point(new Line(new Point3d (min_p.X, min_p.Y, 0) , new Point3d(mirror_p.X, mirror_p.Y, 0)));
            Vector2d dir_vec = (min_p - mirror_p).GetNormal();
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
                Search_poly_border(merged_endline, out Point2d top, out Point2d left, out Point2d right, out Point2d bottom);
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
            Line top_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = Point_to_line(p, l);
                if (l.StartPoint.Y > p.Y && dis < min_dis && Is_in_mirror_range(p, l))
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
            Line right_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = Point_to_line(p, l);
                if (l.StartPoint.X > p.X && dis < min_dis && Is_in_mirror_range(p, l))
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
            Line left_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = Point_to_line(p, l);
                if (l.StartPoint.X < p.X && Point_to_line(p, l) < min_dis && Is_in_mirror_range(p, l))
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
            Line bottom_line = new Line();
            double min_dis = Double.MaxValue;
            foreach (Line l in grid_set)
            {
                double dis = Point_to_line(p, l);
                if (l.StartPoint.Y < p.Y && Point_to_line(p, l) < min_dis && Is_in_mirror_range(p, l))
                {
                    min_dis = dis;
                    bottom_line = l;
                }
            }
            if (bottom_line.StartPoint.DistanceTo(bottom_line.EndPoint) > 1e-3)
                crossing_lines.Add(bottom_line);
        }
        private bool Is_in_mirror_range(Point2d p, Line l)
        {
            var vertical_p = Get_vertical_point(p, l);
            return Mid_point_is_in_line(vertical_p, l);
        }
        private bool Mid_point_is_in_line(Point2d p, Line l)
        {
            double maxX = l.StartPoint.X > l.EndPoint.X ? l.StartPoint.X : l.EndPoint.X;
            double maxY = l.StartPoint.Y > l.EndPoint.Y ? l.StartPoint.Y : l.EndPoint.Y;
            double minX = l.StartPoint.X < l.EndPoint.X ? l.StartPoint.X : l.EndPoint.X;
            double minY = l.StartPoint.Y < l.EndPoint.Y ? l.StartPoint.Y : l.EndPoint.Y;
            if (minX <= p.X && p.X <= maxX && minY <= p.Y && p.Y <= maxY)
                return true;
            return false;
        }
        private double Point_to_line(Point2d p, Line l)
        {
            Point2d vertical_p = Get_vertical_point(p, l);
            return vertical_p.GetDistanceTo(p);
        }
        private void Search_poly_border(Merged_endline_Info endlines, out Point2d top, out Point2d left, out Point2d right, out Point2d bottom)
        {
            top = new Point2d(0, Double.MinValue);
            left = new Point2d(Double.MaxValue, 0);
            right = new Point2d(Double.MinValue, 0);
            bottom = new Point2d(0, Double.MaxValue);
            foreach (var seg in endlines.segments)
            {
                Update_border(seg.direct_edge.Source.Position.ToPoint2D(), ref top, ref left, ref right, ref bottom);
                Update_border(seg.direct_edge.Target.Position.ToPoint2D(), ref top, ref left, ref right, ref bottom);
            }
        }
        private void Update_border(Point2d p, ref Point2d top, ref Point2d left, ref Point2d right, ref Point2d bottom)
        {
            if (p.X > right.X)
                right = p;
            if (p.X < left.X)
                left = p;
            if (p.Y > top.Y)
                top = p;
            if (p.Y < bottom.Y)
                bottom = p;
        }
        private void Seperate_v_and_h_grid(DBObjectCollection crossing_lines, List<Line> v_set, List<Line> h_set)
        {
            foreach (Line l in crossing_lines)
            {
                Vector3d dir_vec = Get_edge_direction(l);
                if (Is_vertical(dir_vec))
                    v_set.Add(l);
                else
                    h_set.Add(l);
            }
        }

        private Point3dCollection Create_poly_line(Merged_endline_Info merged_line)
        {
            Point3dCollection polygon = new Point3dCollection();
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
        public static void Inner_shrink(List<List<Duct_ports_Info>> endline_segs)
        {
            foreach (var endline in endline_segs)
            {
                foreach (var seg in endline)
                {
                    Vector3d dir_vec = Get_edge_direction(seg.l);
                    int port_num = seg.ports_position.Count;
                    if (port_num > 1)
                    {
                        double real_step = Math.Ceiling(seg.l.Length / 100) * 100.0 / port_num;
                        Point3d cur_p = seg.start_point + dir_vec * (0.5 * real_step);
                        for (int i = port_num - 1; i >= 0; --i)
                        {
                            seg.ports_position[i] = cur_p;
                            cur_p += real_step * dir_vec;
                        }
                    }
                    else if (port_num == 1)
                        seg.ports_position[0] = Get_mid_point(seg.l);
                }
            }
        }
        private Point2d Get_vertical_point(Point2d p, Line l)
        {
            var mirror = Get_mirror_point(p, l);
            return Get_mid_point(mirror, p);
        }
        private double Align_distance(double dis, double multiple)
        {
            return (Math.Ceiling(dis / multiple)) * multiple;
        }
        private Point2d Get_mirror_point(Point2d p, Line l)
        {
            return p.Mirror(new Line2d(l.StartPoint.ToPoint2D(), l.EndPoint.ToPoint2D()));
        }
        private static Vector3d Get_edge_direction(Line l)
        {
            Point3d srt_p = l.StartPoint;
            Point3d end_p = l.EndPoint;
            return (end_p - srt_p).GetNormal();
        }
        private static Point3d Get_mid_point(Line l)
        {
            Point3d sp = l.StartPoint;
            Point3d ep = l.EndPoint;
            return new Point3d((sp.X + ep.X) * 0.5, (sp.Y + ep.Y) * 0.5, 0);
        }
        private Point2d Get_mid_point(Point2d p1, Point2d p2)
        {
            return new Point2d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
        }
        private DBObjectCollection Get_grid_lines()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var engine = new ThAXISLineRecognitionEngine();
                engine.Recognize(acadDatabase.Database, new Point3dCollection());
                return engine.Elements.Select(o => o.Outline).ToCollection();
            }
        }
        private bool Is_vertical(Vector3d vec)
        {
            return Math.Abs(vec.DotProduct(Vector3d.XAxis)) < 1e-3;
        }
        private bool Is_horizontal(Vector3d vec)
        {
            return Math.Abs(vec.DotProduct(Vector3d.YAxis)) < 1e-3;
        }
    }
}