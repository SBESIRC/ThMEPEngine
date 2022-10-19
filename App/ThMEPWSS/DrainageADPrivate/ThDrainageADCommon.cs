using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.DrainageADPrivate
{
    internal class ThDrainageADCommon
    {
        public static string Layer_EQPM_D = "W-DRAI-EQPM";
        public static string Layer_EQPM = "W-WSUP-EQPM";
        public static string Layer_NOTE = "W-WSUP-NOTE";
        public static string Layer_DIMS_D = "W-DRAI-DIMS";
        public static string Layer_DIMS = "W-WSUP-DIMS";
        public static string Layer_Bush = "W-BUSH";
        public static string Layer_HotPipe = "W-WSUP-HOT-PIPE";
        public static string Layer_CoolPipe = "W-WSUP-COOL-PIPE";
        public static string Layer_CoolPipe_v = "W-WSUP";
        public static string Layer_HotIPipe = "W-WSUP-HOTI-PIPE";
        public static string Layer_HotRPipe = "W-WSUP-HOTR-PIPE";

        public static string BlkName_WaterHeater = "热水器标注";
        public static string BlkName_WaterHeater_AD = "燃气热水器";
        public static string BlkName_AngleValve = "给水角阀平面";
        public static string BlkName_AngleValve_AD = "给水角阀";

        public static string BlkName_ShutoffValve = "截止阀";
        public static string BlkName_ShutoffValve_TchTag = "$VALVE$00000295";
        public static string BlkName_GateValve = "闸阀";
        public static string BlkName_GateValve_TchTag = "$VALVE$00000296";
        public static string BlkName_CheckValve = "止回阀";
        public static string BlkName_CheckValve_TchTag = "$VALVE$00000315";
        public static string BlkName_AntifoulingCutoffValve = "防污隔断阀";
        public static string BlkName_AntifoulingCutoffValve_TchTag = "$VALVE$00000656";
        public static string BlkName_WaterMeteValve_TchTag = "$VALVE$00000742";
        public static string BlkName_OpeningSign = "断线";
        public static string BlkName_OpeningSign_TchTag = "$TwtSys$00000132";
        public static string BlkName_Casing = "套管";
        public static string BlkName_Casing_AD = "套管系统";

        public static string BlkName_Dim = "给水管径50";
        public static List<double> Radius_Vertical = new List<double> { 25, 50 };

        public static string DiameterDN_visi_pre = "DN";
        public static int DiameterDim_move_x = 25;
        public static int DiameterDim_move_y = 30;
        public static int DiameterDim_blk_x = 440;
        public static int DiameterDim_blk_y = 180;

        public static double Blk_scale_end = 1.0;

        public static string VisiName_valve = "可见性";
        public static string VisiName1_valve = "可见性1";

        public static double Th = 24.0; //用水时数（h） 等于24
        public static double TransEnlargeScale = 1.5;
        public static double BreakLineLength = 75.0;

        public static double Tol_TerminalArea = 3000 * 3000; //洁具面积>3*3米过滤
        public static double Tol_PipeToVerticalPipeCenter = 100;
        public static int Tol_SamePoint = 10;
        public static int Tol_AngleValveToPipe = 30;
        public static double Tol_PipeEndPair = 250;
        public static double Tol_PipeEndToTerminal = 500;

        public enum TerminalType
        {
            Toilet,  //坐便器
            Washbasin,//洗手台
            Sink,   //洗涤盆
            Shower, //淋浴器
            WashingMachine,//洗衣机
            MopSink, //拖把池
            Bathtub,//浴缸
            WaterHeater,//热水器
            Unknow,
        }

        public static Dictionary<int, string> TerminalChineseName = new Dictionary<int, string>(){
                                                            {(int) TerminalType.Toilet , "坐便器" },
                                                            {(int) TerminalType.Washbasin , "单盆洗手台" },
                                                            {(int) TerminalType.Sink , "厨房洗涤盆" },
                                                            {(int) TerminalType.Shower , "淋浴器" },
                                                            {(int) TerminalType.WashingMachine , "洗衣机" },
                                                            {(int) TerminalType.MopSink , "拖把池" },
                                                            {(int) TerminalType.Bathtub , "浴缸" },
                                                        };

        public static Dictionary<int, double> TerminalFixtureUnitCoolHot = new Dictionary<int, double>()
                                                        {
                                                            {(int) TerminalType.Toilet ,  0.5},
                                                            {(int) TerminalType.Washbasin , 0.75 },
                                                            {(int) TerminalType.Sink , 1},
                                                            {(int) TerminalType.Shower , 0.75 },
                                                            {(int) TerminalType.WashingMachine , 1 },
                                                            {(int) TerminalType.MopSink , 1 },
                                                            {(int) TerminalType.Bathtub ,1.2 },
                                                            {(int) TerminalType.WaterHeater , 0},
                                                            {(int) TerminalType.Unknow  , 0.75 },
                                                        };

        public static Dictionary<int, double> TerminalFixtureUnitCool = new Dictionary<int, double>()
                                                            {
                                                                 {(int) TerminalType.Toilet ,  0.5},
                                                                {(int) TerminalType.Washbasin , 0.5 },
                                                                {(int) TerminalType.Sink , 0.7},
                                                                {(int) TerminalType.Shower , 0.5 },
                                                                {(int) TerminalType.WashingMachine , 1 },
                                                                {(int) TerminalType.MopSink , 1 },
                                                                {(int) TerminalType.Bathtub ,1},
                                                                {(int) TerminalType.WaterHeater , 0},
                                                                {(int) TerminalType.Unknow  , 0.5 },
                                                            };

        public static Dictionary<int, double> TerminalFixtureUnitHot = new Dictionary<int, double>()
                                                            {
                                                                {(int) TerminalType.Toilet ,  0},
                                                                {(int) TerminalType.Washbasin , 0.5 },
                                                                {(int) TerminalType.Sink , 0.7},
                                                                {(int) TerminalType.Shower , 0.5 },
                                                                {(int) TerminalType.WashingMachine , 0 },
                                                                {(int) TerminalType.MopSink , 0 },
                                                                {(int) TerminalType.Bathtub ,1},
                                                                {(int) TerminalType.WaterHeater , 0},
                                                                {(int) TerminalType.Unknow  , 0.5 },
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

        public static Dictionary<int, string> Terminal_end_name = new Dictionary<int, string>() {
                                                            {(int) TerminalType.Toilet ,  "给水角阀"},
                                                            {(int) TerminalType.Washbasin , "给水角阀" },
                                                            {(int) TerminalType.Sink , "给水角阀"},
                                                            {(int) TerminalType.Shower , "淋浴器系统" },
                                                            {(int) TerminalType.WashingMachine , "给水角阀" },
                                                            {(int) TerminalType.MopSink , "给水角阀" },
                                                            {(int) TerminalType.Bathtub ,"给水角阀" },
                                                            {(int) TerminalType.WaterHeater , "燃气热水器"},
                                                            {(int)TerminalType .Unknow ,"给水角阀" },
                                                        };
        public static Dictionary<string, List<string>> EndValve_dir_name = new Dictionary<string, List<string>>  {
                                                            {"给水角阀",new List<string>() {"向右","向前","向左","向后"}},
                                                            {"感应式冲洗阀",new List<string>() {"向右水平", "向后水平", "向左水平", "向前水平" } }, //前后和其他反着的
                                                            {"延时自闭阀" ,new List<string>() {"向右水平", "向前水平", "向左水平", "向后水平"} },
                                                            {"水龙头1", new List<string>() { "向右","向前","向左","向后" } },
                                                            {"淋浴器系统", new List<string>() {"向右","向前","向左","向后" } },
                                                            {"浴缸系统", new List<string>() {"向右","向前","向左","向后" } },
                                                            {"燃气热水器",new List<string>() {"向右", "向后", "向左","向前" }  },
                                                             {"套管系统",new List<string>() {"普通套管水平", "普通套管垂直", "普通套管水平", "普通套管垂直" } },
                                                        };
    }
}
