using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.LoadCalculation.Model
{
    public class LoadCalculationParameterFromConfig
    {
        public static string Room_Layer_Name = "AI-房间框线";
        public static string RoomFunctionLayer = "AI-暖通-房间功能";
        public static string RoomFunctionBlockName = "AI-暖通-房间功能";

        public static string LoadCalculationTableName = "天华负荷计算表";
        public static string LoadCalculationTableLayer = "AI-负荷通风标注";
        public static DataTable RoomFunctionConfigTable { get; set; }
        public static Dictionary<string,string> RoomFunctionConfigDic { get; set; }

        public static List<Dictionary<double, int>> WPipeDiameterConfig = new List<Dictionary<double, int>>()
        {
            new Dictionary<double, int>()
            {
                {8800,1000 },
                {7000,900 },
                {5600,800 },
                {4250,700 },
                {3250,600 },
                {2250,500 },
                {1800,450 },
                {1400,400 },
                {1000,350 },
                {650,300 },
                {400,250 },
                {250,200 },
                {105,150 },
                {66,125 },
                {38,100 },
                {18,80 },
                {11.5,70 },
                {6.0,50 },
                {3.0,40 },
                {2.1,32 },
                {1.1,25 },
                {0.5,20 },
                {0.22,15 },
            },
            new Dictionary<double, int>()
            {
                {7000,900 },
                {5600,800 },
                {4250,700 },
                {3250,600 },
                {2100,500 },
                {1600,450 },
                {1200,400 },
                {880,350 },
                {550,300 },
                {340,250 },
                {215,200 },
                {90,150 },
                {57,125 },
                {33,100 },
                {15.5,80 },
                {9.8,70 },
                {5.1,50 },
                {2.5,40 },
                {1.8,32 },
                {0.85,25 },
                {0.12,20 },
                {0.06,15 },
            }
        };
    }
}
