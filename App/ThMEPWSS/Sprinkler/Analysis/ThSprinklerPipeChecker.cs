using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Sprinkler.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerPipeChecker : ThSprinklerChecker
    {
        public DBObjectCollection PipeLine { get; set; }

        public ThSprinklerPipeChecker()
        {
            PipeLine = new DBObjectCollection();
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
            var results = new List<Point3d>();
            var points = sprinklers
                    .OfType<ThSprinkler>()
                    .Where(o => o.Category == Category)
                    .Where(o => entity.EntityContains(o.Position))
                    .Select(o => o.Position)
                    .ToList();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(PipeLine);
            points.ForEach(o =>
            {
                var circle = new Circle(o, Vector3d.ZAxis, 10.0);
                var filter = spatialIndex.SelectCrossingPolygon(circle.TessellateCircleWithArc(1.0 * Math.PI));
                if (filter.Count == 0)
                {
                    results.Add(o);
                }
            });
            return results;
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
        }
    }
}
