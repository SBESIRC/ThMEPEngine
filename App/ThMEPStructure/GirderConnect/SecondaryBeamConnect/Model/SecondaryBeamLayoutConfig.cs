using Autodesk.AutoCAD.Geometry;

namespace ThMEPStructure.GirderConnect.SecondaryBeamConnect.Model
{
    public class SecondaryBeamLayoutConfig
    {
        public static double Da = 5500;//mm
        public static double Db = 6300;//mm
        public static double Dc = 3300;//mm
        public static double Er = 10.0;

        public static double AngleTolerance = 60; //容差：30°
        public static int FloorSelection = 1;
        public static int RegionSelection = 1;
        public static int DirectionSelection = 1;
        public static Vector3d MainDir = Vector3d.ZAxis;
    }

    public class SecondaryBeamConfigFromFile
    {
        public static int FloorSelection = 1;
        public static double Da = 5.5;//m
        public static double Db = 6.3;//m
        public static double Dc = 3.3;//m
        public static int RegionSelection = 1;
        public static int DirectionSelection = 1;
    }
}
