using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPHVAC.CAD;

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
                if (Math.Abs(cur_width - favorite_width) < 1e-3)
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
            Point3d srt_p = l.StartPoint;
            Point3d end_p = l.EndPoint;
            Vector2d edge_vec = new Vector2d(end_p.X - srt_p.X, end_p.Y - srt_p.Y);
            angle = edge_vec.Angle;
            center_point = new Point3d(0.5 * (srt_p.X + end_p.X), 0.5 * (srt_p.Y + end_p.Y), 0);
        }
        public static Point3d Get_mid_point(Point3d p1, Point3d p2)
        {
            return new Point3d((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5, 0);
        }
        public static Vector3d Get_edge_direction(Line l)
        {
            Point3d srt_p = l.StartPoint;
            Point3d end_p = l.EndPoint;
            return (end_p - srt_p).GetNormal();
        }
        public static Vector2d Get_2D_edge_direction(Line l)
        {
            Point2d srt_p = l.StartPoint.ToPoint2D();
            Point2d end_p = l.EndPoint.ToPoint2D();
            return (end_p - srt_p).GetNormal();
        }
        public static Vector3d Get_left_vertical_vec(Vector3d dir_vec)
        {
            return new Vector3d(-dir_vec.Y, dir_vec.X, 0);
        }
        public static Vector3d Get_right_vertical_vec(Vector3d dir_vec)
        {
            return new Vector3d(dir_vec.Y, -dir_vec.X, 0);
        }
        public static bool Is_between_points(Point3d p, Point3d p1, Point3d p2)
        {
            //判断直线上的三个点 其中某点是否在其他两个之间
            Vector3d v1 = (p - p1).GetNormal();
            Vector3d v2 = (p - p2).GetNormal();
            return Math.Abs(v1.GetAngleTo(v2)) > 1e-3;
        }
        public static bool Is_vertical(Vector3d v1, Vector3d v2)
        {
            return Math.Abs(v1.DotProduct(v2)) < 1e-1;
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
            Vector3d vertical_right = Get_right_vertical_vec(dir_vec);
            Vector3d dis_vec = -dir_vec * port_height * 0.5 + vertical_right * port_width * 0.5;
            return pos + dis_vec;
        }
        public static void Get_side_port_insert_pos(Vector3d dir_vec, 
                                                    Point3d pos, 
                                                    double duct_width,
                                                    double port_width,
                                                    out Point3d pL, 
                                                    out Point3d pR)
        {
            Vector3d vertical_left = ThDuctPortsService.Get_left_vertical_vec(dir_vec);
            pL = pos + dir_vec * (port_width * 0.5) + vertical_left * (duct_width * 0.5);
            Vector3d vertical_right = ThDuctPortsService.Get_right_vertical_vec(dir_vec);
            pR = pos - dir_vec * (port_width * 0.5) + vertical_right * (duct_width * 0.5);
        }
        public static double Get_port_rotate_angle(Vector3d dir_vec)
        {
            Vector3d judger = -Vector3d.YAxis;
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
            double seperate_dis = 500;
            if (scale == "1:100")
                seperate_dis = 300;
            else if (scale == "1:50")
                seperate_dis = 100;
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
                                                                 ObjectId start_id)
        {
            var center_line = cur_seg.center_line[0] as Line;
            var dir_vec = Get_2D_edge_direction(center_line);
            Point2d sp = center_line.StartPoint.ToPoint2D();
            Point2d ep = center_line.EndPoint.ToPoint2D();
            double leave_dis = 10;
            var pos_list = new List<Point2d>() { sp, ep };
            var pos_ext_list = new List<Point2d>() { sp + leave_dis * dir_vec, ep - leave_dis * dir_vec };
            var param = new Entity_modify_param(start_id, pos_list, pos_ext_list);
            return new Duct_modify_param(duct_size, air_volume, param);
        }
        public static Entity_modify_param Create_special_modify_param(Line_Info cur_seg, ObjectId start_id)
        {
            double leave_dis = 10;
            var pos_list = new List<Point2d>();
            var pos_ext_list = new List<Point2d>();
            foreach (var l in cur_seg.center_line)
            {
                if (l.GetType() != typeof(Line))
                    continue;
                var line = l as Line;
                var dir_vec = Get_2D_edge_direction(line);
                Point2d ep = line.EndPoint.ToPoint2D();
                pos_list.Add(ep);
                pos_ext_list.Add(ep - dir_vec * leave_dis);
            }
            return new Entity_modify_param(start_id, pos_list, pos_ext_list);
        }
    }
}