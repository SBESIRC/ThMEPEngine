using System;
using System.Collections.Generic;

namespace ThMEPHVAC.Service
{
    public class ThMEPHAVCDataManager
    {
        public static string FGDXLayer = "H-DAPP-DAMP";
        public static string FGLYLayer = "AI-风管路由";
        public static string SGLYLayer = "AI-水管路由";
        public static string AirPortLayer = "H-DAPP-GRIL";
        public static string AirPortBlkName = "AI-风口";
        public static string FGDXBlkName = "AI-风管断线";        
        public static string FGLGBlkName = "AI-风管立管";
        public static string SGDXBlkName = "AI-水管断线";

        public static List<string> GetSystemTypes()
        {
            return new List<string> { "新风口", "排油烟风口", "排油烟补风口", "事故排风口", "平时排风口", "平时补风口" };
        }
        public static List<string> GetAirPortTypes()
        {
            return new List<string> { "下送风口", "下回风口", "方形散流器", "圆形风口", "侧送风口", "侧回风口" };
        }
        public static string GetAirVolumeQueryKeyword(string systemType)
        {
            // 请参照LogicService.CalculateData中创建表格使用的字符
            switch (systemType)
            {
                case "新风口":
                    return "新风量";
                case "排油烟风口":
                    return "排油烟";
                case "排油烟补风口":
                    return "油烟补风";
                case "事故排风口":
                    return "事故排风";
                case "平时排风口":
                    return "平时排风";
                case "平时补风口":
                    return "平时补风";
            }
            return "";
        }
        public static string GetInitAirportType(string systemType)
        {
            switch (systemType)
            {
                case "新风口":
                    return "方形散流器";
                case "排油烟风口":
                    return "下回风口";
                case "排油烟补风口":
                    return "下送风口";
                case "事故排风口":
                    return "下回风口";
                case "平时排风口":
                    return "下回风口";
                case "平时补风口":
                    return "下回风口";
            }
            return "";
        }
        public static Tuple<int, int> CalculateAirPortSize(double singleAirPortVolume, string airPortType)
        {
            switch (airPortType)
            {
                case "下送风口":
                case "下回风口":
                case "侧送风口":
                case "侧回风口":
                    var airSpeedUpper = GetAirSpeedUpperLimitedValue(airPortType);
                    var lwRatio = ThMEPHAVCDataManager.GetAirPortLengthWidthRatio(airPortType);
                    return ThRectangleAirPortSizeCalculator.CalculateAirPortSize(singleAirPortVolume, airSpeedUpper, lwRatio);
                case "方形散流器":
                    return ThSquareDiffuserAirPortSizeCalculator.CalculateAirPortSize(singleAirPortVolume);
                case "圆形风口":
                    return ThCircleAirPortSizeCalculator.CalculateAirPortSize(singleAirPortVolume);
            }
            return Tuple.Create(0, 0);
        }
        
        public static double GetAirPortLengthWidthRatio(string airPortType)
        {
            switch (airPortType)
            {
                case "下送风口":
                case "下回风口":
                case "侧送风口":
                case "侧回风口":
                    return 4.0;
                case "方形散流器":
                case "圆形风口":
                    return 1.0;
            }
            return 0.0;
        }

        public static List<int> GetRectangleSizes()
        {
            return new List<int>() {100,200,300,400,500,600,800,1000,1500,2000,2500,3000,3500,4000 };
        }
        public static List<int> GetSquareSizes()
        {
            return new List<int>() { 90, 120, 150, 180, 210, 240, 270, 300, 330, 360, 390, 420, 450, 480 };
        }
        public static List<int> GetCircleSizes()
        {
            return GetSquareSizes(); // 共享方形
        }
        private static double GetAirSpeedUpperLimitedValue(string airPortType)
        {
            switch (airPortType)
            {
                case "下送风口":
                case "下回风口":
                case "侧送风口":
                case "侧回风口":
                    return 2.2;
                case "方形散流器":
                case "圆形风口":
                    return 3.0;
            }
            return 0.0;
        }
        private static double GetAirSpeedDownLimitedValue(string airPortType)
        {
            switch (airPortType)
            {
                case "下送风口":
                case "下回风口":
                case "侧送风口":
                case "侧回风口":
                    return 0.0;
                case "方形散流器":
                case "圆形风口":
                    return 2.4;
            }
            return 0.0;
        }
    }
}
