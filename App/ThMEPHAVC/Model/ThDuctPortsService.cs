using System;
using ThMEPHVAC.CAD;

namespace ThMEPHVAC.Model
{
    public class ThDuctPortsService
    {
        public static double Calc_duct_width(double air_vloume, double favorite_width, string scenario)
        {
            double width;
            double air_speed = Calc_air_speed(air_vloume, scenario);
            if (air_speed < 1e-3)
                throw new NotImplementedException();
            var duct_info = new ThDuctParameter(air_vloume, air_speed);
            if (Math.Abs(favorite_width) < 1e-3)
                return Get_width(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
            else
            {
                foreach (var size in duct_info.DuctSizeInfor.DefaultDuctsSizeString)
                {
                    width = Get_width(size);
                    if (Math.Abs(width - favorite_width) < 1e-3)
                        return favorite_width;
                }
                width = Get_width(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
                if (width > favorite_width)
                    width = favorite_width;
                return width;
            }
        }
        public static double Calc_directed_duct_width(double pre_air_vloume,
                                                      double cur_air_vloume,
                                                      double favorite_width, 
                                                      ref string duct_size)
        {
            double cur_height = Get_height(duct_size);
            Get_air_speed_range(cur_air_vloume, out double ceiling, out double floor);
            if (!Is_duct_size_change(pre_air_vloume, cur_air_vloume))
                return favorite_width;
            else
            {
                var duct_info = new ThDuctParameter(cur_air_vloume, ceiling, floor);
                foreach (var size in duct_info.DuctSizeInfor.DefaultDuctsSizeString)
                {
                    Seperate_size_info(size, out double width, out double height);
                    if (height > cur_height)
                        continue;
                    if (Math.Abs(width - favorite_width) < 1e-3)
                    {
                        duct_size = size;
                        return favorite_width;
                    }
                }
                double duct_width = Get_width(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
                double duct_height = Get_height(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
                if (duct_height > cur_height)
                    return favorite_width;
                if (duct_width > favorite_width)
                    duct_width = favorite_width;
                else
                    duct_size = duct_info.DuctSizeInfor.RecommendOuterDuctSize;
                return duct_width;
            }
        }
        public static double Calc_duct_width(double air_vloume, double favorite_width, double in_speed, string scenario, ref string duct_size)
        {
            double air_speed = (Math.Abs(in_speed) < 1e-3) ? Calc_air_speed(air_vloume, scenario) : in_speed;
            if (air_speed < 1e-3)
                throw new NotImplementedException();
            var duct_info = new ThDuctParameter(air_vloume, air_speed);
            if (Math.Abs(favorite_width) < 1e-3)
            {
                duct_size = duct_info.DuctSizeInfor.RecommendOuterDuctSize;
                return Get_width(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
            }
            else
            {
                foreach (var size in duct_info.DuctSizeInfor.DefaultDuctsSizeString)
                {
                    double cur_width = Get_width(size);
                    if (Math.Abs(cur_width - favorite_width) < 1e-3)
                    {
                        duct_size = size;
                        return favorite_width;
                    }
                }
                double width = Get_width(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
                if (width > favorite_width)
                    width = favorite_width;
                else
                    duct_size = duct_info.DuctSizeInfor.RecommendOuterDuctSize;
                return width;
            }
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
        private static bool Is_duct_size_change(double pre_air_vloume, double cur_air_vloume)
        {
            return ((pre_air_vloume >= 26000 && cur_air_vloume < 26000) ||
                    (pre_air_vloume >= 12000 && cur_air_vloume < 12000) ||
                    (pre_air_vloume >= 8000 && cur_air_vloume < 8000));
        }
        private static void Get_air_speed_range(double air_vloume, out double ceiling, out double floor)
        {
            ceiling = floor = 0;
            if (air_vloume >= 26000)
            {
                ceiling = 10;
                floor = 8;
            }
            else if (air_vloume >= 12000)
            {
                ceiling = 8;
                floor = 6;
            }
            else if (air_vloume >= 8000)
            {
                ceiling = 6;
                floor = 4.5;
            }
            else if (air_vloume >= 1000)
            {
                ceiling = 4.5;
                floor = 3.5;
            }
        }
        private static double Calc_air_speed(double air_vloume, string scenario)
        {
            if (scenario.Contains("消防") && !scenario.Contains("兼"))
            {
                if (air_vloume <= 15000)
                    return 10;
                else if (air_vloume <= 30000)
                    return 15;
                else
                    return 18;
            }
            else
            {
                if (air_vloume <= 500)
                    return 3;
                else if (air_vloume <= 1000)
                    return 3.5;
                else if (air_vloume <= 1500)
                    return 4;
                else if (air_vloume <= 2000)
                    return 4.5;
                else if (air_vloume <= 2500)
                    return 5;
                else if (air_vloume <= 3000)
                    return 5.5;
                else if (air_vloume <= 5000)
                    return 6;
                else if (air_vloume <= 10000)
                    return 6.5;
                else if (air_vloume <= 15000)
                    return 7;
                else if (air_vloume <= 20000)
                    return 7.5;
                else if (air_vloume <= 25000)
                    return 8;
                else if (air_vloume <= 30000)
                    return 9;
                else
                    return 10;
            }
        }
    }
}
