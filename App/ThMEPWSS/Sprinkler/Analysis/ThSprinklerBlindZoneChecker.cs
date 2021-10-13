using System;
using NFox.Cad;
using Linq2Acad;
using Catel.Linq;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerBlindZoneChecker : ThSprinklerChecker
    {
        public override void Clean(Polyline pline)
        {
            CleanDimension(ThSprinklerCheckerLayer.Blind_Zone_LayerName, pline);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var distanceCheck = DistanceCheck(sprinklers, pline);
            if (distanceCheck.Count > 0) 
            {
                var results = BuildingCheck(geometries, distanceCheck, pline);
                if (results.Count > 0)
                {
                    Present(results);
                }
            }
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, Polyline pline)
        {
            var nodeCapacity = 5;
            var objs = sprinklers
                .OfType<ThSprinkler>()
                .Where(o => o.Category == Category)
                .Where(o => pline.Contains(o.Position))
                .Select(o => new DBPoint(o.Position))
                .ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var results = new HashSet<Line>();
            var num = spatialIndex.SelectAll().Count;
            if (num <= 1) 
            {
                return results;
            }
            else if (num > 1 && num < 5) 
            {
                nodeCapacity = num; 
            }
            objs.OfType<DBPoint>().ForEach(o =>
            {
                var points = spatialIndex.NearestNeighbours(o.Position, nodeCapacity)
                                         .OfType<DBPoint>()
                                         .Select(o => o.Position)
                                         .ToList();
                points.ForEach(p =>
                {
                    if (p.DistanceTo(o.Position) > 2 * RadiusB)
                    {
                        results.Add(new Line(o.Position, p));
                    }
                });
            });
            return results;
        }

        private HashSet<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lines, Polyline pline)
        {
            var polygon = pline.ToNTSPolygon();
            var geometriesFilter = geometries.Where(g => !((g.Properties.ContainsKey("BottomDistanceToFloor") 
                                                         && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) < BeamHeight)
                                                         || (g.Properties["Category"] as string).Contains("Room")))
                                             .Select(g => g.Boundary)
                                             .Where(g => polygon.Intersects(g.ToNTSGeometry()))
                                             .ToCollection();
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
                var layerId = acadDatabase.Database.CreateAISprinklerBlindZoneCheckerLayer();
                Present(result, layerId);
            }
        }

    }
}
