using System;
using SQLite;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.Model;
using System.IO;

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
        private ThDrawTCHDuct ductService;
        private ThDrawTCHTee teeService;
        private ThDrawTCHElbow elbowService;
        private ThDrawTCHCross crossService;
        private ThDrawTCHReducing reducingService;
        private SQLiteConnection connection;

        public ThTCHDrawFactory(string databasePath)
        {
            InitialiseConnection(databasePath);
            ulong incerse = 0;
            ductService = new ThDrawTCHDuct(incerse);
            CloseConnection();
        }

        public void DrawDuct(SegInfo segInfo)
        {
            ductService.GetInsertStatement(segInfo);
            InsertRecord<TCHDuctParam>(ductService.recordDuct);
            InsertRecord<TCHInterfaceParam>(ductService.recordDuctSrtInfo);
            InsertRecord<TCHInterfaceParam>(ductService.recordDuctEndInfo);
        }

        private void InitialiseConnection(string databasePath)
        {
            var options = new SQLiteConnectionString(databasePath, false);
            connection = new SQLiteConnection(databasePath);
        }
        
        private void InsertRecord<T>(string statement) where T : new()
        {
            connection.Query<T>(statement);
        }

        private void CloseConnection()
        {
            connection.Close();
        }
    }
}
