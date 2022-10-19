using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPHVAC.FanConnect
{
    public class ThFanConnectCommon
    {
        public static int Tol_SamePoint = 1;
        public static double Tol_LineToFan = 400.0;
        public static double MoveLength = 300.0;
        public static string BlkVisibility_Turn = "翻转状态";

        public static string BlkName_PipeDim2 = "AI-水管多排标注(2排)";
        public static string BlkName_PipeDim4 = "AI-水管多排标注(4排)";
        public static string BlkName_PipeDimPre = "AI-水管多排标注";
        public static string BlkName_PipeDim2_NoH = "AI-水管多排标注(2排)(无标高)";
        public static string BlkName_PipeDim4_NoH = "AI-水管多排标注(4排)(无标注)";

        public static string ACPipeConfigFileName = "大金VRV冷媒管管径.xlsx";
    }
}
