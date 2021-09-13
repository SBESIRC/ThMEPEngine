using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using ThMEPHVAC.CAD;

namespace TianHua.Hvac.UI
{
    class ThHvacUIService
    {
        public static void Limit_air_speed_range(string scenario, ref double air_speed, ref double air_speed_min, ref double air_speed_max)
        {
            switch (scenario)
            {
                case "消防排烟":
                case "消防补风":
                case "消防加压送风":
                    air_speed = 15;
                    air_speed_min = 5;
                    air_speed_max = 20;
                    break;
                case "厨房排油烟":
                case "厨房排油烟补风":
                case "事故排风":
                case "事故补风":
                case "平时送风":
                case "平时排风":
                case "消防排烟兼平时排风":
                case "消防补风兼平时送风":
                case "平时送风兼事故补风":
                case "平时排风兼事故排风":
                    air_speed = 8;
                    air_speed_min = 5;
                    air_speed_max = 10;
                    break;
                default:
                    throw new NotImplementedException("Check scenario!!!");
            }
        }
        public static void Port_init(string scenario, out string down_port_name, out string side_port_name)
        {
            switch (scenario)
            {
                case "消防排烟":
                case "厨房排油烟":
                case "平时排风":
                case "消防排烟兼平时排风":
                case "事故排风":
                case "平时排风兼事故排风":
                    down_port_name = "下回单层百叶";
                    side_port_name = "侧回单层百叶";
                    break;
                case "消防补风":
                case "消防加压送风":
                case "厨房排油烟补风":
                case "平时送风":
                case "消防补风兼平时送风":
                case "事故补风":
                case "平时送风兼事故补风":
                    down_port_name = "下送单层百叶";
                    side_port_name = "侧送单层百叶";
                    break;
                default:
                    throw new NotImplementedException("Check scenario!!!");
            }
        }
        public static double Calc_air_speed(double air_volume, double duct_width, double duct_height)
        {
            return air_volume / 3600 / (duct_width * duct_height / 1000000);
        }
        public static void Update_recommend_duct_size_list(ListBox listBox, double air_volume, double air_speed)
        {
            if (Math.Abs(air_speed) < 1e-3 || Math.Abs(air_volume) < 1e-3)
                return;
            var Duct = new ThDuctParameter(air_volume, air_speed, true);
            listBox.Items.Clear();
            foreach (var duct_size in Duct.DuctSizeInfor.DefaultDuctsSizeString)
                listBox.Items.Add(duct_size);
            listBox.SelectedItem = Duct.DuctSizeInfor.RecommendOuterDuctSize;
        }
        public static void Limit_air_speed(double ceiling,
                                           double floor,
                                           out bool is_high,
                                           ref double air_speed)
        {
            is_high = false;
            if (Math.Abs(air_speed) < 1e-3)
                return;
            if (air_speed > ceiling)
            {
                is_high = true;
                air_speed = ceiling;
            }
            if (air_speed < floor)
                air_speed = floor;
        }
        public static void Limit_air_volume(out bool is_high, ref double air_volume)
        {
            is_high = false;
            if (Math.Abs(air_volume) < 1e-3)
                return;
            double air_volume_floor = 1500;
            double air_volume_ceiling = 60000;
            if (air_volume > air_volume_ceiling)
            {
                is_high = true;
                air_volume = air_volume_ceiling;
            }
            if (air_volume < air_volume_floor)
                air_volume = air_volume_floor;
        }
        public static bool Is_float_2_decimal(string text)
        {
            string reg = "^[0-9]*[.]?[0-9]{0,2}$";
            return Regex.Match(text, reg).Success;
        }
        public static bool Is_integer_str(string text)
        {
            string reg = "^[0-9]*$";
            return Regex.Match(text, reg).Success;
        }
    }
}
