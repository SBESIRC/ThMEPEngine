using System;
using System.Linq;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;

using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Sprinkler.Service;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceBetweenSprinklerChecker : ThSprinklerChecker
    {
        public override void Clean(Polyline pline)
        {
            CleanDimension(ThWSSCommon.Sprinkler_Distance_LayerName, pline);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Entity entity)
        {
            var distanceCheck = DistanceCheck(sprinklers, 1799.0, entity);
            if (distanceCheck.Count > 0)
            {
                var buildingCheck = BuildingCheck(geometries, distanceCheck, entity);
                if (buildingCheck.Count > 0)
                {
                    Present(buildingCheck);
                }
            }
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, double tolerance, Entity entity)
        {
            var sprinklersClone = sprinklers.OfType<ThSprinkler>().Where(o => o.Category == Category).Where(o => entity.EntityContains(o.Position)).ToList();
            var result = new HashSet<Line>();
            while (sprinklersClone.Count > 0)
            {
                var position = sprinklersClone[0].Position;
                sprinklersClone.RemoveAt(0);
                var kdTree = new ThCADCoreNTSKdTree(1.0);
                sprinklersClone.ForEach(o => kdTree.InsertPoint(o.Position));
                var closePointList = ThSprinklerKdTreeService.QueryOther(kdTree, position, tolerance);
                closePointList.ForEach(o =>
                {
                    if (position.DistanceTo(o) > 1.0)
                    {
                        result.Add(new Line(position, o));
                    }
                });
            }
            return result;
        }

        private HashSet<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lines, Entity entity)
        {
            var polygon = entity.ToNTSPolygonalGeometry();
            var geometriesFilter = geometries.Where(g => !g.Properties.ContainsKey("Height") || Convert.ToDouble(g.Properties["Height"]) > BeamHeight).Select(g => g.Boundary).Where(g => polygon.Intersects(g.ToNTSGeometry())).ToCollection();
            var result = new HashSet<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            lines.ForEach(o =>
            {
                var filter = spatialIndex.SelectCrossingPolygon(o.Buffer(1.0));
                if (filter.Count == 0)
                {
                    result.Add(o);
                }
            });
            return result;
        }

        private void Present(HashSet<Line> result)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerDistanceCheckerLayer();
                Present(result, layerId);
            }
        }

        public override void Extract(Database database, Polyline pline)
        {
            //
        }
    }
}
