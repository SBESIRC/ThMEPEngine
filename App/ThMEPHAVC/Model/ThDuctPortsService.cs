using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;
using System.Text.RegularExpressions;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsService
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
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException();
            return Double.Parse(width[0]);
        }
        public static double Get_height(string size)
        {
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException();
            return Double.Parse(width[1]);
        }
        public static void Seperate_size_info(string size, out double width, out double height)
        {
            string[] s = size.Split('x');
            if (s.Length != 2)
                throw new NotImplementedException();
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
        public static Vector2d Get_dir_vec(double angle)
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
            return Is_equal(l.StartPoint.X, l.EndPoint.X);
        }
        public static bool Is_horizontal(Line l)
        {
            return Is_equal(l.StartPoint.Y, l.EndPoint.Y);
        }
        public static bool Is_outter(Vector2d v2_1, Vector2d v2_2)
        {
            var v1 = new Vector3d(v2_1.X, v2_1.Y, 0);
            var v2 = new Vector3d(v2_1.X, v2_1.Y, 0);
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
            var vertical_left = ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            pL = pos + dir_vec * (port_width * 0.5) + vertical_left * (duct_width * 0.5);
            var vertical_right = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            pR = pos - dir_vec * (port_width * 0.5) + vertical_right * (duct_width * 0.5);
        }
        public static double Get_port_rotate_angle(Vector3d dir_vec)
        {
            var judger = -Vector3d.YAxis;
            double angle = dir_vec.GetAngleTo(judger);
            if (judger.CrossProduct(dir_vec).Z < 0)
                angle = -angle;
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
                seperate_dis = 900;
            return seperate_dis;
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
        public static Duct_modify_param Create_duct_modify_param(Line_Info cur_seg, 
                                                                 string duct_size, 
                                                                 double air_volume,
                                                                 Handle start_handle)
        {
            if (cur_seg.center_line.Count > 0)
            {
                var center_line = cur_seg.center_line[0] as Line;
                var sp = center_line.StartPoint.ToPoint2D();
                var ep = center_line.EndPoint.ToPoint2D();
                return new Duct_modify_param(duct_size, air_volume, sp, ep, start_handle);
            }
            else
                throw new NotImplementedException();
        }
        public static Entity_modify_param Create_special_modify_param(Line_Info cur_seg,
                                                                      Handle start_handle,
                                                                      string type,
                                                                      Matrix3d mat)
        {
            var pos_list = new List<Point2d>();
            var pos_ext_list = new List<Point2d>();
            var port_widths = new List<double>();
            int inc = 0;
            for (int i = 0; i < cur_seg.center_line.Count; ++i)
            {
                var l = cur_seg.center_line[i];
                if (l.GetType() != typeof(Line))
                    continue;
                var line = l as Line;
                var dir_vec = Get_2D_edge_direction(line);
                var p = line.EndPoint.TransformBy(mat);
                var ep = p.ToPoint2D();
                pos_list.Add(ep);
                pos_ext_list.Add(ep - dir_vec);
                var flg = cur_seg.flg[inc++] as Line;
                port_widths.Add(flg.Length - 90);
            }
            return new Entity_modify_param(type, start_handle, pos_list, pos_ext_list, port_widths);
        }
        public static Entity_modify_param Create_reducing_modify_param(Line_Info cur_seg, Handle start_handle)
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
                return new Entity_modify_param("Reducing", start_handle, pos_list, pos_ext_list, ports_width);
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
        public static void Get_max(Point2d sp1, Point2d ep1, Point2d sp2, Point2d ep2, out Point2d p1, out Point2d p2)
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
            var vertical_p = ThDuctPortsService.Get_vertical_point(p, l);
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
            var vertical_p = ThDuctPortsService.Get_vertical_point(p, l);
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
        public static void Get_ports(Line l, out List<Point3d> ports, out List<Point3d> ports_ext)
        {
            var dir_vec = Get_edge_direction(l);
            ports = new List<Point3d>() { l.StartPoint, l.EndPoint };
            ports_ext = new List<Point3d>() { l.StartPoint + dir_vec, l.EndPoint - dir_vec };
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
    }
}