using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.FireAlarmFixLayout
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
      
        public static Dictionary<string, double> blk_move_length = new Dictionary<string, double>() {
                                                                    {BlkName_Display_Fire,3},
                                                                    {BlkName_Display_Floor, 3},
                                                                    {BlkName_Monitor, 3},
                                                                    {BlkName_FireTel, 3},
                                                                };

        public static Dictionary<string, string> blk_layer = new Dictionary<string, string>()
                                                                {
                                                                    {BlkName_Display_Fire,"E-FAS-DEVC"},
                                                                    {BlkName_Display_Floor, "E-FAS-DEVC"},
                                                                    {BlkName_Monitor, "E-FAS-DEVC"},
                                                                    {BlkName_FireTel, "E-FAS-DEVC"},
                                                                 };
    }
}
