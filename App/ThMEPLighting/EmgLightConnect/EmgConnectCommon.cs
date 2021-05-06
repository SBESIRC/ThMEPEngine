﻿using System;
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
        public static int TolGroupEmgLightEvac = 600;
        public static int TolOrderSideLanePtOnFrame = 200;
        public static int TolSaperateGroupMaxDistance = 15000;
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
        public static int TolLinkOffsetWithFrame = 50;
        //public static int TolLinkTooShortLine = 500;


        public static string LayerBlockCenter = "l0BlockCenter";
        public static string LayerMovedLane = "l1MovedLane";
        public static string LayerBlkOutline = "l1BlkOutline";
        public static string LayerConnectLine = "l4ConnectLine";
        public static string LayerConnectLineFinal = "l4ConnectLineFinal";
        public static string LayerFinalFinal = "l4FinalFinal";
        public static string LayerOptimalSingleSideGroup = "l3SingleSideGroup";


        

        public enum BlockGroupType
        {
            mainBlock = 0,
            secBlock = 1,

        }

    }
}
