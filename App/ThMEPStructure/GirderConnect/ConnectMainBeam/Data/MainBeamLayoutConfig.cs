using System;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Data
{
    public class MainBeamLayoutConfig
    {
        public static double SplitArea = 52000000;
        public static double OverLength = 9000;

        public static bool SplitSelection = false;
        public static bool OverLengthSelection = true;
        public static bool RegionSelection = true;

        public static double SimilarAngle = Math.PI / 8;
        public static double SimilarPointsDis = 500;
        public static double SamePointsDis = 1;
        public static double MaxBeamLength = 13000;
    }

    public class MainBeamConfigFromFile
    {
        public static bool SplitSelection = false;
        public static double SplitArea = 52;
        public static bool OverLengthSelection = true;
        public static double OverLength = 9;
        public static bool RegionSelection = true;
    }
}
