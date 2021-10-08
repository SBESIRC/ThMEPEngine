using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPWSS.Sprinkler.Service;
using System.Collections.Generic;
using NetTopologySuite.Operation.Buffer;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Sprinkler.Analysis
{
    public class ThSprinklerSoDenseChecker : ThSprinklerChecker
    {
        // 喷头区域密度阈值
        public int AreaDensity { get; set; }

        public override void Clean(Polyline pline)
        {
            CleanPline(ThSprinklerCheckerLayer.Sprinkler_So_Dense_LayerName, pline);
        }

        public override void Check(List<ThIfcDistributionFlowElement> sprinklers, List<ThGeometry> geometries, Polyline pline)
        {
            var distanceCheck = DistanceCheck(sprinklers, pline).ToList();
            var results = MergeCloseLine(distanceCheck);
            if (results.Count > 0) 
            {
                Present(results);
            }
        }

        private HashSet<Line> DistanceCheck(List<ThIfcDistributionFlowElement> sprinklers, Polyline pline)
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
                var closePointList = ThSprinklerKdTreeService.QueryOther(kdTree, position, AreaDensity);
                closePointList.ForEach(o => result.Add(new Line(position, o)));
            }
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
                    if (tag && (distanceCheck[0].Delta.GetAngleTo(o[0].Delta) < Math.PI / 60 || distanceCheck[0].Delta.GetAngleTo(o[0].Delta) > Math.PI * 59 / 60))
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
                for (int i = 0; i < o.Count; i++)
                {
                    var beMerged = false;
                    for (int j = i + 1; j < o.Count; j++)
                    {
                        if (o[i].Distance(o[j]) < 1.0)
                        {
                            o[i] = CreateLine(o[i], o[j]);
                            o.RemoveAt(j);
                            j = i + 1;
                            beMerged = true;
                        }
                    }
                    if (beMerged)
                    {
                        results.Add(o[i]);
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
    }
}
