using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.EmgLightConnect
{
    public class EmgConnectCommon
    {

        public static int TolGroupBlkLane = 6000;
        public static int TolGroupBlkLaneHead = 400;
        public static int TolGroupEmgLightEvac = 400;
        public static int TolOrderSideLanePtOnFrame = 200;
        public static int TolSaperateGroupMaxDistance = 20000;
        public static int TolReturnValueDistCheck = 5000;
        public static int TolReturnValueMinRange = 500;
        public static int TolTooClosePt = 800;
        public static double TolRegroupMainYRange = 1000;
        public static int TolReturnRangeInGroup = 4500;
        public static int TolReturnRangeInSide = 1000;
        public static int TolBlkMaxConnect = 4;
        public static int TolMaxLigthNo = 25;
        public static int TolMaxReturnValue = 10000;
        public static int TolConnectSecPtRange = 12000;
        public static int TolConnectSecPrimAddValue = 10000;



        public static string LayerMovedLane = "l0movedLane";
        public static string LayerLaneSape = "l0LaneSape";
        public static string LayerBlockCenter = "l0BlockCenter";
        public static string LayerSingleSideGroup = "l0SingleSideGroup";
        public static string LayerGroupConnectLine = "l0GroupConnectLine";

        public enum BlockType 
        {
            emgLight = 0 ,
            evac = 1,
            exit = 2,
            evacCeiling = 3,
            enter = 4,
            ale = 5,
        }

        public enum BlockGroupType
        {
            mainBlock = 0,
            secBlock =1,
           
        }

    }
}
