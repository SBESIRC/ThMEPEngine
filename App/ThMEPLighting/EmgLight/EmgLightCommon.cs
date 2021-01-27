using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.EmgLight
{
    public static class EmgLightCommon
    {
        public static int TolLane = 6000;
        public static double TolUniformSideLenth = 0.6;
        public static int TolAvgColumnDist = 7900;
        public static int TolLightRangeMin = 4000;
        public static int TolLightRangeMax = 8900;
        public static int TolLaneProtect = 1000;
        public static int TolLight = 400;
        public static int BufferFrame = 100;

        public static string LayerStruct = "lStruct";
        public static string LayerStructLayout = "lStructLayout";
        public static string LayerExtendPoly = "lExtendPoly";
        public static string LayerFrame = "lFrame";
        public static string LayerGetStruct = "lGetStruct";
        public static string LayerParallelStruct = "lParallelStruct";
        public static string LayerSeparate = "lSeparate";
        public static string LayerSeparatePoly = "lSeparatePoly";
        public static string LayerLaneHead = "lLaneHead";
        public static string LayerLane = "lLane";
    }
}
