using System.Data;
using System.Collections.Generic;

namespace ThMEPHVAC.LoadCalculation.Model
{
    public class LoadCalculationParameterFromConfig
    {
        public static string Room_Layer_Name = "AI-房间框线";
        public static string RoomFunctionLayer = "AI-暖通-房间功能";
        public static string RoomFunctionLayer_New = "AI-房间功能";
        public static string RoomFunctionBlockName = "AI-暖通-房间功能";
        public static string RoomFunctionBlockName_New = "AI-房间功能";

        public static string LoadCalculationTableName = "天华负荷计算表";
        public static string LoadCalculationTableLayer = "AI-负荷通风标注";

        public static string DefaultRoomNumber = "N-1F-01";
        public static string DefaultTableTextStyle = "TH-AI-STYLE3";
        public static DataTable RoomFunctionConfigTable { get; set; }
        public static Dictionary<string,string> RoomFunctionConfigDic { get; set; }

        public static List<Dictionary<double, int>> WPipeDiameterConfig = new List<Dictionary<double, int>>()
        {
            new Dictionary<double, int>()
            {
                {0.22,15 },
                {0.5,20 },
                {1.1,25 },
                {2.1,32 },
                {3.0,40 },
                {6.0,50 },
                {11.5,70 },
                {18,80 },
                {38,100 },
                {66,125 },
                {105,150 },
                {250,200 },
                {400,250 },
                {650,300 },
                {1000,350 },
                {1400,400 },
                {1800,450 },
                {2250,500 },
                {3250,600 },
                {4250,700 },
                {5600,800 },
                {7000,900 },
                {8800,1000 },
            },
            new Dictionary<double, int>()
            {
                {0.06,15 },
                {0.12,20 },
                {0.85,25 },
                {1.8,32 },
                {2.5,40 },
                {5.1,50 },
                {9.8,70 },
                {15.5,80 },
                {33,100 },
                {57,125 },
                {90,150 },
                {215,200 },
                {340,250 },
                {550,300 },
                {880,350 },
                {1200,400 },
                {1600,450 },
                {2100,500 },
                {3250,600 },
                {4250,700 },
                {5600,800 },
                {7000,900 },
            }
        };
    }
}
