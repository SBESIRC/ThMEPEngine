using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.ElectricalLoadCalculation
{
    public class ElectricalLoadCalculationConfig
    {
        public static List<DynamicLoadCalculationModelData> ModelDataList { get; set; } = new List<DynamicLoadCalculationModelData>();
        public static string RoomFunctionName { get; set; }
        public static string Room_Layer_Name = "AI-房间框线";
        public static string RoomFunctionLayer = "AI-暖通-房间功能";
        public static string RoomFunctionBlockName = "AI-房间功能";

        public static string LoadCalculationTableName = "天华负荷计算表";
        public static string LoadCalculationTableLayer = "E-POWR-ANNO";

        public static string DefaultRoomNumber = "N-1F-01";
        public static string DefaultTableTextStyle = "TH-AI-STYLE3";
        public static bool chk_Area { get; set; } = true;//UI-CheckBox-面积
        public static bool chk_ElectricalIndicators { get; set; } = true; //UI-CheckBox-用电指标
        public static bool chk_ElectricalLoad { get; set; } = true; //UI-CheckBox-用电量
    }
}
