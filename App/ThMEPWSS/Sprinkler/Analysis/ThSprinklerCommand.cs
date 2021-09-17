using System;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerCommand : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            using (AcadDatabase currentDb = AcadDatabase.Active())
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

                var factory = new ThSprinklerDataSetFactory();
                var geometries = factory.Create(currentDb.Database, frame.Vertices()).Container;


                var engine = new ThTCHSprinklerRecognitionEngine();
                engine.RecognizeMS(currentDb.Database, frame.Vertices());

                // 梁高校核
                //var beamChecker = new ThSprinklerBeamChecker();
                //var objs = beamChecker.Check(geometries);
                //beamChecker.Present(currentDb.Database, objs);

                // 房间布置情况校核
                var roomChecker = new ThSprinklerRoomChecker();
                var objs = roomChecker.Check(geometries, engine.Elements);
                roomChecker.Present(currentDb.Database, objs);
            }
        }
    }
}
