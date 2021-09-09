using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.FireAlarm
{
    public class ThFixLayoutCommon
    {
        public static string Layer_Display = "E-FAS-DEVC";
        public static string Layer_Monitor = "E-FAS-DEVC";
        public static string Layer_FireTel = "E-FAS-DEVC";

        public static string BlkName_Display_Fire = "E-BFAS030";
        public static string BlkName_Display_Floor = "E-BFAS031";
        public static string BlkName_Monitor = "E-BEFPS110";
        public static string BlkName_FireTel = "E-BFAS220";
        public static List<string> BlkNameList = new List<string>() { BlkName_Display_Fire, BlkName_Display_Floor, BlkName_Monitor, BlkName_FireTel };
        
        public static int blk_scale = 100;
        public static double blk_display_size_x = 5;
        public static double blk_display_size_y = 3;


    }
}
