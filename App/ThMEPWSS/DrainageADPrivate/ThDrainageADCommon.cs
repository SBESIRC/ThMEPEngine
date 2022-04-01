using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.DrainageADPrivate
{
    internal class ThDrainageADCommon
    {
        public static string Layer_EQPM_D = "W-DRAI-EQPM";
        public static string Layer_EQPM = "W-WSUP-EQPM";

        public static string Layer_DIMS_D = "W-DRAI-DIMS";
        public static string Layer_DIMS = "W-WSUP-DIMS";

        public static List<double> Radius_Vertical = new List<double>() { 25, 50, 100, 150 };
        public static string Layer_HotPipe = "W-WSUP-HOT-PIPE";
        public static string Layer_CoolPipe = "W-WSUP-COOL-PIPE";
        public static string Layer_HotIPipe = "W-WSUP-HOTI-PIPE";
        public static string Layer_HotRPipe = "W-WSUP-HOTR-PIPE";
        public static string BlkName_WaterHeater = "燃气热水器";
        public static string BlkName_AngleValve = "给水角阀平面";
        public static List<string> BlkName_TchValve = new List<string> { "截止阀", "闸阀", "止回阀", "防污隔断阀" };

        public enum TerminalType
        {
            Toilet,  //坐便器
            Washbasin,//洗手台
            WashbasinDouble,//双盆洗手台
            Sink,   //洗涤盆
            Shower, //淋浴器
            WashingMachine,//洗衣机
            BalconyWashBasin, //阳台洗手盆
            MopSink, //拖把池
            Bathtub,//浴缸
            WaterHeater,//热水器
            Unknow,
        }

        public static Dictionary<int, string> TerminalChineseName = new Dictionary<int, string>(){
                                                            {(int) TerminalType.Toilet , "坐便器" },
                                                            {(int) TerminalType.Washbasin , "单盆洗手台" },
                                                            {(int) TerminalType.WashbasinDouble  , "双盆洗手台" },
                                                            {(int) TerminalType.Sink , "厨房洗涤盆" },
                                                            {(int) TerminalType.Shower , "淋浴器" },
                                                            {(int) TerminalType.WashingMachine , "洗衣机" },
                                                            {(int) TerminalType.BalconyWashBasin , "阳台洗手盆" },
                                                            {(int) TerminalType.MopSink , "拖把池" },
                                                            {(int) TerminalType.Bathtub , "浴缸" },
                                                        };
    }
}
