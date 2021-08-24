using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;

namespace ThMEPLighting.Garage.Engine
{
    /// <summary>
    /// 单排编号
    /// </summary>
    public class ThSingleRowNumberEngine : ThBuildNumberEngine, IDisposable
    {
        public List<ThLightEdge> DxLightEdges { get; set; }
        public ThQueryLightBlockService QueryLightBlockService { get; set; }
        public ThSingleRowNumberEngine(
            List<Point3d> ports,
            List<ThLightEdge> lightEdges,
            ThLightArrangeParameter arrangeParameter)
            : base(ports, lightEdges, arrangeParameter)
        {
            DxLightEdges = new List<ThLightEdge>();
        }
        public void Dispose()
        {
        }
        public override void Build()
        {
            //对传入的灯边界不在进行任何处理            
            if (LineEdges.Count == 0)
            {
                return;
            }
            Point3d findSp = LineEdges[0].Edge.StartPoint;
            var lightEdges = new List<ThLightEdge>();
            LineEdges.ForEach(o => lightEdges.Add(o)); //包括Dx和Fdx边界
            do
            {
                if (lightEdges.Where(o => o.IsDX).Count() == 0)
                {
                    break;
                }
                if (Ports.Count > 0)
                {
                    findSp = Ports[0];
                }
                else 
                {
                    findSp = lightEdges.Where(o => o.IsDX).First().Edge.StartPoint;
                }                

                //对灯线边建图,创建从findSp开始可以连通的图
                var lightGraph = new ThCdzmLightGraphService(lightEdges, findSp);
                lightGraph.Build();

                //找到从ports中的点出发拥有最长边的图
                var centerEdges = new List<ThLightEdge>();
                lightGraph.Links.ForEach(o => o.Path.ForEach(p => centerEdges.Add(new ThLightEdge(p.Edge))));
                var centerStart = LaneServer.getMergedOrderedLane(centerEdges);
                centerEdges.ForEach(o => o.IsTraversed = false);

                lightGraph = new ThCdzmLightGraphService(centerEdges, centerStart);
                lightGraph.Build();

                var distributeService = new ThSingleRowDistributeService(
                    lightGraph, ArrangeParameter, QueryLightBlockService);
                distributeService.Distribute();                
                UpdateLoopNumber(lightGraph);
                ThSingleRowNumberService.Number(lightGraph, ArrangeParameter);
                lightGraph.Links.ForEach(o => DxLightEdges.AddRange(o.Path));
                lightEdges = LineEdges.Where(o => o.IsTraversed == false).ToList();
                Ports = Ports.PtOnLines(lightEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());  //更新端口点             
            } while (lightEdges.Count > 0);
            //指定为中心线
            DxLightEdges.ForEach(o => o.Pattern = EdgePattern.Center);
        }
        private void UpdateLoopNumber(ThLightGraphService lightGraph)
        {
            if (ArrangeParameter.AutoCalculate)
            {
                if (lightGraph == null)
                {
                    return;
                }
                int numOfLights = 0;
                lightGraph.Links.ForEach(l => l.Path.ForEach(p => numOfLights += p.LightNodes.Count));
                ArrangeParameter.LoopNumber = CalculateLoopNumber(numOfLights);
            }
            else
            {
                if (ArrangeParameter.LoopNumber < 2)
                {
                    ArrangeParameter.LoopNumber = 2;
                }
            }
        }
        private int CalculateLoopNumber(int lightNumbers)
        {
            double number = Math.Ceiling(lightNumbers * 1.0 / 25);
            if (number < 3)
            {
                number = 3;
            }
            return (int)number;
        }
    }
}
