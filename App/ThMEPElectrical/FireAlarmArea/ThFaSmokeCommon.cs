using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPElectrical.FireAlarmArea
{
    public class ThFaSmokeCommon
    {
        public static double[,] protectRadius = new double[,]{{6700,7200,8000},
                                                            {6700,8000,9900},
                                                            {5800,7200,9000},
                                                            {4400,4900,5500},
                                                            {3600,4900,6300}};


        public static string smokeTag = "感烟火灾探测器";
        public static string heatTag = "感温火灾探测器";
        public static string gasTag = "可燃气体探测器";
        public static string expPrfTag = "防爆";
        public static string nonLayoutTag = "非火灾探测区域";

        public static double AisleAreaThreshold = 0.75;

        public enum layoutType
        {
            stair = 0,
            smoke = 1,
            heat = 2,
            smokeHeat = 3,
            gas = 4,
            gasPrf = 5,
            nonLayout = 6,
            noName = 7,
            heatPrf=8,
            smokePrf=9,
            smokeHeatPrf = 10,
        }

        public static string Layer_Blind = "AI-烟温感盲区";
        public static string Layer_Gas_Blind = "AI-可燃气盲区";
        public static int Color_Blind = 1;
    }
}
