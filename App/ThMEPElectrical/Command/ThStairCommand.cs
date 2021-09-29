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
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Stair;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
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

                var rooms = new List<Polyline>();
                var engine = new ThStairEquimentLayout();
                var normalLighting = engine.StairNormalLighting(acadDatabase.Database, rooms,frame.Vertices(), 100);
                var evacuationLighting = engine.StairEvacuationLighting(acadDatabase.Database, rooms, frame.Vertices(), 100);
                var stairFireDetector = engine.StairFireDetector(acadDatabase.Database, rooms, frame.Vertices(), 100);
                var stairStoreyMark = engine.StairStoreyMark(acadDatabase.Database, rooms,frame.Vertices(), 100);
                var stairBroadcast = engine.StairBroadcast(acadDatabase.Database, rooms, frame.Vertices(), 100);
            }
        }

        public void Dispose()
        {
            //
        }
    }
}
