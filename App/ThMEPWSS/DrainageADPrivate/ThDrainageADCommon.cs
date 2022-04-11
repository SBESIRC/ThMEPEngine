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
        public static string BlkName_WaterHeater = "热水器标注";
        public static string BlkName_AngleValve = "给水角阀平面";
        public static List<string> BlkName_TchValve = new List<string> { "截止阀", "闸阀", "止回阀", "防污隔断阀" };

        public enum TerminalType
        {
            Toilet,  //坐便器
            Washbasin,//洗手台
            //WashbasinDouble,//双盆洗手台
            Sink,   //洗涤盆
            Shower, //淋浴器
            WashingMachine,//洗衣机
            //BalconyWashBasin, //阳台洗手盆
            MopSink, //拖把池
            Bathtub,//浴缸
            WaterHeater,//热水器
            Unknow,
        }

        public static Dictionary<int, string> TerminalChineseName = new Dictionary<int, string>(){
                                                            {(int) TerminalType.Toilet , "坐便器" },
                                                            {(int) TerminalType.Washbasin , "单盆洗手台" },
                                                            //{(int) TerminalType.WashbasinDouble  , "双盆洗手台" },
                                                            {(int) TerminalType.Sink , "厨房洗涤盆" },
                                                            {(int) TerminalType.Shower , "淋浴器" },
                                                            {(int) TerminalType.WashingMachine , "洗衣机" },
                                                            //{(int) TerminalType.BalconyWashBasin , "阳台洗手盆" },
                                                            {(int) TerminalType.MopSink , "拖把池" },
                                                            {(int) TerminalType.Bathtub , "浴缸" },
                                                        };

        public static Dictionary<int, double> TerminalFixtureUnitCoolHot = new Dictionary<int, double>()
                                                        {
                                                            {(int) TerminalType.Toilet ,  0.5},
                                                            {(int) TerminalType.Washbasin , 0.75 },
                                                            //{(int) TerminalType.WashbasinDouble  , 1 },
                                                            {(int) TerminalType.Sink , 1},
                                                            {(int) TerminalType.Shower , 0.75 },
                                                            {(int) TerminalType.WashingMachine , 1 },
                                                            //{(int) TerminalType.BalconyWashBasin , 0.75 },
                                                            {(int) TerminalType.MopSink , 1 },
                                                            {(int) TerminalType.Bathtub ,1.2 },
                                                            {(int) TerminalType.WaterHeater , 0},
                                                            {(int) TerminalType.Unknow  , 0 },
                                                        };

        public static Dictionary<int, double> TerminalFixtureUnitCool = new Dictionary<int, double>()
                                                            {
                                                                 {(int) TerminalType.Toilet ,  0.5},
                                                                {(int) TerminalType.Washbasin , 0.5 },
                                                                //{(int) TerminalType.WashbasinDouble  , 1 },
                                                                {(int) TerminalType.Sink , 0.7},
                                                                {(int) TerminalType.Shower , 0.5 },
                                                                {(int) TerminalType.WashingMachine , 1 },
                                                                //(int) TerminalType.BalconyWashBasin , 0.5 },
                                                                {(int) TerminalType.MopSink , 1 },
                                                                {(int) TerminalType.Bathtub ,1},
                                                                {(int) TerminalType.WaterHeater , 0},
                                                                {(int) TerminalType.Unknow  , 0 },
                                                            };

        public static Dictionary<int, double> TerminalFixtureUnitHot = new Dictionary<int, double>()
                                                            {
                                                                {(int) TerminalType.Toilet ,  0},
                                                                {(int) TerminalType.Washbasin , 0.5 },
                                                                //{(int) TerminalType.WashbasinDouble  , 1 },
                                                                {(int) TerminalType.Sink , 0.7},
                                                                {(int) TerminalType.Shower , 0.5 },
                                                                {(int) TerminalType.WashingMachine , 0 },
                                                                //(int) TerminalType.BalconyWashBasin , 0.5 },
                                                                {(int) TerminalType.MopSink , 0 },
                                                                {(int) TerminalType.Bathtub ,1},
                                                                {(int) TerminalType.WaterHeater , 0},
                                                                {(int) TerminalType.Unknow  , 0 },
                                                            };

        public static List<Tuple<double, double>> AlphaList = new List<Tuple<double, double>>()
                                {
                                    new Tuple <double,double>(1,0.00323),
                                    new Tuple <double,double>(1.5, 0.00697),
                                    new Tuple <double,double>(2.0, 0.01097),
                                    new Tuple <double,double>(2.5, 0.01512),
                                    new Tuple <double,double>(3.0, 0.01939),
                                    new Tuple <double,double>(3.5, 0.02374),
                                    new Tuple <double,double>(4.0, 0.02816),
                                    new Tuple <double,double>(4.5, 0.03263),
                                    new Tuple <double,double>(5.0, 0.03715),
                                    new Tuple <double,double>(6.0, 0.04629),
                                    new Tuple <double,double>(7.0, 0.05555),
                                    new Tuple <double,double>(8.0, 0.06489),
                                };

        public static Dictionary<int, double> FlowDiam = new Dictionary<int, double>() {
                                                            {15,0.0157},
                                                            {20,0.0213},
                                                            {25,0.0273},
                                                            {32,0.0354},
                                                            };

        public static Dictionary<int, (double, double)> DiamFlowRange = new Dictionary<int, (double, double)>{
                                                            {15,(0,0.8)},
                                                            {20,(0,0.8)},
                                                            {25,(0,1)},
                                                            {32,(0,1)},
                                                            {40,(0,1)},
                                                            {50,(1,1.2)},
                                                            {65,(1,1.2)},
                                                            {80,(1.2,1.5)},
                                                            {100,(1.2,1.5)},
                                                            {125,(1.2,1.5)},
                                                            {150,(1.2,1.5)},
                                                            {200,(1.2,1.5)}
                                                        };

        public static double Th = 24.0; //用水时数（h） 等于24


    }
}
