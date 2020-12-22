using System;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;

namespace ThMEPLighting.Garage.Service
{
    public class ThBuildDoubleRowPosService
    {
        /// <summary>
        /// 共线且相连
        /// </summary>
        private List<ThLightEdge> Edges { get; set; }
        private List<Tuple<Point3d, Point3d>> SplitPts { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThBuildDoubleRowPosService(
            List<ThLightEdge> edges,
            List<Tuple<Point3d,Point3d>> splitPts,
            ThLightArrangeParameter arrangeParameter)
        {
            Edges = edges;
            SplitPts = splitPts;
            ArrangeParameter = arrangeParameter;
        }
        public static void Build(
            List<ThLightEdge> edges,
            List<Tuple<Point3d, Point3d>> splitPts,
            ThLightArrangeParameter arrangeParameter)
        {
            var instance = new ThBuildDoubleRowPosService(edges, splitPts, arrangeParameter);
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
            foreach(var pt in installPoints)
            {
                foreach(var lightEdge in Edges)
                {                    
                    if(ThGeometryTool.IsPointInLine(lightEdge.Edge.StartPoint, lightEdge.Edge.EndPoint, pt))
                    {
                        lightEdge.LightNodes.Add(new ThLightNode { Position = pt });
                        break;
                    }
                }
            }
        }
        private void BuildByExtractFromCad(ThLineSplitParameter splitParameter)
        {
            throw new NotSupportedException();
        }
    }
}
