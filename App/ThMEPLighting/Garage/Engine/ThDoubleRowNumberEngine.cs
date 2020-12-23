using System;
using System.Linq;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Worker;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;

namespace ThMEPLighting.Garage.Engine
{
    public class ThDoubleRowNumberEngine : ThBuildNumberEngine
    {
        public List<ThLightEdge> FirstLightEdges { get; set; }
        public List<ThLightEdge> SecondLightEdges { get; set; }
        public ThWireOffsetDataService WireOffsetDataService { get; set; }
        public ThDoubleRowNumberEngine(
            List<Point3d> centerPorts,
            List<ThLightEdge> centerLineEdges,
            ThLightArrangeParameter arrangeParameter,
            ThWireOffsetDataService wireOffsetDataService)
            :base(centerPorts, centerLineEdges, arrangeParameter)
        {
            WireOffsetDataService = wireOffsetDataService;
            FirstLightEdges = new List<ThLightEdge>();
            SecondLightEdges = new List<ThLightEdge>();
        }
        public ThDoubleRowNumberEngine(
            List<Point3d> centerPorts,
            List<ThLightEdge> centerLineEdges,
            ThLightArrangeParameter arrangeParameter,
            Point3d centerStart,
            ThWireOffsetDataService wireOffsetDataService
            ) : base(centerPorts, centerLineEdges, arrangeParameter, centerStart)
        {
            WireOffsetDataService = wireOffsetDataService;
        }
        public override void Build()
        {
            //对传入的灯边界不在进行任何处理
            var centerLightEdges = new List<ThLightEdge>();
            LineEdges.ForEach(o => centerLightEdges.Add(o));
            var centerPorts = new List<Point3d>();
            Ports.ForEach(o => centerPorts.Add(o));
            Point3d centerStart = Start;
            do
            {
                //外圈布灯,用于创建布灯点或从Cad图纸上获取等安装位置
                var firstLightEdges = new List<ThLightEdge>();                
                using (var posDistributeEngine = new ThOuterCircleDistributionEngine(
                    centerPorts, centerLightEdges, ArrangeParameter, WireOffsetDataService, centerStart))
                {
                    posDistributeEngine.Distribute();
                    firstLightEdges = posDistributeEngine.FirstLightEdges;
                }
                firstLightEdges = firstLightEdges.Where(o => o.Edge.Length >= 1.0).ToList();
                //对firstLightEdges中的IsTraverse设为False
                firstLightEdges.ForEach(o => o.IsTraversed = false);
                //获取1号线的起点
                var firstStartPt = WireOffsetDataService.FindOuterStartPt(centerStart);
                //对1号线的边建图
                var firstLightGraph = ThLightGraphService.Build(firstLightEdges, firstStartPt.Value);
                //对1号线的边编号
                ThDoubleRowNumberService.Number(firstLightGraph, ArrangeParameter, WireOffsetDataService);
                firstLightGraph.Links.ForEach(o => FirstLightEdges.AddRange(o.Path));

                centerLightEdges = LineEdges.Where(o => o.IsTraversed == false).ToList();
                centerPorts = centerPorts.PtOnLines(centerLightEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());
                if (centerPorts.Count>0)
                {
                    centerStart = centerPorts.First();
                }
            } while (centerLightEdges.Count>0 && centerPorts.Count>0);

            //对2号线灯编号
            BuildSecondLightEdges();
        }
        private void BuildSecondLightEdges()
        {
            int loopCharLength = ThDoubleRowLightNumber.GetLoopCharLength(ArrangeParameter.LoopNumber);
            FirstLightEdges.Where(o=>o.IsDX).ForEach(m =>
            {
                var first = WireOffsetDataService.FindFirstBySplitLine(m.Edge);
                var second = WireOffsetDataService.FindSecondByFirst(first);
                var secondEdgeSp = m.Edge.StartPoint.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
                var secondEdgeEp = m.Edge.EndPoint.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
                var secondLightEdge = new ThLightEdge
                {
                    Edge = new Line(secondEdgeSp, secondEdgeEp),
                    Id=Guid.NewGuid().ToString(),
                    Direction= m.Direction
                };
                m.LightNodes.ForEach(n =>
                {
                    if(!string.IsNullOrEmpty(n.Number))
                    {
                        int firstLightIndex=n.GetIndex();
                        if(firstLightIndex != -1)
                        {
                            var position = n.Position.GetProjectPtOnLine(second.StartPoint, second.EndPoint);
                            int secondLightIndex = firstLightIndex+1;
                            var number = secondLightIndex.ToString().PadLeft(loopCharLength, '0');
                            var secondLightNode = new ThLightNode
                            {
                                Number= ThGarageLightCommon.LightNumberPrefix + number,
                                Position= position
                            };
                            secondLightEdge.LightNodes.Add(secondLightNode);
                        }
                    }
                });
                SecondLightEdges.Add(secondLightEdge);
            });
        }
    }
}
