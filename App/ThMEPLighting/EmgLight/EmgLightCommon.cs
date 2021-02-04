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
        public static int TolLightRangeMax = 9000;
        public static int TolLaneProtect = 1000;
        public static int TolBrakeWall= 400;
        public static int TolInterFilter = 350;
        public static int BufferFrame = 100;
        public static double TolIntersect = 20;
        public static double BlockScaleNum = 100;

        public static string LayerExtendPoly = "l0ExtendPoly";
        public static string LayerFrame = "l0Frame";
        public static string LayerSeparatePoly = "l0SeparatePoly";
        public static string LayerLaneHead = "l0LaneHead";
        public static string LayerLane = "l0LaneLable";
        public static string LayerGetStruct = "l1GetStruct";
        public static string LayerStructSeg = "l2StructSeg";
        public static string LayerParallelStruct = "l3ParallelStruct";
        public static string LayerNotIntersectStruct = "l4NotIntersectStruct";
        public static string LayerSeparate = "l5Separate";
        public static string LayerStruct = "l6Struct";
        public static string LayerStructLayout = "l7StructLayout";

        public enum ThStructType : int
        {
            column = 0,
            wall = 1
        }
    }
}
