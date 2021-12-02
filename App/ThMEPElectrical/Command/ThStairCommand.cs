using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Linq2Acad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Stair;

namespace ThMEPElectrical.Command
{
    public class ThStairCommand : ThMEPBaseCommand, IDisposable
    {
        public ThStairCommand()
        {
            CommandName = "THLTSBBZ";
            ActionName = "楼梯设备布置";
        }

        public override void SubExecute()
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

                var rooms = new List<Polyline>();
                var engine = new ThStairEquimentLayout();

                var scale = 100.0;
                var normalLighting = engine.StairNormalLighting(acadDatabase.Database, rooms,frame.Vertices(), scale);
                var evacuationLighting = engine.StairEvacuationLighting(acadDatabase.Database, rooms, frame.Vertices(), scale);
                var stairFireDetector = engine.StairFireDetector(acadDatabase.Database, rooms, frame.Vertices(), scale);
                var stairStoreyMark = engine.StairStoreyMark(acadDatabase.Database, rooms,frame.Vertices(), scale);
                var stairBroadcast = engine.StairBroadcast(acadDatabase.Database, rooms, frame.Vertices(), scale);
            }
        }

        public void Dispose()
        {
            //
        }
    }
}
