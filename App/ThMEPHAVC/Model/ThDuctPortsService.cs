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
        public static double Calc_duct_width(double air_vloume, double favorite_width, double in_speed, string scenario, ref string duct_size)
        {
            double width;
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
                    width = Get_width(size);
                    if (Math.Abs(width - favorite_width) < 1e-3)
                    {
                        duct_size = size;
                        return favorite_width;
                    }
                }
                width = Get_width(duct_info.DuctSizeInfor.RecommendOuterDuctSize);
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
        private static double Calc_air_speed(double air_vloume, string scenario)
        {
            if (scenario.Contains("消防") && !scenario.Contains("兼"))
            {
                if (air_vloume < 15000)
                    return 10;
                else if (air_vloume < 30000)
                    return 15;
                else
                    return 18;
            }
            else
            {
                if (air_vloume < 500)
                    return 3;
                else if (air_vloume < 1000)
                    return 3.5;
                else if (air_vloume < 1500)
                    return 4;
                else if (air_vloume < 2000)
                    return 4.5;
                else if (air_vloume < 2500)
                    return 5;
                else if (air_vloume < 3000)
                    return 5.5;
                else if (air_vloume < 5000)
                    return 6;
                else if (air_vloume < 10000)
                    return 6.5;
                else if (air_vloume < 15000)
                    return 7;
                else if (air_vloume < 20000)
                    return 7.5;
                else if (air_vloume < 25000)
                    return 8;
                else if (air_vloume < 30000)
                    return 9;
                else
                    return 10;
            }
        }
    }
}
