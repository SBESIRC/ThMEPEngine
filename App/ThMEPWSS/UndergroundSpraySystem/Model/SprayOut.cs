using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Block;
using ThMEPWSS.UndergroundSpraySystem.General;
using ThMEPWSS.UndergroundSpraySystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.Model
{
    public class SprayOut
    {
        public Point3d InsertPoint { get; set; }
        public Point3d PipeInsertPoint { get; set; }
        public string CurrentFloor { get; set; }
        public List<Line> PipeLine { get; set; }
        public List<Line> NoteLine { get; set; }
        public List<Line> FloorLine { get; set; }
        public List<Text> Texts { get; set; }
        public List<SprayBlock> SprayBlocks { get; set; }

        public List<AlarmValveSys> AlarmValves { get; set; }
        public List<FireDistrictRight> FireDistricts { get; set; }
        public List<FireDistrictLeft> FireDistrictLefts { get; set; }
        public List<WaterPump>  WaterPumps{ get; set; }
        public SprayOut(Point3d insertPt)
        {
            InsertPoint = insertPt;
            PipeInsertPoint = InsertPoint.Cloned();
            CurrentFloor = "B1";
            PipeLine = new List<Line>();
            NoteLine = new List<Line>();
            FloorLine = new List<Line>();
            Texts = new List<Text>();
            SprayBlocks = new List<SprayBlock>();
            AlarmValves = new List<AlarmValveSys>();
            FireDistricts = new List<FireDistrictRight>();
            FireDistrictLefts = new List<FireDistrictLeft>();
            WaterPumps = new List<WaterPump>();
        }
        public void Draw(AcadDatabase acadDatabase)
        {
            var currentSpace = acadDatabase.CurrentSpace;
            var modelID = acadDatabase.ModelSpace.ObjectId;
            BlocksImport.ImportElementsFromStdDwg();
            var u2wMat = Active.Editor.UCS2WCS();
            foreach (var line in PipeLine)
            {
                line.TransformBy(u2wMat);
                line.LayerId = DbHelper.GetLayerId("W-FRPT-SPRL-PIPE");
                line.ColorIndex = (int)ColorIndex.BYLAYER;
                currentSpace.Add(line);

            }
            foreach (var line in NoteLine)
            {
                line.TransformBy(u2wMat);
                line.LayerId = DbHelper.GetLayerId("W-FRPT-SPRL-DIMS");
                line.ColorIndex = (int)ColorIndex.BYLAYER;
                currentSpace.Add(line);
            }
            foreach(var line in FloorLine)
            {
                line.TransformBy(u2wMat);
                line.LayerId = DbHelper.GetLayerId("W-NOTE");
                line.ColorIndex = (int)ColorIndex.BYLAYER;
                currentSpace.Add(line);
            }
            foreach(var text in Texts)
            {
                var dbText = text.DbText;
                dbText.TransformBy(u2wMat);
                currentSpace.Add(dbText);
            }
            foreach (SprayBlock block in SprayBlocks)
            {
                var blk = block.Insert(acadDatabase);
                blk.TransformBy(u2wMat);
            }
            foreach(var alarmValve in AlarmValves)
            {
                alarmValve.Insert(acadDatabase);
            }
            foreach(var fire in FireDistricts)
            {
                fire.InsertBlock(acadDatabase);
            }
            foreach (var fire in FireDistrictLefts)
            {
                fire.InsertBlock(acadDatabase);
            }
            foreach (var pump in WaterPumps)
            {
                pump.Insert(acadDatabase);
            }
        }
    }
}
