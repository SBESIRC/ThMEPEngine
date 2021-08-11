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
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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

                var PLAT_LAYOUT_EQUIPMENT_LIST = new List<string>
                {
                    "E-BL302",
                    "E-BFEL800",
                    "E-BFAS110"

                };
                var HALFPLAT_LAYOUT_EQUIPMENT_LIST = new List<string>
                {
                    "E-BL302",
                    "E-BFEL800"
                };
                var engine = new ThDB3StairRecognitionEngine();
                engine.Recognize(acadDatabase.Database, frame.Vertices());
                engine.Elements
                    .Cast<ThIfcStair>()
                    .ForEach(o =>
                    {
                        if (o.PlatForLayout.Count != 0)
                        {
                            var pline = new Polyline();
                            pline.CreatePolyline(new Point3dCollection(o.PlatForLayout.ToArray()));
                            pline.Closed = true;
                            acadDatabase.ModelSpace.Add(pline);

                            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                            {
                                var scale = new Scale3d(100);
                                var layoutEngine = new ThStairEngine();
                                for (int i = 0; i < PLAT_LAYOUT_EQUIPMENT_LIST.Count(); i++) 
                                {
                                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(PLAT_LAYOUT_EQUIPMENT_LIST[i]), false);
                                    var objId = layoutEngine.Insert(PLAT_LAYOUT_EQUIPMENT_LIST[i], scale);
                                    layoutEngine.Displacement(objId, o.PlatForLayout);
                                }
                            }
                        }

                        if (o.HalfPlatForLayout.Count != 0)
                        {
                            var halfPline = new Polyline();
                            halfPline.CreatePolyline(new Point3dCollection(o.HalfPlatForLayout.ToArray()));
                            halfPline.Closed = true;
                            acadDatabase.ModelSpace.Add(halfPline);

                            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
                            {
                                var scale = new Scale3d(100);
                                var layoutEngine = new ThStairEngine();
                                for (int i = 0; i < HALFPLAT_LAYOUT_EQUIPMENT_LIST.Count(); i++)
                                {
                                    acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(HALFPLAT_LAYOUT_EQUIPMENT_LIST[i]), false);
                                    var objId = layoutEngine.Insert(HALFPLAT_LAYOUT_EQUIPMENT_LIST[i], scale);
                                    layoutEngine.Displacement(objId, o.HalfPlatForLayout);
                                }
                            }
                        }
                    });
            }
        }

        public void Dispose()
        {
            //
        }
    }
}
