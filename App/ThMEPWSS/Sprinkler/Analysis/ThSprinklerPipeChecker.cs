using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Engine;
using ThMEPWSS.Sprinkler.Data;
using ThMEPWSS.Sprinkler.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerPipeChecker : ThSprinklerChecker
    {
        private DBObjectCollection PipeLine { get; set; }
        private List<Polyline> Indicator { get; set; }

        public ThSprinklerPipeChecker()
        {
            PipeLine = new DBObjectCollection();
            Indicator = new List<Polyline>();
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Entity entity)
        {
            var results = Check(sprinklers, entity);
            if (results.Count > 0)
            {
                Present(results);
            }
        }

        private List<Point3d> Check(List<ThIfcDistributionFlowElement> sprinklers, Entity entity)
        {
            var searchPoint = new List<Point3d>();
            var points = sprinklers
                .OfType<ThSprinkler>()
                .Where(o => o.Category == Category)
                .Where(o => entity.EntityContains(o.Position))
                .Select(o => o.Position)
                .ToList();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(PipeLine);
            Indicator.ForEach(o =>
            {
                var searchFrame = Buffer(o);
                var filter = spatialIndex.SelectCrossingPolygon(searchFrame);
                while (filter.Count > 0)
                {
                    var newObjs = new DBObjectCollection();
                    filter.OfType<Line>().ForEach(l => newObjs.Add(l));
                    newObjs.Add(searchFrame);
                    searchFrame = newObjs.Buffer(10.0)
                        .OfType<Polyline>()
                        .OrderByDescending(p => p.Area)
                        .First();
                    spatialIndex.Update(new DBObjectCollection(), filter);
                    filter = spatialIndex.SelectCrossingPolygon(searchFrame);
                }
                points.Except(searchPoint).ToList().ForEach(p =>
                {
                    if (searchFrame.Contains(p) || searchFrame.Distance(p) < 10.0)
                    {
                        searchPoint.Add(p);
                    }
                });
            });

            return points.Except(searchPoint).ToList();
        }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThWSSCommon.Pipe_Checker_LayerName, pline);
        }

        private void Present(List<Point3d> results)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAIPipeCheckerLayer();
                results.ForEach(o =>
                {
                    var circle = new Circle(o, Vector3d.ZAxis, 200.0);
                    var pline = circle.TessellateCircleWithChord(200.0);
                    acadDatabase.ModelSpace.Add(pline);
                    pline.LayerId = layerId;
                    pline.ConstantWidth = 50;
                });
            }
        }

        public override void Extract(Database database, Polyline pline)
        {
            var pipe = new SprayPipe();
            pipe.Extract(database, pline.Vertices());//提取管道
            PipeLine = pipe.CreateSprayLines().ToCollection();//生成管道线

            var nameFilter = new List<string>
            {
                "水流指示器",
                "信号阀+水流指示器",
                "信号阀＋水流指示器",
            };
            var engine = new ThWaterFlowIndicatorEngine
            {
                NameFilter = nameFilter,
            };
            engine.ExtractFromMS(database);
            var geometries = engine.Results.Select(o => o.Geometry).ToCollection();
            var index = new ThCADCoreNTSSpatialIndex(geometries);
            Indicator.AddRange(index.SelectCrossingPolygon(pline)
                .OfType<Polyline>()
                .Where(o => o.Bounds.HasValue)
                .ToList());

            var tchEngine = new ThTCHWaterIndicatorRecognitionEngine();
            tchEngine.RecognizeMS(database, pline.Vertices());
            Indicator.AddRange(tchEngine.Results);
        }

        private Polyline Buffer(Polyline pline)
        {
            return Buffer(new DBObjectCollection { pline });
        }

        private Polyline Buffer(DBObjectCollection objs)
        {
            return objs.Buffer(10.0).OfType<Polyline>().OrderByDescending(p => p.Area).First();
        }
    }
}
