using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;
using ThCADCore.NTS;
using NetTopologySuite.Geometries;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.Model
{
    public class ThMEPHVACService
    {
        public static double Calc_duct_width(bool is_first, 
                                             double ui_air_speed, 
                                             double air_vloume, 
                                             ref string duct_size)
        {
            air_vloume = (air_vloume < 3000) ? 2800 : air_vloume;
            double speed = (is_first) ? ui_air_speed : Calc_air_speed(air_vloume, duct_size);
            double favorite_width = Get_width(duct_size);
            Get_air_speed_floor(air_vloume, out double floor);
            if (speed >= floor)
                return favorite_width;
            var duct_info = new ThDuctParameter(air_vloume, speed, is_first);
            double w = Search_equal_duct_size(duct_info, favorite_width, ref duct_size);
            if (w > 0)
                return w;
            w = Search_second_duct_size(duct_info, favorite_width, ref duct_size);
            if (w > 0)
                return w;
            double width = Get_width(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
            if (width > favorite_width)
                width = favorite_width;
            else
                duct_size = duct_info.DuctSizeInfor.RecommendOuterDuctSize;
            return width;
        }
        private static double Search_equal_duct_size(ThDuctParameter duct_info,
                                                     double favorite_width,
                                                     ref string duct_size)
        {
            double height = Get_height(duct_size);
            foreach (var size in duct_info.DuctSizeInfor.DefaultDuctsSizeString)
            {
                Seperate_size_info(size, out double cur_width, out double cur_height);
                if (cur_height > height)
                    continue;
                if (Is_equal(cur_width, favorite_width))
                {
                    duct_size = size;
                    return favorite_width;
                }
            }
            return 0;
        }
        private static double Search_second_duct_size(ThDuctParameter duct_info,
                                                      double favorite_width,
                                                      ref string duct_size)
        {
            double height = Get_height(duct_size);
            foreach (var size in duct_info.DuctSizeInfor.DefaultDuctsSizeString)
            {
                Seperate_size_info(size, out double cur_width, out double cur_height);
                if (cur_height > height)
                    continue;
                if (cur_width < favorite_width)
                {
                    duct_size = size;
                    return cur_width;
                }
            }
            return 0;
        }
        public static double Get_width(string size)
        {
            if (size == null)
                return 0;
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            return Double.Parse(width[0]);
        }
        public static double Get_height(string size)
        {
            if (size == null)
                return 0;
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            return Double.Parse(width[1]);
        }
        public static void Seperate_size_info(string size, out double width, out double height)
        {
            string[] s = size.Split('x');
            if (s.Length != 2)
                throw new NotImplementedException("Duct size info doesn't contain width or height");
            width = Double.Parse(s[0]);
            height = Double.Parse(s[1]);
        }
        private static double Calc_air_speed(double air_vloume, string duct_size)
        {
            Seperate_size_info(duct_size, out double width, out double height);
            return air_vloume / 3600 / (width * height / 1000000);
        }
        private static void Get_air_speed_floor(double air_vloume, out double floor)
        {
            if (air_vloume >= 26000)
                floor = 8;
            else if (air_vloume >= 12000)
                floor = 6;
            else if (air_vloume >= 8000)
                floor = 4.5;
            else if (air_vloume >= 4000)
                floor = 3.5;
            else if (air_vloume >= 3000)
                floor = 5.14;
            else if (air_vloume >= 2800)
                floor = 4.8;
            else
                floor = 3;
        }
        public static void Get_line_pos_info(Line l, out double angle, out Point3d center_point)
        {
            var srt_p = l.StartPoint;
            var end_p = l.EndPoint;
            var edge_vec = new Vector2d(end_p.X - srt_p.X, end_p.Y - srt_p.Y);
            angle = edge_vec.Angle;
            center_point = new Point3d(0.5 * (srt_p.X + end_p.X), 0.5 * (srt_p.Y + end_p.Y), 0);
        }
        public static Point2d Get_vertical_point(Point2d p, Line l)
        {
            var mirror = Get_mirror_point(p, l);
            return Get_mid_point(mirror, p);
        }
        public static double Get_point_to_line(Point2d p, Line l)
        {
            var mirror = Get_mirror_point(p, l);
            return mirror.GetDistanceTo(p) * 0.5;
        }
        public static Point2d Get_mirror_point(Point2d p, Line l)
        {
            return p.Mirror(new Line2d(l.StartPoint.ToPoint2D(), l.EndPoint.ToPoint2D()));
        }
        public static Point2d Get_mid_point(Point2d p1, Point2d p2)
        {
            return new Point2d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5);
        }
        public static Point3d Get_mid_point(Line l)
        {
            var sp = l.StartPoint;
            var ep = l.EndPoint;
            return new Point3d((sp.X + ep.X) * 0.5, (sp.Y + ep.Y) * 0.5, 0);
        }
        public static Point3d Get_mid_point(Point3d p1, Point3d p2)
        {
            return new Point3d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5, 0);
        }
        public static Vector3d Get_edge_direction(Line l)
        {
            var srt_p = l.StartPoint;
            var end_p = l.EndPoint;
            return (end_p - srt_p).GetNormal();
        }
        public static Vector2d Get_2D_edge_direction(Line l)
        {
            var srt_p = l.StartPoint.ToPoint2D();
            var end_p = l.EndPoint.ToPoint2D();
            return (end_p - srt_p).GetNormal();
        }
        public static Vector2d Get_2D_edge_direction(Line2d l)
        {
            return (l.EndPoint - l.StartPoint).GetNormal();
        }
        public static Vector3d Get_left_vertical_vec(Vector3d dir_vec)
        {
            return new Vector3d(-dir_vec.Y, dir_vec.X, 0);
        }
        public static Vector3d Get_right_vertical_vec(Vector3d dir_vec)
        {
            return new Vector3d(dir_vec.Y, -dir_vec.X, 0);
        }
        public static Vector2d Get_left_vertical_vec(Vector2d dir_vec)
        {
            return new Vector2d(-dir_vec.Y, dir_vec.X);
        }
        public static Vector2d Get_right_vertical_vec(Vector2d dir_vec)
        {
            return new Vector2d(dir_vec.Y, -dir_vec.X);
        }
        public static Vector3d Get_dir_vec_by_angle_3(double angle)
        {
            var v = Get_dir_vec_by_angle(angle);
            return new Vector3d(v.X, v.Y, 0);
        }
        public static Vector2d Get_dir_vec_by_angle(double angle)
        {
            return new Vector2d(Math.Cos(angle), Math.Sin(angle));
        }
        public static bool Is_between_points(Point3d p, Point3d p1, Point3d p2)
        {
            //判断直线上的三个点 其中某点是否在其他两个之间
            var v1 = (p - p1).GetNormal();
            var v2 = (p - p2).GetNormal();
            return Math.Abs(v1.GetAngleTo(v2)) > 1e-3;
        }
        public static bool Is_vertical(Vector3d v1, Vector3d v2)
        {
            return Math.Abs(v1.DotProduct(v2)) < 1e-3;
        }
        public static bool Is_vertical(Line l)
        {
            return Math.Abs(l.StartPoint.X - l.EndPoint.X) <= 1e-1;
        }
        public static bool Is_horizontal(Line l)
        {
            return Math.Abs(l.StartPoint.Y - l.EndPoint.Y) <= 1e-1;
        }
        public static bool Is_outter(Vector2d v2_1, Vector2d v2_2)
        {
            var v1 = new Vector3d(v2_1.X, v2_1.Y, 0);
            var v2 = new Vector3d(v2_2.X, v2_2.Y, 0);
            return Is_outter(v1, v2);
        }
        public static bool Is_outter(Vector3d v1, Vector3d v2)
        {
            return v1.CrossProduct(v2).Z > 0;
        }
        public static Point3d Get_down_port_insert_pos(Vector3d dir_vec, 
                                                       Point3d pos, 
                                                       double port_width, 
                                                       double port_height)
        {
            var vertical_right = Get_right_vertical_vec(dir_vec);
            var dis_vec = -dir_vec * port_height * 0.5 + vertical_right * port_width * 0.5;
            return pos + dis_vec;
        }
        public static void Get_side_port_insert_pos(Vector3d dir_vec, 
                                                    Point3d pos, 
                                                    double duct_width,
                                                    double port_width,
                                                    out Point3d pL, 
                                                    out Point3d pR)
        {
            var vertical_left = Get_left_vertical_vec(dir_vec);
            pL = pos + dir_vec * (port_width * 0.5) + vertical_left * (duct_width * 0.5);
            var vertical_right = Get_right_vertical_vec(dir_vec);
            pR = pos - dir_vec * (port_width * 0.5) + vertical_right * (duct_width * 0.5);
        }
        public static double Get_port_rotate_angle(Vector3d dir_vec)
        {
            var judger = -Vector3d.YAxis;
            double angle = dir_vec.GetAngleTo(judger);
            var z = judger.CrossProduct(dir_vec).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z < 0)
                angle = 2 * Math.PI - angle;
            return angle;
        }
        public static double Get_text_height(string scale)
        {
            double h = 450;
            if (scale == "1:100")
                h = 300;
            else if (scale == "1:50")
                h = 150;
            return h;
        }
        public static double Get_text_sep_dis(string scale)
        {
            double seperate_dis = 2500;
            if (scale == "1:100")
                seperate_dis = 1800;
            else if (scale == "1:50")
                seperate_dis = 1100;
            return seperate_dis - 400;
        }
        public static string Get_dim_style(string scale)
        {
            string style = "TH-DIM150";
            if (scale == "1:100")
                style = "TH-DIM100";
            else if (scale == "1:50")
                style = "TH-DIM50";
            return style;
        }
        public static DuctModifyParam Create_duct_modify_param(DBObjectCollection center_line,
                                                                 string duct_size,
                                                                 string elevation,
                                                                 double air_volume,
                                                                 Handle start_handle)
        {
            if (center_line.Count > 0)
            {
                var line = center_line[0] as Line;
                var sp = line.StartPoint.ToPoint2D();
                var ep = line.EndPoint.ToPoint2D();
                return new DuctModifyParam(duct_size, air_volume, Double.Parse(elevation), sp, ep, start_handle);
            }
            else
                throw new NotImplementedException();
        }
        public static DuctModifyParam Create_duct_modify_param(DBObjectCollection center_line,
                                                                 string duct_size,
                                                                 double air_volume,
                                                                 ThMEPHVACParam param,
                                                                 Handle start_handle)
        {
            if (center_line.Count > 0)
            {
                var line = center_line[0] as Line;
                var sp = line.StartPoint.ToPoint2D();
                var ep = line.EndPoint.ToPoint2D();
                var height = Get_height(duct_size);
                bool is_first = duct_size == param.in_duct_size;
                double elevation = is_first ? param.elevation : (param.elevation * 1000 + param.main_height - height) / 1000;
                return new DuctModifyParam(duct_size, air_volume, elevation, sp, ep, start_handle);
            }
            else
                throw new NotImplementedException();
        }
        public static EntityModifyParam Create_special_modify_param( string type,
                                                                       Matrix3d mat,
                                                                       Handle start_handle,
                                                                       DBObjectCollection flange,
                                                                       DBObjectCollection center_line)
        {
            var pos_list = new List<Point2d>();
            var pos_ext_list = new List<Point2d>();
            var port_widths = new List<double>();
            int inc = 0;
            for (int i = 0; i < center_line.Count; ++i)
            {
                var l = center_line[i];
                if (l.GetType() != typeof(Line))
                    continue;
                var line = l as Line;
                var dir_vec = Get_2D_edge_direction(line);
                var p = line.EndPoint.TransformBy(mat);
                var ep = p.ToPoint2D();
                pos_list.Add(ep);
                pos_ext_list.Add(ep - dir_vec);
                var flg = flange[inc++] as Line;
                port_widths.Add(flg.Length - 90);
            }
            return new EntityModifyParam(type, start_handle, pos_list, pos_ext_list, port_widths);
        }
        public static EntityModifyParam Create_reducing_modify_param(Line_Info cur_seg, Handle start_handle)
        {
            if (cur_seg.center_line.Count > 0)
            {
                var l = cur_seg.center_line[0] as Line;
                var dir_vec = Get_2D_edge_direction(l);
                var sp = l.StartPoint.ToPoint2D();
                var ep = l.EndPoint.ToPoint2D();
                var pos_list = new List<Point2d>() { sp, ep };
                var pos_ext_list = new List<Point2d>() { sp + dir_vec, ep - dir_vec };
                var l1 = cur_seg.flg[0] as Line;
                var l2 = cur_seg.flg[1] as Line;
                var ports_width = new List<double>() { l1.Length - 90, l2.Length - 90 };
                return new EntityModifyParam("Reducing", start_handle, pos_list, pos_ext_list, ports_width);
            }
            else
                throw new NotImplementedException();
        }
        public static Handle Covert_obj_to_handle(object o)
        {
            return new Handle(Convert.ToInt64((string)o, 16));
        }
        public static Point2d Covert_obj_to_point(object o)
        {
            string s = (string)o;
            string sub_str = s.Substring(1, s.Length - 2);
            string[] nums = sub_str.Split(',');
            if (nums.Length != 2)
                return Point2d.Origin;
            double X = Double.Parse(nums[0]);
            X = (Math.Abs(X) < 1e-3) ? 0 : X;
            double Y = Double.Parse(nums[1]);
            Y = (Math.Abs(Y) < 1e-3) ? 0 : Y;
            return new Point2d(X, Y);
        }
        public static bool Is_collinear(Vector2d vec1, Vector2d vec2)
        {
            var tor = new Tolerance(1e-3, 1e-3);
            return vec1.IsEqualTo(vec2, tor) || vec1.IsEqualTo(-vec2, tor);
        }
        public static bool Is_collinear(Vector3d vec1, Vector3d vec2)
        {
            var tor = new Tolerance(1e-3, 1e-3);
            return vec1.IsEqualTo(vec2, tor) || vec1.IsEqualTo(-vec2, tor);
        }
        public static Matrix3d Get_trans_mat(bool is_flip, double rotate_angle, Point2d center_point)
        {
            var trans = new Trans_info(is_flip, rotate_angle, center_point);
            return Get_trans_mat(trans);
        }
        public static Matrix3d Get_trans_mat(Trans_info trans)
        {
            var p = new Point3d(trans.center_point.X, trans.center_point.Y, 0);
            var mat = Matrix3d.Displacement(p.GetAsVector()) *
                      Matrix3d.Rotation(-trans.rotate_angle, Vector3d.ZAxis, Point3d.Origin);
            if (trans.is_flip)
                mat *= Matrix3d.Mirroring(new Line3d(Point3d.Origin, new Point3d(0, 1, 0)));
            return mat;
        }
        public static Line Get_shrink_line(ThDuctEdge<ThDuctVertex> edge)
        {
            var src_point = edge.Source.Position;
            var tar_point = edge.Target.Position;
            var dir_vec = (tar_point - src_point).GetNormal();
            var new_src_point = src_point + dir_vec * edge.SourceShrink;
            var new_tar_point = tar_point - dir_vec * edge.TargetShrink;
            return new Line(new_src_point, new_tar_point);
        }
        public static Vector3d Get_vertical_vec(Vector3d dir_vec)
        {
            Vector3d vertical_vec;
            if (Math.Abs(dir_vec.X) < 1e-3)
                vertical_vec = (dir_vec.Y > 0) ? Get_left_vertical_vec(dir_vec) : Get_right_vertical_vec(dir_vec);
            else if (dir_vec.X > 0)
                vertical_vec = Get_left_vertical_vec(dir_vec);
            else
                vertical_vec = Get_right_vertical_vec(dir_vec);
            return vertical_vec;
        }
        public static bool Is_same_line(Line l1, Line l2, Tolerance point_tor)
        {
            var sp1 = l1.StartPoint;
            var ep1 = l1.EndPoint;
            var sp2 = l2.StartPoint;
            var ep2 = l2.EndPoint;
            return ((sp1.IsEqualTo(sp2, point_tor) && ep1.IsEqualTo(ep2, point_tor)) ||
                    (sp1.IsEqualTo(ep2, point_tor) && ep1.IsEqualTo(sp2, point_tor)));
        }
        public static bool Is_same_line(Line l1, Point3d sp, Point3d ep, Tolerance point_tor)
        {
            var sp1 = l1.StartPoint;
            var ep1 = l1.EndPoint;
            return ((sp1.IsEqualTo(sp, point_tor) && ep1.IsEqualTo(ep, point_tor)) ||
                    (sp1.IsEqualTo(ep, point_tor) && ep1.IsEqualTo(sp, point_tor)));
        }
        public static bool Is_same_line(Point2d sp1, Point2d ep1, Point2d sp2, Point2d ep2, Tolerance point_tor)
        {
            return (sp1.IsEqualTo(sp2, point_tor) && ep1.IsEqualTo(ep2, point_tor));
        }
        public static void Get_longest_dis(Point3d sp1, Point3d ep1, Point3d sp2, Point3d ep2, out Point3d p1, out Point3d p2)
        {
            var sp_2D_1 = sp1.ToPoint2D();
            var ep_2D_1 = ep1.ToPoint2D();
            var sp_2D_2 = sp2.ToPoint2D();
            var ep_2D_2 = ep2.ToPoint2D();
            Get_longest_dis(sp_2D_1, ep_2D_1, sp_2D_2, ep_2D_2, out Point2d p_2D_1, out Point2d p_2D_2);
            p1 = new Point3d(p_2D_1.X, p_2D_1.Y, 0);
            p2 = new Point3d(p_2D_2.X, p_2D_2.Y, 0);
        }
        public static void Get_longest_dis(Point2d sp1, Point2d ep1, Point2d sp2, Point2d ep2, out Point2d p1, out Point2d p2)
        {
            double dis1 = sp1.GetDistanceTo(sp2);
            double dis2 = sp1.GetDistanceTo(ep2);
            double dis3 = ep1.GetDistanceTo(sp2);
            double dis4 = ep1.GetDistanceTo(ep2);
            double[] a = { dis1, dis2, dis3, dis4 };
            double max = a[0];
            int max_idx = 0;
            for (int i = 1; i < 4; ++i)
            {
                if (max < a[i])
                {
                    max_idx = i;
                    max = a[i];
                }
            }
            switch (max_idx)
            {
                case 0: p1 = sp1; p2 = sp2; break;
                case 1: p1 = sp1; p2 = ep2; break;
                case 2: p1 = ep1; p2 = sp2; break;
                case 3: p1 = ep1; p2 = ep2; break;
                default: throw new NotImplementedException();
            }
        }
        public static Line Extend_line(Line l, double dis)
        {
            var dir_vec = Get_edge_direction(l);
            var dis_vec = dis * dir_vec;
            var sp = l.StartPoint - dis_vec;
            var ep = l.EndPoint + dis_vec;
            return new Line(sp, ep);
        }
        public static double Align_distance(double dis, double multiple)
        {
            return (Math.Ceiling(dis / multiple)) * multiple;
        }
        public static bool Is_in_mirror_range(Point2d p, Line l)
        {
            var vertical_p = Get_vertical_point(p, l);
            return Mid_point_is_in_line(vertical_p, l);
        }
        private static bool Mid_point_is_in_line(Point2d p, Line l)
        {
            double maxX = Math.Max(l.StartPoint.X, l.EndPoint.X);
            double maxY = Math.Max(l.StartPoint.Y, l.EndPoint.Y);
            double minX = Math.Min(l.StartPoint.X, l.EndPoint.X);
            double minY = Math.Min(l.StartPoint.Y, l.EndPoint.Y);
            if (minX <= p.X && p.X <= maxX && minY <= p.Y && p.Y <= maxY)
                return true;
            return false;
        }
        public static double Point_to_line(Point2d p, Line l)
        {
            var vertical_p = Get_vertical_point(p, l);
            return vertical_p.GetDistanceTo(p);
        }
        public static bool Is_equal(double a, double b)
        {
            return Math.Abs(a - b) <= 1e-3;
        }
        public static void Get_ports(List<Line> lines, out List<Point3d> ports, out List<Point3d> ports_ext)
        {
            ports = new List<Point3d>();
            ports_ext = new List<Point3d>();
            foreach (var l in lines)
            {
                var dir_vec = Get_edge_direction(l);
                ports.Add(l.EndPoint);
                ports_ext.Add(l.EndPoint - dir_vec);
            }
        }
        public static void Get_duct_ports(Line l, out List<Point3d> ports, out List<Point3d> ports_ext)
        {
            var dir_vec = Get_edge_direction(l);
            ports = new List<Point3d>() { l.StartPoint, l.EndPoint };
            ports_ext = new List<Point3d>() { l.StartPoint + dir_vec, l.EndPoint - dir_vec };
        }
        public static void Get_elbow_ports(DBObjectCollection center_lines, out List<Point3d> ports, out List<Point3d> ports_ext)
        {
            ports = new List<Point3d>();
            ports_ext = new List<Point3d>();
            foreach (var e in center_lines)
            {
                if (e is Line)
                {
                    var l = e as Line;
                    var dir_vec = Get_edge_direction(l);
                    ports.Add(l.EndPoint);
                    ports_ext.Add(l.EndPoint - dir_vec);
                }
            }
        }
        public static Vector2d Vec3_to_vec2(Vector3d vec)
        {
            return new Vector2d(vec.X, vec.Y);
        }
        public static double Extract_decimal(string s)
        {
            s = Regex.Replace(s, @"[^\d.\d]", "");
            if (Regex.IsMatch(s, @"^[+-]?\d*[.]?\d*$"))
                return Double.Parse(s);
            throw new NotImplementedException();
        }
        public static void Prompt_msg(string message)
        {
            Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(message);
        }
        public static Line Covert_duct_to_line(DuctModifyParam param)
        {
            var sp_2 = param.sp;
            var ep_2 = param.ep;
            var sp = new Point3d(sp_2.X, sp_2.Y, 0);
            var ep = new Point3d(ep_2.X, ep_2.Y, 0);
            return new Line(sp, ep);
        }
        public static Point3d Get_max_point(Point3d p1, Point3d p2)
        {
            double max_x = Math.Max(p1.X, p2.X);
            double max_y = Math.Max(p1.Y, p2.Y);
            return new Point3d(max_x, max_y, 0);
        }
        public static Point3d Get_min_point(Point3d p1, Point3d p2)
        {
            double min_x = Math.Min(p1.X, p2.X);
            double min_y = Math.Min(p1.Y, p2.Y);
            return new Point3d(min_x, min_y, 0);
        }
        public static Polyline Get_line_extend(Point2d sp, Point2d ep, double ext_len)
        {
            var p1 = new Point3d(sp.X, sp.Y, 0);
            var p2 = new Point3d(ep.X, ep.Y, 0);
            var l = new Line(p1, p2);
            return l.Buffer(ext_len * 0.5);
        }
        public static Polyline Get_line_extend(Line l, double ext_len)
        {
            return l.Buffer(ext_len * 0.5);
        }
        public static Point3d Round_point(Point3d p, int tail_num)
        {
            var X = Math.Abs(p.X) < 1e-3 ? 0 : p.X;
            var Y = Math.Abs(p.Y) < 1e-3 ? 0 : p.Y;
            return new Point3d(Math.Round(X, tail_num), Math.Round(Y, tail_num), 0);
        }
        public static Point2d Round_point(Point2d p, int tail_num)
        {
            double X = Math.Round(p.X, tail_num);
            if (Math.Abs(X) < 1e-3)
                X = 0;
            double Y = Math.Round(p.Y, tail_num);
            if (Math.Abs(Y) < 1e-3)
                Y = 0;
            return new Point2d(X, Y);
        }
        public static Polyline Create_detect_poly(Point3d p)
        {
            var poly = new Polyline();
            poly.CreatePolygon(p.ToPoint2D(), 4, 10);
            return poly;
        }
        public static Polyline Create_detect_poly(Point3d p, double len)
        {
            var poly = new Polyline();
            poly.CreatePolygon(p.ToPoint2D(), 4, len);
            return poly;
        }
        public static bool Is_point_in_left_side(Line l, Point3d p)
        {
            var dir_vec = Get_edge_direction(l);
            var vec = (p - l.StartPoint).GetNormal();
            return dir_vec.CrossProduct(vec).Z > 0;
        }
        public static void Search_poly_border(DBObjectCollection lines, out Point2d top, out Point2d left, out Point2d right, out Point2d bottom)
        {
            top = new Point2d(0, Double.MinValue);
            left = new Point2d(Double.MaxValue, 0);
            right = new Point2d(Double.MinValue, 0);
            bottom = new Point2d(0, Double.MaxValue);
            foreach (Line l in lines)
            {
                Update_border(l.StartPoint.ToPoint2D(), ref top, ref left, ref right, ref bottom);
                Update_border(l.EndPoint.ToPoint2D(), ref top, ref left, ref right, ref bottom);
            }
        }
        private static void Update_border(Point2d p, ref Point2d top, ref Point2d left, ref Point2d right, ref Point2d bottom)
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
        public static void Search_poly_border(DBObjectCollection lines, out Fence_Info fence)
        {
            fence = new Fence_Info();
            foreach (Line l in lines)
            {
                Update_border(l.StartPoint.ToPoint2D(), ref fence);
                Update_border(l.EndPoint.ToPoint2D(), ref fence);
            }
        }
        private static void Update_border(Point2d p, ref Fence_Info fence)
        {
            if (p.X > fence.right)
                fence.right = p.X;
            if (p.X < fence.left)
                fence.left = p.X;
            if (p.Y > fence.top)
                fence.top = p.Y;
            if (p.Y < fence.bottom)
                fence.bottom = p.Y;
        }
        public static bool Is_in_fence(Point3d p, Fence_Info fence)
        {
            return (p.X > fence.left && p.X < fence.right) && (p.Y > fence.bottom && p.Y < fence.top);
        }
        public static bool Is_connected(Line l1, Line l2, Tolerance tor)
        {
            return l1.StartPoint.IsEqualTo(l2.StartPoint, tor) || l1.StartPoint.IsEqualTo(l2.EndPoint, tor) ||
                   l1.EndPoint.IsEqualTo(l2.StartPoint, tor) || l1.EndPoint.IsEqualTo(l2.EndPoint, tor);
        }
        public static Point3d Line_set_with_one_intersection(DBObjectCollection set1, 
                                                             DBObjectCollection set2,
                                                             out Line cross1,
                                                             out Line cross2)
        {
            cross1 = new Line();
            cross2 = new Line();
            var tor = new Tolerance(1.5, 1.5);
            foreach (Line l1 in set1)
            {
                foreach (Line l2 in set2)
                {
                    var p = Intersect_point(l1, l2);
                    if (!p.IsEqualTo(Point3d.Origin, tor))
                    {
                        cross1 = l1;
                        cross2 = l2;
                        return p;
                    }
                }
            }
            return Point3d.Origin;
        }
        public static Point3d Intersect_point(Line l1, Line l2)
        {
            var vec1 = l1.EndPoint - l1.StartPoint;
            var vec2 = l2.EndPoint - l2.StartPoint;
            var cross_x = l2.StartPoint.X - l1.StartPoint.X;
            var cross_y = l2.StartPoint.Y - l1.StartPoint.Y;
            var det = vec2.X * vec1.Y - vec2.Y * vec1.X;
            if (Math.Abs(det) < 1e-9)
                return Point3d.Origin;//The two line segmenta are parallel 
            var det_inv = 1.0f / det;
            var S = (vec2.X * cross_y - vec2.Y * cross_x) * det_inv;
            var T = (vec1.X * cross_y - vec1.Y * cross_x) * det_inv;
            if (Math.Abs(S) < 1e-9)
                S = 0;
            if (Math.Abs(T) < 1e-9)
                T = 0;
            if (Math.Abs(S - 1) < 1e-8)
                S = 1;
            if (Math.Abs(T - 1) < 1e-8)
                T = 1;
            if (S < 0 || S > 1 || T < 0 || T > 1)
                return Point3d.Origin;//Intersection not within line segments
            else
                return l1.StartPoint + vec1 * S;
        }
        public static bool Is_out_polyline(DBObjectCollection lines, DBObjectCollection fence)
        {
            if (lines.Count > 0)
            {
                var first = lines[0] as Line;
                var is_out = !Is_in_polyline(first.StartPoint, fence);
                foreach (Line l in lines)
                    is_out = is_out && (!Is_in_polyline(l.EndPoint, fence));
                return is_out;
            }
            return false;
        }
        public static bool Is_in_polyline(Point3d p, DBObjectCollection lines)
        {
            var shadow = new DBObjectCollection();
            foreach (Line obj in lines)
                shadow.Add(obj);
            var pts = new Point3dCollection();
            var search_p = (shadow[0] as Line).StartPoint;
            pts.Add(search_p);
            while (shadow.Count > 0)
            {
                var cur_l = Search_p(search_p, shadow, out Point3d other_p);
                pts.Add(other_p);
                shadow.Remove(cur_l);
                search_p = other_p;
            }
            var pl = new Polyline();
            pl.CreatePolyline(pts);
            return pl.Contains(p);
        }
        private static Line Search_p(Point3d p, DBObjectCollection lines, out Point3d other_p)
        {
            var tor = new Tolerance(1.5, 1.5);
            foreach (Line l in lines)
            {
                if (p.IsEqualTo(l.StartPoint, tor) || p.IsEqualTo(l.EndPoint, tor))
                {
                    other_p = p.IsEqualTo(l.StartPoint, tor) ? l.EndPoint : l.StartPoint;
                    return l;
                }
            }
            throw new NotImplementedException("search point not belongs wall lines");
        }
        public static double Get_elbow_open_angle(Special_graph_Info info)
        {
            var l1 = info.lines[0];
            var l2 = info.lines[1];
            var v1 = Get_2D_edge_direction(l1);
            var v2 = Get_2D_edge_direction(l2);
            return v1.GetAngleTo(v2);
        }
        public static double Get_reducing_len(double big, double small)
        {
            double reducinglength = 0.5 * (big - small) / Math.Tan(20 * Math.PI / 180);
            return reducinglength < 200 ? 200 : reducinglength > 1000 ? 1000 : reducinglength;
        }
        public static double Get_line_dis(Point3d l1_sp, Point3d l1_ep, Point3d l2_sp, Point3d l2_ep)
        {
            var coordinate1 = new Coordinate[] { l1_sp.ToNTSCoordinate(), l1_ep.ToNTSCoordinate() };
            var coordinate2 = new Coordinate[] { l2_sp.ToNTSCoordinate(), l2_ep.ToNTSCoordinate() };
            var l1 = new LineString(coordinate1);
            var l2 = new LineString(coordinate2);
            return l1.Distance(l2);
        }
        public static Line Get_max_line(DBObjectCollection lines)
        {
            var max_line = new Line();
            double max_len = 0;
            foreach (Line l in lines)
            {
                if (max_len < l.Length)
                {
                    max_len = l.Length;
                    max_line = l;
                }
            }
            return max_line;
        }
        public static Polyline Create_rect(Point2d p, Vector2d dir_vec, double width, double height)
        {
            var l_vec = Get_left_vertical_vec(dir_vec);
            var r_vec = Get_right_vertical_vec(dir_vec);
            var w = 0.5 * width;
            var h = 0.5 * height;
            var min_p = p - dir_vec * w + r_vec * h;
            var max_p = p + dir_vec * w + l_vec * h;
            var poly = new Polyline();
            poly.CreateRectangle(min_p, max_p);
            return poly;
        }
        public static double Get_srt_flag_rotation(Vector3d vec)
        {
            var angle = vec.GetAngleTo(Vector3d.YAxis);
            var z = vec.CrossProduct(Vector3d.YAxis).Z;
            if (Math.Abs(z) < 1e-3)
                z = 0;
            if (z > 0)
                angle = 2 * Math.PI - angle;
            return angle;
        }
    }
}