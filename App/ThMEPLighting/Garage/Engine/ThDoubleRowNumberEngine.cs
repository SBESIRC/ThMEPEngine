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
    public class ThDoubleRowNumberEngine : ThBuildNumberEngine, IDisposable
    {
        public List<ThLightEdge> FirstLightEdges { get; set; }
        public List<ThLightEdge> SecondLightEdges { get; set; }
        public ThWireOffsetDataService WireOffsetDataService { get; set;}
        public ThQueryLightBlockService QueryLightBlockService { get; set; }
        public ThDoubleRowNumberEngine(
            List<Point3d> centerPorts,
            List<ThLightEdge> centerLineEdges,
            List<ThLightEdge> firstLightEdges,
            ThLightArrangeParameter arrangeParameter):base(centerPorts, centerLineEdges, arrangeParameter)
        {
            FirstLightEdges = firstLightEdges;            
            SecondLightEdges = new List<ThLightEdge>();
        }
        public void Dispose()
        {
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

            //获取一号线上所有的端口点
            var firstPorts = GetFirstLinePorts(FirstLightEdges.Select(o => o.Edge).ToList());
            if(LineEdges.Count==0 || FirstLightEdges.Count==0)
            {
                return;
            }
            Point3d firstStart= LineEdges[0].Edge.StartPoint;
            var nubmeredLightEdges = new List<ThLightEdge>();
            do
            {
                if (FirstLightEdges.Where(o => o.IsDX).Count() == 0 )
                {
                    break;
                }
                #region ---------获取图的起点----------
                if (LineEdges.Count>0)
                {
                    //优先获取具有端口点的边
                    if(Ports.Count > 0)
                    {
                        var res = FindPropertyStart(LineEdges, Ports[0]);
                        if (res != null)
                        {
                            firstStart = GetFirstStart(firstPorts, res.Value);
                        }
                        else if (firstPorts.Count > 0)
                        {
                            firstStart = firstPorts[0];
                        }
                        else
                        {
                            firstStart = FirstLightEdges[0].Edge.StartPoint;
                        }
                    }
                    else if(firstPorts.Count>0)
                    {
                        var centerLinePorts = new List<Point3d>();
                        LineEdges.ForEach(l =>
                        {
                            centerLinePorts.Add(l.Edge.StartPoint);
                            centerLinePorts.Add(l.Edge.EndPoint);
                        });
                        var centerPort = GetCenterPort(centerLinePorts, firstPorts[0]); //获取1号边端口点对应的中心点
                        var res = FindPropertyStart(LineEdges, centerPort); //获取中心线上最优的起点
                        if(res != null)
                        {
                            firstStart = GetFirstStart(firstPorts, res.Value); //获取中心线上最优的起点对应的1号线的起点
                        }
                        else
                        {
                            firstStart = firstPorts[0];
                        }
                    }
                }
                else if(firstPorts.Count>0)
                {
                    //当中心线遍历完了，还剩余1号边的话，继续遍历
                    firstStart = firstPorts[0];
                }
                else 
                {
                    //表示中心线全部遍历完
                    break;
                }
                #endregion
                //为1号边建图
                var firstLightGraph = ThLightGraphService.Build(FirstLightEdges, firstStart);
                if(firstLightGraph==null)
                {
                    return;
                }
                //布点
                var distributedEdges = ThDoubleRowDistributeService.Distribute(
                    firstLightGraph, ArrangeParameter, WireOffsetDataService, QueryLightBlockService);
                UpdateLoopNumber(firstLightGraph);
                //对1号线的边编号
                ThDoubleRowNumberService.Number(firstLightGraph, ArrangeParameter, WireOffsetDataService);

                //过滤还未遍历的边
                nubmeredLightEdges.AddRange(FirstLightEdges.Where(o => o.IsTraversed).ToList());
                FirstLightEdges = FirstLightEdges.Where(o => o.IsTraversed == false).ToList();
                firstPorts = firstPorts.PtOnLines(FirstLightEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());
                //过滤中心线未遍历的边
                LineEdges = LineEdges.Where(o => o.IsTraversed == false).ToList();
                //过滤剩下的端口，
                Ports = Ports.PtOnLines(LineEdges.Where(o => o.IsDX).Select(o => o.Edge).ToList());
            } while (FirstLightEdges.Count>0);

            //对2号线灯编号
            BuildSecondLightEdges(nubmeredLightEdges);
            //指定边类型
            FirstLightEdges = nubmeredLightEdges;
            FirstLightEdges.ForEach(o => o.Pattern = EdgePattern.First);
            SecondLightEdges.ForEach(o => o.Pattern = EdgePattern.Second);
        }
        private Point3d? FindPropertyStart(List<ThLightEdge> lineEdges,Point3d start)
        {
            if (lineEdges.Count > 0)
            {
                //获取中心线路径最长的路径
                var centerLightGraph = ThLightGraphService.Build(LineEdges, start);
                var centerEdges = new List<ThLightEdge>();
                centerLightGraph.Links.ForEach(o => o.Path.ForEach(p => centerEdges.Add(new ThLightEdge(p.Edge))));
                return LaneServer.getMergedOrderedLane(centerEdges);
            }
            else
            {
                return null;
            }
        }
        private Point3d GetFirstStart(List<Point3d> firstPorts, Point3d centerStart)
        {
            var res = firstPorts.OrderBy(o => o.DistanceTo(centerStart));
            foreach (var pt in res)
            {
                var dis = Math.Abs(pt.DistanceTo(centerStart) - ArrangeParameter.RacywaySpace / 2.0);
                if(dis<=20.0)
                {
                    return pt;
                }
            }
            return res.First();
        }
        private Point3d GetCenterPort(List<Point3d> centerPorts, Point3d firstPort)
        {
            var res = centerPorts.OrderBy(o => o.DistanceTo(firstPort));
            foreach (var pt in res)
            {
                var dis = Math.Abs(pt.DistanceTo(firstPort) - ArrangeParameter.RacywaySpace / 2.0);
                if (dis <= 20.0)
                {
                    return pt;
                }
            }
            return res.First();
        }

        private void BuildSecondLightEdges(List<ThLightEdge> numberedEdges)
        {
            int loopCharLength = ThDoubleRowLightNumber.GetLoopCharLength(ArrangeParameter.LoopNumber);
            numberedEdges.Where(o=>o.IsDX).Where(o=>o.Edge.Length>0).ForEach(m =>
            {
                var first = WireOffsetDataService.FindFirstByPt(m.Edge.StartPoint.GetMidPt(m.Edge.EndPoint));
                var seconds = WireOffsetDataService.FindSecondByFirst(first);
                if(seconds.Count>0)
                {
                    var pts = new List<Point3d>();
                    seconds.ForEach(o=>
                    {
                        pts.Add(o.StartPoint);
                        pts.Add(o.EndPoint);
                    });
                    var result = ThGeometryTool.GetCollinearMaxPts(pts);
                    var secondLightEdge = new ThLightEdge
                    {
                        Edge = new Line(result.Item1, result.Item2),
                        Id = Guid.NewGuid().ToString(),
                        Direction = m.Direction
                    };
                    m.LightNodes.ForEach(n =>
                    {
                        if (!string.IsNullOrEmpty(n.Number))
                        {
                            int firstLightIndex = n.GetIndex();
                            if (firstLightIndex != -1)
                            {                               
                                var position = n.Position.GetProjectPtOnLine(result.Item1, result.Item2);
                                int secondLightIndex = firstLightIndex + 1;
                                var number = secondLightIndex.ToString().PadLeft(loopCharLength, '0');
                                var secondLightNode = new ThLightNode
                                {
                                    Number = ThGarageLightCommon.LightNumberPrefix + number,
                                    Position = position
                                };
                                if (ThGarageLightUtils.IsPointOnLines(position, new Line(result.Item1, result.Item2)))
                                {
                                    secondLightEdge.LightNodes.Add(secondLightNode);
                                }
                            }
                        }
                    });
                    SecondLightEdges.Add(secondLightEdge);
                }
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
            else
            {
                int loopNumber = ArrangeParameter.LoopNumber;
                if(loopNumber<4)
                {
                    loopNumber = 4;
                }
                else
                {
                    if(loopNumber% 2 !=0)
                    {
                        loopNumber = loopNumber+1;
                    }
                    ArrangeParameter.LoopNumber= loopNumber;
                }
            }
        }
        private int CalculateLoopNumber(int lightNumbers)
        {
            var value = lightNumbers * 1.0 / 25;
            double number = Math.Ceiling(value) * 2;
            if (number < 4)
            {
                number = 4;
            }
            return (int)number / 2;
        }
        private List<Point3d> GetFirstLinePorts(
             List<Line> firstLines)
        {
            var spatialIndex = ThGarageLightUtils.BuildSpatialIndex(firstLines);
            var firstPorts = new List<Point3d>();
            firstLines.ForEach(o =>
            {
                var spOutline = ThDrawTool.CreateSquare(o.StartPoint, 2.0);
                var epOutline = ThDrawTool.CreateSquare(o.EndPoint, 2.0);
                var spObjs = spatialIndex.SelectCrossingPolygon(spOutline);
                spObjs.Remove(o);
                var epObjs = spatialIndex.SelectCrossingPolygon(epOutline);
                epObjs.Remove(o);
                if(spObjs.Count==0)
                {
                    firstPorts.Add(o.StartPoint);
                }
                if (epObjs.Count ==0)
                {
                    firstPorts.Add(o.EndPoint);
                }
            });
            return firstPorts;
        }
    }
}
