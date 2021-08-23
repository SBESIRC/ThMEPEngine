using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public abstract class ThBuildLightPosService
    {
        protected List<ThLightEdge> Edges { get; set; }
        protected List<Tuple<Point3d, Point3d>> SplitPts { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        protected ThQueryLightBlockService QueryLightBlockService { get; set; }
        public ThBuildLightPosService(
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
        public abstract void Build();
        protected virtual void BuildByCalculation(ThLineSplitParameter splitParameter)
        {
            var installPoints = ThDistributeLightService.Distribute(splitParameter);
            DistributePoints(installPoints);
        }
        protected virtual void BuildByExtractFromCad(ThLineSplitParameter splitParameter)
        {
            var line = new Line(splitParameter.LineSp, splitParameter.LineEp);
            var points = QueryLightBlockService.Query(line);
            points = points.OrderBy(o => o.DistanceTo(splitParameter.LineSp)).ToList();
            DistributePoints(points);
        }
        protected virtual void DistributePoints(List<Point3d> installPoints)
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
    public class ThBuildDoubleRowPosService : ThBuildLightPosService
    {
        public ThBuildDoubleRowPosService(
            List<ThLightEdge> edges,
            List<Tuple<Point3d, Point3d>> splitPts,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService) 
            : base(edges, splitPts, arrangeParameter, queryLightBlockService)
        {
        }
        public override void Build()
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
    }
}
