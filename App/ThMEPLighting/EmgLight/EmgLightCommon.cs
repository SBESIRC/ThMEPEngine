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
        public static string LayerNotIntersectStruct = "l6NotIntersectStruct";
        public static string LayerOverlap = "l5overlap";
        public static string LayerSeparate = "l4Separate";
        public static string LayerStruct = "l7Struct";
        public static string LayerStructLayout = "l8StructLayout";

        public static string Layer = string.Format ("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}", 
                                                        LayerExtendPoly, LayerFrame, LayerSeparatePoly,
                                                        LayerLaneHead, LayerLane, LayerGetStruct,
                                                        LayerStructSeg, LayerParallelStruct, LayerNotIntersectStruct,
                                                        LayerOverlap, LayerSeparate, LayerStruct, LayerStructLayout
                                                   );


        public enum ThStructType : int
        {
            column = 0,
            wall = 1
        }
    }
}
