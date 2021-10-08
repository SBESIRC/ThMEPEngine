using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDistanceBetweenSprinklerChecker : ThSprinklerChecker
    {
        public override void Clean(Polyline pline)
        {
            CleanDimension(ThSprinklerCheckerLayer.Sprinkler_Distance_LayerName, pline);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var distanceCheck = DistanceCheck(sprinklers, 1800.0, pline);
            if (distanceCheck.Count > 0)
            {
                var buildingCheck = BuildingCheck(geometries, distanceCheck, pline);
                if (buildingCheck.Count > 0) 
                {
                    Present(buildingCheck);
                }
            }
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, double tolerance, Polyline pline)
        {
            var sprinklersClone = sprinklers.OfType<ThSprinkler>()
                                            .Where(o => o.Category == Category)
                                            .Where(o => pline.Contains(o.Position))
                                            .ToList();
            var result = new HashSet<Line>();
            while (sprinklersClone.Count > 0) 
            {
                var position = sprinklersClone[0].Position;
                sprinklersClone.RemoveAt(0);
                var kdTree = new ThCADCoreNTSKdTree(1.0);
                sprinklersClone.ForEach(o => kdTree.InsertPoint(o.Position));
                var closePointList = ThSprinklerKdTreeService.QueryOther(kdTree, position, tolerance);
                closePointList.ForEach(o => result.Add(new Line(position, o)));
            }
            return result;
        }

        private HashSet<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lines, Polyline pline)
        {
            var polygon = pline.ToNTSPolygon();
            var geometriesFilter = geometries.Where(g => !g.Properties.ContainsKey("BottomDistanceToFloor")
                                                      || Convert.ToDouble(g.Properties["BottomDistanceToFloor"]) > BeamHeight)
                                             .Select(g => g.Boundary)
                                             .Where(g => polygon.Intersects(g.ToNTSGeometry()))
                                             .ToCollection();
            var result = new HashSet<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            lines.ForEach(o =>
            {
                var line = new Line(o.StartPoint, o.EndPoint);
                var filter = spatialIndex.SelectCrossingPolygon(line.Buffer(1.0));
                if(filter.Count == 0)
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
    }
}
