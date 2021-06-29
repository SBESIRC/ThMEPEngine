using System;
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
        private static double Get_width(string size)
        {
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException();
            return Double.Parse(width[0]);
        }
        private static double Get_height(string size)
        {
            string[] width = size.Split('x');
            if (width.Length != 2)
                throw new NotImplementedException();
            return Double.Parse(width[1]);
        }
        private static void Seperate_size_info(string size, out double width, out double height)
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
    }
}
