﻿using System;
using Linq2Acad;
using Catel.Linq;
using System.Linq;
using ThCADCore.NTS;
using Catel.Collections;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using ThMEPWSS.Sprinkler.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerBlindZoneChecker : ThSprinklerChecker
    {
        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries)
        {
            var distanceCheck = DistanceCheck(sprinklers);
            var results = BuildingCheck(geometries, distanceCheck);
            Present(results);
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers)
        {
            var nodeCapacity = 5;
            var sprinklersTidal = sprinklers
                .Cast<ThSprinkler>()
                .Where(o => o.Category == Category);
            var objs = new DBObjectCollection();
            sprinklersTidal.ForEach(o =>
            {
                objs.Add(new DBPoint(o.Position));
            });

            var results = new HashSet<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            sprinklersTidal.ForEach(o =>
            {
                var points = spatialIndex.NearestNeighbours(o.Position, nodeCapacity)
                                         .Cast<DBPoint>()
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

        private HashSet<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lines)
        {
            var geometriesFilter = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if ((g.Properties.ContainsKey("BottomDistanceToFloor") && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) < BeamHeight)
                 || (g.Properties["Category"] as string).Contains("Room"))
                {
                    return;
                }
                geometriesFilter.Add(g.Boundary);
            });

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
