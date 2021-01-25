using System;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Worker;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Engine
{
    public class ThDoubleRowNumberEngine : ThBuildNumberEngine
    {
        public List<ThLightEdge> FirstLightEdges { get; set; }
        public List<ThLightEdge> SecondLightEdges { get; set; }
        public ThWireOffsetDataService WireOffsetDataService { get; set; }
        private List<Point3d> FirstPorts { get; set; }
        private Point3d FirstStart { get; set; }
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
            FirstPorts = new List<Point3d>();
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
            FirstPorts = new List<Point3d>();
        }
        public override void Build()
        {
            //对传入的灯边界不在进行任何处理
            //1.  通过灯线中心线,获取1号线对应的边
            //2.  通过中心线起始点获取1号线起始点
            //3.  根据1号线起始点，创建LightGraph
            //4.  LightGraph产生的路径，设置布灯点
            //4.1 对每一条直路径进行分析，如果支路已经布灯，则此区域跳过
            //4.2 再根据布灯逻辑进行布点
            //5.  编号

            var firstLightEdges = GetFirstEdges();             
            do
            {
                if (firstLightEdges.Where(o => o.IsDX).Count() == 0)
                {
                    break;
                }
                //对1号线的边建图
                if(LineEdges.Count>0)
                {
                    //获取中心线路径最长的路径
                    var centerLightGraph = ThLightGraphService.Build(LineEdges, Start);
                    centerLightGraph = ThFindLongestPathService.Find(Ports, centerLightGraph);
                    //获取1号边的端口点和起始点
                    GetFirstPortsAndStart(firstLightEdges, centerLightGraph.StartPoint);
                } 
                //为1号边建图
                var firstLightGraph = ThLightGraphService.Build(firstLightEdges, FirstStart);
                //布点
                var distributedEdges = ThDoubleRowDistributeService.Distribute(
                    firstLightGraph, ArrangeParameter, WireOffsetDataService);
                UpdateLoopNumber(firstLightGraph);
                //获取 firstLightGraph 中所有的边
                var firstGraphEdges = new List<ThLightEdge>();
                firstLightGraph.Links.ForEach(o=>firstGraphEdges.AddRange(o.Path));
                firstGraphEdges.ForEach(o => o.IsTraversed = false);
                firstLightGraph = ThLightGraphService.Build(firstGraphEdges, firstLightGraph.StartPoint);
                //对1号线的边编号
                ThDoubleRowNumberService.Number(firstLightGraph, ArrangeParameter, WireOffsetDataService);
                firstLightGraph.Links.ForEach(o => FirstLightEdges.AddRange(o.Path));

                //过滤还未遍历的边
                firstLightEdges = firstLightEdges.Where(o => o.IsTraversed == false).ToList();
                FirstPorts = FirstPorts.PtOnLines(firstLightEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());
                if (FirstPorts.Count>0)
                {
                    FirstStart = FirstPorts.First();
                }
                else if (firstLightEdges.Where(o => o.IsDX).Count() > 0)
                {
                    FirstStart = firstLightEdges.Where(o => o.IsDX).First().Edge.StartPoint;
                }
                else
                {
                    break;
                }
                //过滤中心线未遍历的边
                LineEdges = LineEdges.Where(o => o.IsTraversed == false).ToList();
                //过滤剩下的端口，
                Ports = Ports.PtOnLines(LineEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());
                //设置新的中心线起点
                if (Ports.Count > 0)
                {
                    Start = Ports.First();
                }
                else if (LineEdges.Where(o => o.IsDX).Count() > 0)
                {
                    Start = LineEdges.Where(o => o.IsDX).First().Edge.StartPoint;
                }
            } while (firstLightEdges.Count>0);

            //对2号线灯编号
            BuildSecondLightEdges();
            //指定边类型
            FirstLightEdges.ForEach(o => o.Pattern = EdgePattern.First);
            SecondLightEdges.ForEach(o => o.Pattern = EdgePattern.Second);
        }
        private void GetFirstPortsAndStart(List<ThLightEdge> firstLightEdges,Point3d centerStart)
        {
            var portsInstance = ThFindFirstEdgePortsService.Find(
                firstLightEdges.Select(o => o.Edge).ToList(), Ports, ArrangeParameter.RacywaySpace / 2.0);
            FirstPorts = portsInstance.Ports;
            Point3d? firstStartValue = portsInstance.FindFirstPtByCenterPt(centerStart);
            if (firstStartValue.HasValue)
            {
                FirstStart = firstStartValue.Value;
            }
            else if (firstLightEdges.Count > 0)
            {
                FirstStart = firstLightEdges[0].Edge.StartPoint;
            }
        }
        private List<ThLightEdge> GetFirstEdges()
        {
            var firstLightEdges = new List<ThLightEdge>();
            var firstSplitLines = new List<Line>();
            LineEdges.ForEach(o =>
            {
                var first = WireOffsetDataService.FindFirstByCenter(o.Edge);
                firstSplitLines.AddRange(WireOffsetDataService.FindFirstSplitLines(first));
            });
            firstSplitLines.ForEach(o => firstLightEdges.Add(new ThLightEdge(o) { IsDX = true }));
            return firstLightEdges;
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
        /// <summary>
        /// 自动计算灯回路编号
        /// </summary>
        /// <param name="lightGraph"></param>
        private void UpdateLoopNumber(ThLightGraphService lightGraph)
        {
            if (ArrangeParameter.AutoCalculate)
            {
                int numOfLights = 0;
                lightGraph.Links.ForEach(l => l.Path.ForEach(p => numOfLights += p.LightNodes.Count));
                ArrangeParameter.LoopNumber = CalculateLoopNumber(numOfLights)*2;
            }
        }
        private int CalculateLoopNumber(int lightNumbers)
        {
            double number = Math.Ceiling(lightNumbers * 1.0 / 25);
            if (number < 4)
            {
                number = 4;
            }
            return (int)number;
        }
    }
}
