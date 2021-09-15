using System;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using GeometryExtensions;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPElectrical.Stair;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPElectrical.Staircase;

namespace ThMEPElectrical.Command
{
    public class ThStairCommand : IAcadCommand, IDisposable
    {
        /// <summary>
        /// 图纸比例
        /// </summary>
        public double Scale { get; set; }

        public void Execute()
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var engine = new ThStairEquimentLayout();
                var normalLighting = engine.StairNormalLighting(acadDatabase.Database, frame.Vertices(), 100);
                var evacuationLighting = engine.StairEvacuationLighting(acadDatabase.Database, frame.Vertices(), 100);
                var stairFireDetector = engine.StairFireDetector(acadDatabase.Database, frame.Vertices(), 100);
                var stairStoreyMark = engine.StairStoreyMark(acadDatabase.Database, frame.Vertices(), 100);
                var stairBroadcast = engine.StairBroadcast(acadDatabase.Database, frame.Vertices(), 100);

                //var PLAT_LAYOUT_EQUIPMENT_LIST = new List<string>
                //{
                //    "E-BL302",
                //    "E-BFEL800",
                //    "E-BFAS110",
                //    "E-BFEL110",
                //    "E-BFAS410-4"
                //};
                //var HALFPLAT_LAYOUT_EQUIPMENT_LIST = new List<string>
                //{
                //    "E-BL302",
                //    "E-BFEL800"
                //};
                //var engine = new ThDB3StairRecognitionEngine();
                //engine.Recognize(acadDatabase.Database, frame.Vertices());
                //var stairs = engine.Elements.Cast<ThIfcStair>().ToList();

                //var layout = new ThStairIllumination();
                //var temp = layout.Lay(stairs, 100);

                //engine.Elements
                //    .Cast<ThIfcStair>()
                //    .ForEach(o =>
                //    {
                //        var doorsEngine = new ThStairDoorService();
                //        var doors = doorsEngine.GetDoorList(o.SrcBlock);
                //        if (o.PlatForLayout.Count != 0)
                //        {
                //            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                //            {
                //                var scale = new Scale3d(100);
                //                var layoutEngine = new ThStairElectricalEngine();
                //                for (int i = 0; i < PLAT_LAYOUT_EQUIPMENT_LIST.Count(); i++)
                //                {
                //                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(PLAT_LAYOUT_EQUIPMENT_LIST[i]), false);
                //                    var objId = layoutEngine.Insert(PLAT_LAYOUT_EQUIPMENT_LIST[i], scale);
                //                    //layoutEngine.Displacement(objId, o.PlatForLayout, doors);
                //                }
                //            }
                //        }

                //        if (o.HalfPlatForLayout.Count != 0)
                //        {
                //            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                //            {
                //                var scale = new Scale3d(100);
                //                var layoutEngine = new ThStairElectricalEngine();
                //                for (int i = 0; i < HALFPLAT_LAYOUT_EQUIPMENT_LIST.Count(); i++)
                //                {
                //                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(HALFPLAT_LAYOUT_EQUIPMENT_LIST[i]), false);
                //                    var objId = layoutEngine.Insert(HALFPLAT_LAYOUT_EQUIPMENT_LIST[i], scale);
                //                    //layoutEngine.Displacement(objId, o.HalfPlatForLayout, doors);
                //                }
                //            }
                //        }
                //    });
            }
        }

        public void Dispose()
        {
            //
        }
    }
}
