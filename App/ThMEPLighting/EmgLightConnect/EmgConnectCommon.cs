using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPLighting.EmgLightConnect
{
    public class EmgConnectCommon
    {

        public static int TolLane = 6000;
        public static int TolLaneHead = 400;
        public static int TolGroupEmgLight = 400;
        public static int TolLaneEndOnFrame = 200;
        public static int TolGroupDistance = 20000;
        public static int TolReturnValueAs0 = 6000;
        public static int TolMaxLigthNo = 25;
        public static int TolReturn = 5000;
        public static int TolMinReturnValueRange = 500;
        public static int TolTooClosePt = 800;
        public static double TolPtOnSameLineYRange = 1000;
        public static int TolReturnRange = 4500;
        public static int TolMaxConnect = 4;
        public static int TolMaxReturnValue = 10000;



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
