using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.PumpSectionalView.Utils
{
    public class ThHighFireWaterTankCommon
    {
       

        //以下为输入
        public static double Input_Length = 1;
        public static double Input_Width = 1;
        public static double Input_Height = 1;
        public static double Input_Volume = 1;
        public static double Input_BasicHeight = 1;

        public static string Input_Type1 = "有顶有稳压泵";
        public static string Input_Type2 = "有顶";

        public static string Type_WithRoofWithPump = "有顶有稳压泵";
        public static string Type_WithRoofNoPump = "有顶无稳压泵";
        public static string Type_NoRoofWithPump = "无顶有稳压泵";
        public static string Type_NoRoofNoPump = "无顶无稳压泵";
        public static string Type_WithRoof = "有顶";
        public static string Type_NoRoof = "露天";

        public static string BlkName_HighFireWaterTank_1 = "消防水箱剖面1";
        public static string BlkName_HighFireWaterTank_2 = "消防水箱剖面2";

        public static string Layer = "0";
       


        public static Dictionary<string, string> TypeToBlk = new Dictionary<string, string>() {
                                            {Type_WithRoofWithPump,BlkName_HighFireWaterTank_1 },
                                            {Type_WithRoofNoPump,BlkName_HighFireWaterTank_1 },
                                            {Type_NoRoofWithPump,BlkName_HighFireWaterTank_1 },
                                            {Type_NoRoofNoPump,BlkName_HighFireWaterTank_1 },
                                            {Type_WithRoof,BlkName_HighFireWaterTank_2 },
                                            {Type_NoRoof,BlkName_HighFireWaterTank_2 },
                                                                };

        public static Dictionary<int, string> getType = new Dictionary<int, string>() {
                                            {1,Type_WithRoofWithPump },
                                            {2,Type_WithRoofNoPump },
                                            {3, Type_NoRoofWithPump },
                                            {4,Type_NoRoofNoPump },
                                            {5,Type_WithRoof },
                                            {6,Type_NoRoof },
                                                                };


        //自定义版面
        public static string BlkSettingAttrName = "可见性1";

        public static Dictionary<string, string> TypeToAttr = new Dictionary<string, string>() {
                                            {Type_WithRoofWithPump,BlkSettingAttrName },
                                            {Type_WithRoofNoPump,BlkSettingAttrName },
                                            {Type_NoRoofWithPump,BlkSettingAttrName },
                                            {Type_NoRoofNoPump,BlkSettingAttrName },
                                            {Type_WithRoof,BlkSettingAttrName },
                                            {Type_NoRoof,BlkSettingAttrName },
                                                                };

        //计算得到的属性版面
        public static string LevelGaugeHeight = "液位计高度";
        public static string MinimumAlarmWaterLevel = "最低报警水位";
        public static string MaximumWaterLevel = "最高水位";
        public static string ElectricValveClosingWaterLevel = "电动阀关闭水位";
        public static string OverflowWaterLevel = "溢流水位";
        public static string BottomOfWaterInletPipe = "进水管底";
        public static string TankTopHeight = "水箱顶";
        public static string WaterInletHorizontalPipe = "进水横管";
        public static string Snorkel_1 = "通气管1";
        public static string Snorkel_2 = "通气管2";
        public static string BaseHeight = "基础高度";
        public static string BottomHeight = "水箱底高";
        public static string EffectiveWaterDepth = "有效水深";
        public static string TankHeight = "水箱高度";
        public static string ClearHeight  = "水泵房净高";

        public static string EffectiveVolume = "有效容积";
    }

    
}
