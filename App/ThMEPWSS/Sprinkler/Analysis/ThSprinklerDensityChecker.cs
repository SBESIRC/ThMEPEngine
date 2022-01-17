using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using NetTopologySuite.Operation.Buffer;

using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPWSS.Sprinkler.Service;
using ThCADExtension;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerDensityChecker : ThSprinklerChecker
    {
        // 喷头区域密度阈值
        public int AreaDensity { get; set; }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThWSSCommon.Sprinkler_So_Dense_LayerName, pline);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Entity entity)
        {
            var distanceCheck = DistanceCheck(sprinklers, entity);
            var buildingCheck = BuildingCheck(geometries, distanceCheck, entity);
            var results = MergeCloseLine(buildingCheck);
            if (results.Count > 0)
            {
                Present(results);
            }
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, Entity entity)
        {
            var sprinklersClone = sprinklers.OfType<ThSprinkler>()
                                            .Where(o => o.Category == Category)
                                            .Where(o => entity.EntityContains(o.Position))
                                            .ToList();
            var result = new HashSet<Line>();
            while (sprinklersClone.Count > 0)
            {
                var position = sprinklersClone[0].Position;
                sprinklersClone.RemoveAt(0);
                var kdTree = new ThCADCoreNTSKdTree(1.0);
                sprinklersClone.ForEach(o => kdTree.InsertPoint(o.Position));
                var closePointList = ThSprinklerKdTreeService.QueryOther(kdTree, position, AreaDensity);
                closePointList.ForEach(o => result.Add(new Line(position, o)));
            }
            return result;
        }

        private List<Line> BuildingCheck(List<ThGeometry> geometries, HashSet<Line> lines, Entity entity)
        {
            var polygon = entity.ToNTSPolygonalGeometry();
            var geometriesFilter = geometries.Where(g => !g.Properties.ContainsKey("BottomDistanceToFloor")
                                                      || Convert.ToDouble(g.Properties["BottomDistanceToFloor"]) > BeamHeight)
                                             .Select(g => g.Boundary)
                                             .Where(g => polygon.Intersects(g.ToNTSGeometry()))
                                             .ToCollection();
            var result = new List<Line>();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(geometriesFilter);
            lines.ForEach(o =>
            {
                var line = new Line(o.StartPoint, o.EndPoint);
                var filter = spatialIndex.SelectCrossingPolygon(line.Buffer(1.0));
                if (filter.Count == 0)
                {
                    result.Add(o);
                }
            });
            return result;
        }

        private HashSet<Line> MergeCloseLine(List<Line> distanceCheck)
        {
            var lineDirection = new List<List<Line>>();
            while (distanceCheck.Count > 0)
            {
                var tag = true;
                lineDirection.ForEach(o =>
                {
                    if (tag && (distanceCheck[0].LineDirection().GetAngleTo(o[0].LineDirection()) < Math.PI / 60 
                               || distanceCheck[0].LineDirection().GetAngleTo(o[0].LineDirection()) > Math.PI * 59 / 60))
                    {
                        o.Add(distanceCheck[0]);
                        distanceCheck.RemoveAt(0);
                        tag = false;
                    }
                });
                if (tag)
                {
                    lineDirection.Add(new List<Line> { distanceCheck[0] });
                    distanceCheck.RemoveAt(0);
                }
            }

            var results = new HashSet<Line>();
            lineDirection.ForEach(o =>
            {
                var record = new HashSet<int>();
                for (int i = 0; i < o.Count; i++)
                {
                    if (record.Contains(i))
                    {
                        continue;
                    }
                    record.Add(i);
                    var beMerged = false;
                    var mergedLine = new Line(o[i].StartPoint, o[i].EndPoint);
                    for (int j = 0; j < o.Count; j++)
                    {
                        if (record.Contains(j))
                        {
                            continue;
                        }
                        if (mergedLine.Distance(o[j]) < 1.0)
                        {
                            mergedLine = CreateLine(mergedLine, o[j]);
                            record.Add(j);
                            beMerged = true;
                            j = -1;
                        }
                    }
                    if (beMerged)
                    {
                        results.Add(mergedLine);
                    }
                }
            });
            return results;
        }

        private Line CreateLine(Line first, Line second)
        {
            var list = new List<Line>
            {
                first,
                second,
                new Line(first.StartPoint, second.StartPoint),
                new Line(first.StartPoint, second.EndPoint),
                new Line(first.EndPoint, second.StartPoint),
                new Line(first.EndPoint, second.EndPoint)
            };
            return list.OrderByDescending(o => o.Length).First();
        }

        private void Present(HashSet<Line> result)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var layerId = acadDatabase.Database.CreateAISprinklerSoDenseLayer();
                foreach (Line line in result)
                {
                    var pline = Buffer(line, 1000);
                    acadDatabase.ModelSpace.Add(pline);
                    pline.LayerId = layerId;
                    pline.ConstantWidth = 100;
                }
            }
        }

        private static Polyline Buffer(Line line, double distance)
        {
            return line.ToNTSLineString().Buffer(distance, EndCapStyle.Square).ToDbObjects()[0] as Polyline;
        }

        public override void Extract(Database database, Polyline pline)
        {
            //
        }
    }
}
