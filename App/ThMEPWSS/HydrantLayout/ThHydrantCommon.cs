using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.HydrantLayout
{
    internal class ThHydrantCommon
    {
        public static string Layer_Vertical = "W-FRPT-HYDT-EQPM";
        public const string Layer_Warning_TooFar = "AI-过远提示";
        public const string Layer_Warning_NotDo = "AI-没做提示";
        public static List<double> Radius_Vertical = new List<double>() { 100 / 2, 150 / 2 };
        public static string BlkName_Vertical = "带定位立管";
        public static string BlkName_Vertical150 = "带定位立管150";
        public const string BlkName_Hydrant = "室内消火栓平面";
        public const string BlkName_Hydrant_Extinguisher = "手提式灭火器";

        public const string BlkVisibility_Att = "可见性";
        public const string BlkVisibility_Att_1 = "可见性1";
        public const string BlkVisibility_Turn = "翻转";
        public const string BlkVisibility_Att_1_Value = "DN65";
        public const string BlkVisibility_Angle = "角度1";

        public const double DistTol = 1500;
     
    }
}
