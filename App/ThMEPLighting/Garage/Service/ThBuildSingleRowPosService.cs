using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThBuildSingleRowPosService
    {
        private Point3d StartPt { get; set; }
        public Point3d EndPt { get; set; }
        /// <summary>
        /// 共线且相连
        /// </summary>
        private List<ThLightEdge> Edges { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        private ThBuildSingleRowPosService(
            Point3d startPt,
            List<ThLightEdge> edges,
            ThLightArrangeParameter arrangeParameter)
        {
            StartPt = startPt;
            Edges = edges;
            ArrangeParameter = arrangeParameter;
        }
        public static ThBuildSingleRowPosService Build(
            Point3d startPt,
            List<ThLightEdge> edges,
            ThLightArrangeParameter arrangeParameter)
        {
            var instance = new ThBuildSingleRowPosService(startPt,edges, arrangeParameter);
            instance.Build();
            return instance;
        }
        private void Build()
        {
            var basePt = StartPt;
            for(int i=0;i<Edges.Count;i++)
            {
                var currentEdge = Edges[i];
                if (!currentEdge.IsDX)
                {
                    //如果是非灯线的边，在其中点创建一个灯，用于传递起始灯编号
                    var midPt = ThGeometryTool.GetMidPt(currentEdge.Edge.StartPoint, currentEdge.Edge.EndPoint);
                    currentEdge.LightNodes.Add(new ThLightNode() { Position = midPt });
                    continue;
                }
                var linkEdges = new List<ThLightEdge> { currentEdge };
                int j = i+1;
                for (; j < Edges.Count; j++)
                {
                    if (Edges[j].IsDX)
                    {
                        linkEdges.Add(Edges[j]);
                    }
                    else
                    {
                        break;
                    }
                }
                i = j - 1;
                var maxPts=linkEdges.GetMaxPts();
                var sp = maxPts.Item1;
                var ep = maxPts.Item2;
                if (basePt.DistanceTo(ep)< basePt.DistanceTo(sp))
                {
                    sp = maxPts.Item2;
                    ep = maxPts.Item1;
                }
                EndPt = ep; //记录距离StartPt最远的点
                basePt = ep;//获取下一段的起始点（主要是拐外处）
                var splitParameter = new ThLineSplitParameter()
                { 
                    LineSp=sp,
                    LineEp=ep,
                    Interval = ArrangeParameter.Interval,
                    Margin = ArrangeParameter.Margin
                };
                if (ArrangeParameter.AutoGenerate)
                {
                    BuildByCalculation(splitParameter);
                }
                else
                {
                    BuildByExtractFromCad(splitParameter);
                }
            }
        }
        private void BuildByCalculation(ThLineSplitParameter SplitParameter)
        {
            var installPoints = ThDistributeLightService.Distribute(SplitParameter);
            DistributePoints(SplitParameter.LineSp, installPoints);
        }
        private void BuildByExtractFromCad(ThLineSplitParameter SplitParameter)
        {
            if(ArrangeParameter.LightBlockQueryService==null)
            {
                return;
            }
            var line = new Line(SplitParameter.LineSp, SplitParameter.LineEp);
            var installPoints = ArrangeParameter.LightBlockQueryService.Query(line);
            installPoints = installPoints.OrderBy(o => SplitParameter.LineSp.DistanceTo(o)).ToList();
            DistributePoints(SplitParameter.LineSp, installPoints);
        }
        private void DistributePoints(Point3d startPt, List<Point3d> installPoints)
        {
            foreach (var pt in installPoints)
            {
                double disance = startPt.DistanceTo(pt);
                double length = 0.0;
                foreach (var lightEdge in Edges)
                {
                    length += lightEdge.Edge.Length;
                    if (disance <= length)
                    {
                        lightEdge.LightNodes.Add(new ThLightNode { Position = pt });
                        break;
                    }
                }
            }
        }        
    }
}
