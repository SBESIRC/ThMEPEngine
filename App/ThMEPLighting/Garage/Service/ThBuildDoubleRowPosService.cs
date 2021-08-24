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
        protected List<List<Point3d>> Segments { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        protected ThQueryLightBlockService QueryLightBlockService { get; set; }
        public ThBuildLightPosService(
            List<ThLightEdge> edges,
            List<List<Point3d>> segments,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService)
        {
            Edges = edges;
            Segments = segments;
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
            for (int i = 0; i < splitParameter.Segment.Count-1; i++)
            {
                var line = new Line(splitParameter.Segment[i], splitParameter.Segment[i+1]);
                var points = QueryLightBlockService.Query(line);
                points = points.OrderBy(o => o.DistanceTo(line.StartPoint)).ToList();
                DistributePoints(points);
            }
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
            List<List<Point3d>> segments,
            ThLightArrangeParameter arrangeParameter,
            ThQueryLightBlockService queryLightBlockService) 
            : base(edges, segments, arrangeParameter, queryLightBlockService)
        {
        }
        public override void Build()
        {
            Segments.ForEach(o =>
            {
                var splitParameter = new ThLineSplitParameter
                {
                    Segment= o,
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
