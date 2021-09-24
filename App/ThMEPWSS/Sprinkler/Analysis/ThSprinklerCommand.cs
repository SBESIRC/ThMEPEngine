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

                var recognizeAllEngine = new ThTCHSprinklerRecognitionEngine();
                recognizeAllEngine.RecognizeMS(currentDb.Database);

                // 梁高校核
                //var beamChecker = new ThSprinklerBeamChecker();
                //var objs = beamChecker.Check(geometries);
                //beamChecker.Present(currentDb.Database, objs);

                // 房间布置情况校核
                //var roomChecker = new ThSprinklerRoomChecker();
                //var objsTidal = roomChecker.Check(geometries, recognizeAllEngine.Elements);
                //roomChecker.Present(currentDb.Database, objsTidal);

                // 喷头间间距校核
                //var distanceChecker = new ThSprinklerDistanceBetweenSprinklerChecker();
                //var distanceCheck = distanceChecker.DistanceCheck(engine.Elements, 1800.0);
                //var buildingCheck = distanceChecker.BuildingCheck(geometries, distanceCheck);
                //distanceChecker.Present(currentDb.Database, buildingCheck);

                // 喷头距边校核
                //var boundaryChecker = new ThSprinklerDistanceFromBoundarySoCloseChecker();
                //var results = boundaryChecker.DistanceCheck(engine.Elements, geometries);
                //boundaryChecker.Present(currentDb.Database, results);

                // 计算可布置区域
                //var layoutAreasChecker = new ThSprinklerDistanceFromBeamChecker();
                //var areas = layoutAreasChecker.LayoutAreas(geometries);
                //layoutAreasChecker.Present(currentDb.Database, areas);
                //var results = layoutAreasChecker.BeamCheck(recognizeAllEngine.Elements, areas, geometries);
                //layoutAreasChecker.Present(currentDb.Database, results);

                // 盲区校核
                //var blindZoneChecker = new ThSprinklerBlindZoneChecker();
                //var distanceCheck = blindZoneChecker.DistanceCheck(engine.Elements, 2200);
                //var results = blindZoneChecker.BuildingCheck(geometries, distanceCheck, 700);
                //blindZoneChecker.Present(currentDb.Database, results);

                // 喷头距边是否过大
                var boundaryChecker = new ThSprinklerDistanceFromBoundarySoFarChecker();
                var results = boundaryChecker.DistanceCheck(engine.Elements, geometries, 3111, 2200);
                boundaryChecker.Present(currentDb.Database, results);
            }
        }
    }
}
