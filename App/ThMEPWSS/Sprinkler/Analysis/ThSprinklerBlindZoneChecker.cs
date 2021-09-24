using System;
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
    public class ThSprinklerBlindZoneChecker
    {
        public HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, int tolerance)
        {
            var results = new HashSet<Line>();
            results.AddRange(DistanceCheck(sprinklers, tolerance, "上喷"));
            results.AddRange(DistanceCheck(sprinklers, tolerance, "下喷"));
            return results;
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, int tolerance, string category)
        {
            var nodeCapacity = 5;
            var sprinklersTidal = sprinklers
                .Cast<ThSprinkler>()
                .Where(o => o.Category == category);
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
                    if (p.DistanceTo(o.Position) > 2 * tolerance)
                    {
                        results.Add(new Line(o.Position, p));
                    }
                });
            });
            return results;
        }

        public HashSet<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lines, double beamHeight)
        {
            var geometriesFilter = new DBObjectCollection();
            geometries.ForEach(g =>
            {
                if (g.Properties.ContainsKey("BottomDistanceToFloor") && Convert.ToInt32(g.Properties["BottomDistanceToFloor"]) < beamHeight)
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

        public void Present(Database database, HashSet<Line> result)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = database.CreateAISprinklerBlindZoneCheckerLayer();
                var style = "TH-DIM100-W";
                var id = Dreambuild.AutoCAD.DbHelper.GetDimstyleId(style, acadDatabase.Database);
                result.ForEach(o =>
                {
                    var alignedDimension = new AlignedDimension
                    {
                        XLine1Point = o.StartPoint,
                        XLine2Point = o.EndPoint,
                        DimensionText = "",
                        DimLinePoint = ThSprinklerUtils.VerticalPoint(o.StartPoint, o.EndPoint, 2000.0),
                        ColorIndex = 256,
                        DimensionStyle = id,
                        LayerId = layerId,
                        Linetype = "ByLayer"
                    };

                    acadDatabase.ModelSpace.Add(alignedDimension);
                });
            }
        }
    }
}
