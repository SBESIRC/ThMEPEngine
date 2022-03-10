using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model.Hvac;

namespace ThMEPHVAC.TCH
{
    public struct TCHDuctDimContentsParam
    {
        // DuctDimContents 表
        public ulong ID;
        public Int32 section;
        public Int32 haveElevation;
        public Int32 haveAirVolume;
        public Int32 haveVelocity;
        public Int32 wayResis;
    }
    public struct TCHDuctDimensionsParam
    {
        // DuctDimensions 表
        public ulong ID;
        public ulong ductID;
        public ulong dimContentID;
        public ulong subSystemID;
        public string text;
        public Point3d basePoint;
        public Point3d leadPoint;
        public Int32 type;
        public Int32 eleType;
        public double textAngle;
        public Int32 sysKey;
    }
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
        public ThSQLiteHelper sqliteHelper;
        public ThTCHDrawFactory(string databasePath)
        {
            sqliteHelper = new ThSQLiteHelper(databasePath);
            sqliteHelper.Conn();
            subSystemService = new ThDrawTCHSubSystemTypes(sqliteHelper);
            materialsService = new ThDrawTCHMaterials(sqliteHelper);
        }
        public ThTCHDrawFactory(string databasePath, string scenario)
        {
            var subSysId = GetSubSystemId(scenario);
            sqliteHelper = new ThSQLiteHelper(databasePath);
            sqliteHelper.Conn();
            ductService = new ThDrawTCHDuct(sqliteHelper, subSysId);
            elbowService = new ThDrawTCHElbow(sqliteHelper, subSysId);
            teeService = new ThDrawTCHTee(sqliteHelper, subSysId);
            crossService = new ThDrawTCHCross(sqliteHelper, subSysId);
            reducingService = new ThDrawTCHReducing(sqliteHelper, subSysId);
        }

        private ulong GetSubSystemId(string scenario)
        {
            string dbScenario;
            if (scenario == "消防加压送风")
                dbScenario = "消防加压";
            else if(scenario == "消防排烟")
                dbScenario = "消防排烟";
            else if (scenario == "消防补风")
                dbScenario = "消防补风";
            else if (scenario == "消防排烟兼平时排风")
                dbScenario = "排风兼排烟";
            else if (scenario == "消防补风兼平时送风")
                dbScenario = "送风兼补风";
            else if (scenario == "平时排风" || scenario == "平时排风兼事故送风")
                dbScenario = "排风";
            else if (scenario == "平时送风" || scenario == "平时送风兼事故补风")
                dbScenario = "送风";
            else if (scenario == "空调送风")
                dbScenario = "空调送风";
            else if (scenario == "空调回风")
                dbScenario = "空调回风";
            else if (scenario == "空调新风")
                dbScenario = "空调新风";
            else if (scenario == "厨房排油烟")
                dbScenario = "排油烟";
            else if (scenario == "厨房排油烟补风")
                dbScenario = "厨房补风";
            else if (scenario == "事故排风" || scenario == "事故补风")
                dbScenario = "事故排风";
            else
                throw new NotImplementedException("请检查输入场景！！！");
            ulong idx = 1;
            foreach (var subSys in ThTCHCommonTables.subSystems)
            {
                if (subSys == dbScenario)
                    return idx;
                else
                    idx++;
            }
            throw new NotImplementedException("请检查输入场景！！！");
        }

        public void DrawSpecialShape(List<EntityModifyParam> connectors, Matrix3d mat, double mainHeight, double elevation, ref ulong gId)
        {
            foreach (var info in connectors)
            {
                switch (info.portWidths.Count)
                {
                    case 2: elbowService.Draw(info, mat, mainHeight, elevation, ref gId); break;
                    case 3: teeService.Draw(info, mat, mainHeight, elevation, ref gId); break;
                    case 4: crossService.Draw(info, mat, mainHeight, elevation, ref gId); break;
                    default: throw new NotImplementedException("[checkerror]: No such connector!");
                }
            }
        }
    }
}
