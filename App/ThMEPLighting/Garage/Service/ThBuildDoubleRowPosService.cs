using System;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThBuildDoubleRowPosService
    {
        private List<ThLightEdge> Edges { get; set; }
        private List<Tuple<Point3d, Point3d>> SplitPts { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThQueryLightBlockService QueryLightBlockService { get; set; }
        private ThBuildDoubleRowPosService(
            List<ThLightEdge> edges,
            List<Tuple<Point3d, Point3d>> splitPts,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
        {
            Edges = edges;
            SplitPts = splitPts;
            ArrangeParameter = arrangeParameter;
            QueryLightBlockService = queryLightBlockService;
        }
        public static void Build(
            List<ThLightEdge> edges,
            List<Tuple<Point3d, Point3d>> splitPts,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
        {
            var instance = new ThBuildDoubleRowPosService(edges, splitPts, arrangeParameter, queryLightBlockService);
            instance.Build();
        }
        private void Build()
        {
            SplitPts.ForEach(o =>
            {
                var splitParameter = new ThLineSplitParameter
                {
                    LineSp = o.Item1,
                    LineEp = o.Item2,
                    Margin = ArrangeParameter.Margin,
                    Interval = ArrangeParameter.Interval,
                };
                if (ArrangeParameter.AutoGenerate)
                {
                    BuildByCalculation(splitParameter);
                }
                else
                {
                    BuildByExtractFromCad(splitParameter);
                }
            });
        }
        private void BuildByCalculation(ThLineSplitParameter splitParameter)
        {
            var installPoints = ThDistributeLightService.Distribute(splitParameter);
            DistributePoints(installPoints);
        }
        private void BuildByExtractFromCad(ThLineSplitParameter splitParameter)
        {
            var line = new Line(splitParameter.LineSp, splitParameter.LineEp);
            DistributePoints(QueryLightBlockService.Query(line));
        }
        private void DistributePoints(List<Point3d> installPoints)
        {
            foreach (var pt in installPoints)
            {
                foreach (var lightEdge in Edges)
                {
                    if (ThGeometryTool.IsPointInLine(lightEdge.Edge.StartPoint, lightEdge.Edge.EndPoint, pt))
                    {
                        lightEdge.LightNodes.Add(new ThLightNode { Position = pt });
                        break;
                    }
                }
            }
        }
    }
}
