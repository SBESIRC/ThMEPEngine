using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.PressureDrainageSystem.Model;
using static DotNetARX.Preferences;
using static NPOI.HSSF.Util.HSSFColor;

namespace ThMEPWSS.PumpSectionalView.Utils
{
    
    public class ThLifePumpCommon
    {
        public static string Button_Name = "生成剖面图";//被统计的按钮名字
        //以下为输入
        public static double Input_Length = 5;
        public static double Input_Width = 2;
        public static double Input_Height = 2;
        public static double Input_Volume = 1;
        public static double Input_BasicHeight = 1;
        public static string Input_No = "生活泵组";//编号
        public static int Input_Num = 1;//用户输入的水箱数量
        public static string Input_Note = "";//备注


        //public static string BlkName = "生活泵房剖面";
        public static List<string> BlkName = new List<string> { "生活泵房剖面", "生活泵房表头","生活泵房材料"};

        //public static string Layer = "0";
        public static List<string> Layer = new List<string> { "0" };

        //先给出三组 1 2 3
        public static List<Pump_Arr> Input_PumpList = new List<Pump_Arr>() { new Pump_Arr { No="生活水泵加压1区",Flow_Info = 1, Head = 1, Power = 1, Num = 1,Note="三用一备" }, new Pump_Arr { No = "生活水泵加压2区", Flow_Info = 2, Head = 2, Power = 2, Num = 2 , Note = "三用一备" }, new Pump_Arr { No = "生活水泵加压3区",Flow_Info = 3, Head = 3, Power = 3, Num = 3  }, };


    


        //计算得到的基础数值
        public static string MagneticLevel = "磁耦合液位计";
        public static string MinimumAlarmWaterLevel = "最低报警水位";
        public static string MaximumEffectiveWaterLevel = "最高有效水位";
        public static string MaximumAlarmWaterLevel = "最高报警水位";
        public static string OverflowWaterLevel = "溢流水位";
        public static string TankTopHeight = "水箱顶高度";
        public static string TankBottomHeight = "水箱底高度";
        public static string EffectiveWaterDepth = "有效水深";
        public static string PumpBaseHeight = "泵基础高度";
        public static string BuildingFinishElevation = "建筑完成面高度";

        //二次计算
        public static string SuctionTotalPipeDiameter = "吸水总管管径";
        public static string PumpSuctionPipeDiameter = "水泵吸水管管径";
        public static string PumpOutletHorizontalPipeDiameter = "水泵出水横管管径";
        public static string PumpOutletPipeDiameter = "水泵出水立管管径";
        public static string WaterTankInletPipeDiameter = "水箱进水管管径";
        public static string OverflowPipeDiameter = "溢流管管径";
        public static string DrainPipeDiameter = "泄水管管径";

        //组数选择
        public static int PumpSuctionPipeDiameterChoice = 1;//水泵吸水管管径
        public static int PumpOutletHorizontalPipeDiameterChoice = 1;//水泵出水横管管径
        public static int PumpOutletPipeDiameterChoice = 1;//水泵出水立管管径

    }
}
