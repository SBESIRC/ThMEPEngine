using Org.BouncyCastle.Asn1.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.PumpSectionalView.Utils
{
    /// <summary>
    /// 消防泵房
    /// </summary>
    public class ThFirePumpCommon
    {
        public static string Button_Name="生成剖面图";//被统计的按钮名字
        //以下为输入
        public static double Input_BuildingFinishHeight = 1;//建筑完成面高度H1
        public static double Input_RoofHeight = 1;//顶板高度H2
        public static double Input_PoolArea = 1;//消防水池面积S
        public static double Input_Volume = 1;//有效容积V
        public static double Input_EffectiveDepth = 1;//有效水深H
        public static double Input_BasicHeight = 1;//水泵基础高度h0
        public static double Input_FirePressure = 1;//消火栓泵起泵压力
        public static double Input_WaterPressure = 1;//喷淋泵起泵压力


        public static string Input_Type = "立式泵";
 
        public static List<string> BlkName=new List<string>() { "消防泵房剖面1" , "消防泵房剖面2","消防泵房表头","消防泵房材料" };
        //public static string BlkName_FirePump_1 = "消防泵房剖面1";
        //public static string BlkName_FirePump_2 = "消防泵房剖面2";

        public static string Layer = "0";


        //自定义版面
        public static string BlkSettingAttrName = "可见性1";
        public static Dictionary<string, string> TypeToAttr = new Dictionary<string, string>() {
                                            {"立式泵",BlkSettingAttrName },{"卧式泵",BlkSettingAttrName },
                                                                };

        //泵组信息
        public static List<Pump_Arr> Input_PumpList = new List<Pump_Arr>() { new Pump_Arr { No = "泵组1", Flow_Info = 1, Head = 1, Num = 1, Note = "" }, new Pump_Arr { No = "泵组2", Flow_Info = 2, Head = 2, Num = 2, Note = "" }, new Pump_Arr { No = "泵组3", Flow_Info = 3, Head = 3, Num = 3 }, };

        //计算得到的属性版面

        //计算得到的基础数值
        public static string BuildingFinish = "建筑完成面";
        public static string PoolTopHeight = "水池顶高度";
        public static string HighPumpFoundation = "水泵基础高";
        public static string AirVentHeight = "放气孔高度";
        public static string MinimumAlarmWaterLevel = "最低报警水位";
        public static string MaximumEffectiveWaterLevel = "最高有效水位";
        public static string MaximumAlarmWaterLevel = "最高报警水位";
        public static string OverflowWaterLevel = "溢流水位";
        public static string CrossTubeHeight = "横管高度";
        public static string InletPipeHeight = "进水管高度";
        public static string SnorkelHeight = "通气管高度";
        

        //二次计算
        public static string WaterSuctionPipeDiameter = "吸水母管管径";
        public static string PumpSuctionPipeDiameter = "吸水管管径";
        public static string PumpOutletPipeDiameter = "水泵出水立管管径";
        public static string PumpOutletHorizontalPipeDiameter = "水泵出水横管管径";//水泵出水横管管径

        //组数选择 -- 三组相同
        public static int choice=1;
        //public static int PumpSuctionPipeDiameterChoice = 1;//水泵吸水管管径
        //public static int PumpOutletHorizontalPipeDiameterChoice = 1;//水泵出水横管管径
        //public static int PumpOutletPipeDiameterChoice = 1;//水泵出水立管管径
    }

    
}
