using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.EmgLightConnect
{
    public class EmgConnectCommon
    {
        public static int TolOrderSideLanePtOnFrame = 200;
        public static int TolSaperateGroupMaxDistance = 15000;
        public static int TolTooClosePt = 800;
        public static int TolBlkMaxConnect = 4;
        public static int TolMaxLigthNo = 25;
        public static int TolMinLigthNo = 5;

        public static int TolGroupBlkLane = 6000;
        public static int TolGroupBlkLaneHead = 400;
        public static int TolGroupEmgLightEvac = 100;//边框*scale相加以后在外扩【200】的值，不是两点200以内

        public static int TolReturnValueDistCheck = 2500;
        public static int TolReturnValueMinRange = 2500;
        public static int TolReturnValueMaxDistance = 20000;
        public static int TolReturnValue0Approx = 1000;
        public static int TolReturnValueMax = 10000;
        public static int TolReturnValueRange = 6000;
        public static int TolReturnValueRangeTo = 600;

        public static double TolRegroupMainYRange = 1000;

        public static int TolConnectSecPtRange = 12000;
        public static int TolConnectSecPrimAddValue = 10000;
  
         

        public static string LayerBlockCenter = "l0BlockCenter";
        public static string LayerBlkOutline = "l1BlkOutline";
        public static string LayerConnectLine = "l4ConnectLine";
        public static string LayerConnectLineFinal = "l4ConnectLineFinal";
        public static string LayerFinalFinal = "l4ConnectLineCorrectIntersectFinal";
        public static string LayerOptimalSingleSideGroup = "l3SingleSideGroup";
        public static string LayerBlkNo = "l5blkNo";


        public enum BlockGroupType
        {
            mainBlock = 0,
            secBlock = 1,

        }

    }
}
