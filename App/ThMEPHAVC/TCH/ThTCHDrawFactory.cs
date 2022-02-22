using System;
using System.IO;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Hvac;

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
        // Reducing 表
        public ulong ID;
        public ulong endFaceID;
        public ulong startFaceID;
        public ulong subSystemID;
        public ulong materialID;
    }
    public struct TCHElbowParam
    {
        // Elbow 表
        public ulong ID;
        public ulong endFaceID;
        public ulong startFaceID;
        public ulong subSystemID;
        public ulong materialID;
        public Int32 type;
        public Int32 segments;
        public double radRatio;
    }
    public struct TCHTeeParam
    {
        // Elbow 表
        public ulong ID;
        public ulong mainFaceID;
        public ulong branchFaceID1;
        public ulong branchFaceID2;
        public ulong subSystemID;
        public ulong materialID;
        public Int32 type;
        public double radRatio;
    }
    public struct TCHCrossParam
    {
        // Elbow 表
        public ulong ID;
        public ulong mainFaceID;
        public ulong branchFaceID1;
        public ulong branchFaceID2;
        public ulong branchFaceID3;
        public ulong subSystemID;
        public ulong materialID;
        public Int32 type;
        public double radRatio;
    }
    public struct TCHFlangesParam
    {
        // Flanges 表
        public ulong ID;
        public ulong endFaceID;
        public ulong startFaceID;
        public ulong subSystemID;
        public ulong materialID;
        public Int32 type;
        public double thickness;
        public double skirtSize;
    }
    public struct TCHMaterialsParam
    {
        // Materials 表
        public ulong ID;
        public string Name;
        public Int32 type;
    }
    public struct TCHSubSystemParam
    {
        // SubSystem 表
        public ulong ID;
        public string Name;
        public string remark;
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
        public ThDrawTCHTee teeService;
        public ThDrawTCHDuct ductService;
        public ThDrawTCHElbow elbowService;
        public ThDrawTCHCross crossService;
        public ThDrawTCHReducing reducingService;
        public ThDrawTCHMaterials materialsService;
        public ThDrawTCHSubSystemTypes subSystemService;
        private ThSQLiteHelper sqliteHelper;

        public ThTCHDrawFactory(string databasePath)
        {
            sqliteHelper = new ThSQLiteHelper(databasePath);
            sqliteHelper.Conn();
            ductService = new ThDrawTCHDuct(sqliteHelper);
            elbowService = new ThDrawTCHElbow(sqliteHelper);
            teeService = new ThDrawTCHTee(sqliteHelper);
            crossService = new ThDrawTCHCross(sqliteHelper);
            reducingService = new ThDrawTCHReducing(sqliteHelper);
            materialsService = new ThDrawTCHMaterials(sqliteHelper);
            subSystemService = new ThDrawTCHSubSystemTypes(sqliteHelper);
        }
        
        public void DrawSpecialShape(List<EntityModifyParam> connectors, Matrix3d mat, ref ulong gId)
        {
            foreach (var info in connectors)
            {
                switch (info.portWidths.Count)
                {
                    case 2: elbowService.Draw(info, mat, ref gId); break;
                    case 3: teeService.Draw(info, mat, ref gId); break;
                    case 4: crossService.Draw(info, mat, ref gId); break;
                    default: throw new NotImplementedException("[checkerror]: No such connector!");
                }
            }
        }
    }
}
