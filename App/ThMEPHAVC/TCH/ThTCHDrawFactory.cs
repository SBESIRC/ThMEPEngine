using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPHVAC.TCH
{
    public struct TCHDuctParam
    {
        // Ducts 表
        public ulong ID;
        public ulong endFaceID;
        public ulong startFaceID;
        public ulong subSystemID;
        public ulong materialID;
        public Int32 sectionType;
        public Int32 ductType;
        public Int32 Soft;
        public double Bulge;
        public double AirLoad;
    }
    public struct TCHReducingParam
    {
        // Ducts 表
        public ulong ID;
        public ulong endFaceID;
        public ulong startFaceID;
        public ulong subSystemID;
        public ulong materialID;
    }
    public struct TCHInterfaceParam
    {
        // MepInterfaces
        public ulong ID;
        public Int32 sectionType;
        public double height;
        public double width;
        public Vector3d normalVector;
        public Vector3d heighVector;
        public Point3d centerPoint;
    }
    public class ThTCHDrawFactory
    {
        public ThDrawTCHDuct ductService;
        public ThDrawTCHTee teeService;
        public ThDrawTCHElbow elbowService;
        public ThDrawTCHCross crossService;
        public ThDrawTCHReducing reducingService;
        private ThSQLiteHelper sqliteHelper;

        public ThTCHDrawFactory(string databasePath)
        {
            sqliteHelper = new ThSQLiteHelper(databasePath);
            ductService = new ThDrawTCHDuct(sqliteHelper);
            reducingService = new ThDrawTCHReducing(sqliteHelper);
        }
    }
}
