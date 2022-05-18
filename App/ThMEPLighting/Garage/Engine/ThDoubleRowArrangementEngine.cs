using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Service.Number;
using ThMEPLighting.Garage.Service.LayoutPoint;

namespace ThMEPLighting.Garage.Engine
{
    public class ThDoubleRowArrangementEngine : ThArrangementEngine
    {
        private List<Curve> mergeDxLines;
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; private set; }
        public List<Tuple<Point3d,Dictionary<Line,Vector3d>>>  CenterGroupLines { get; private set; }

        public ThDoubleRowArrangementEngine(ThLightArrangeParameter arrangeParameter)
            :base(arrangeParameter)
        {
            mergeDxLines = new List<Curve>();
            CenterSideDicts = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
            CenterGroupLines = new List<Tuple<Point3d, Dictionary<Line,Vector3d>>>();
        }
        protected override void Filter(ThRegionBorder regionBorder)
        {
            base.Filter(regionBorder);
            regionBorder.DxCenterLines = ThFilterMainCenterLineService.Filter(regionBorder.DxCenterLines, ArrangeParameter.DoubleRowOffsetDis / 2.0);
            regionBorder.DxCenterLines = ThFilterElbowCenterLineService.Filter(regionBorder.DxCenterLines, ArrangeParameter.MinimumEdgeLength);
            regionBorder.DxCenterLines = ThFilterIsolatedLineService.Filter(regionBorder.DxCenterLines, ArrangeParameter.MinimumEdgeLength);
        }
        protected override void Preprocess(ThRegionBorder regionBorder)
        {
            regionBorder.Trim(); // 裁剪
            regionBorder.TrimOffsetLines(ArrangeParameter.DoubleRowOffsetDis / 2.0);
            regionBorder.Shorten(ThGarageLightCommon.RegionBorderBufferDistance); // 缩短
            regionBorder.Noding(); // 
            Filter(regionBorder); // 过滤
            regionBorder.Clean(); // 清理
            regionBorder.Normalize(); //单位化
            regionBorder.Sort(); // 排序
            // 合并车道线,可考虑合并掉间距小于 3/4倍输入线槽间距 的线槽
            mergeDxLines = regionBorder.Merge(0.75 * ArrangeParameter.DoubleRowOffsetDis); // 合并            
        }

        public override void Arrange(ThRegionBorder regionBorder)
        {
            // 预处理
            Preprocess(regionBorder);

            if(true)
            {
                NewArrange(regionBorder);
            }
            else
            {
                OldArrange(regionBorder);
            }
        }

        private void OldArrange(ThRegionBorder regionBorder)
        {
            // 识别内外圈
            // 产品对1、2号线的提出了新需求（1号线延伸到1号线，2号线延伸到2号线 -> ToDO
            var innerOuterCircles = new List<ThWireOffsetData>();
            using (var innerOuterEngine = new ThInnerOuterCirclesEngine())
            {
                //需求变化2020.12.23,非灯线不参与编号传递
                //创建1、2号线，车道线merge，配对1、2号线
                innerOuterCircles = innerOuterEngine.Reconize(
                    mergeDxLines, ArrangeParameter.DoubleRowOffsetDis / 2.0);
            }

            // 把创建1、2号线的结果返回
            innerOuterCircles.ForEach(o =>
            {
                CenterSideDicts.Add(o.Center, Tuple.Create(new List<Line> { o.First }, new List<Line> { o.Second }));
            });

            // 布点 + 返回1、2号边（包括布灯的点）
            var firstLines = innerOuterCircles.Select(o => o.First).ToList().Preprocess();
            var secondLines = innerOuterCircles.Select(o => o.Second).ToList().Preprocess();
            var edgeResult = CreateDistributePointEdges(regionBorder, firstLines,secondLines);
            var firstLightEdges = edgeResult.Item1;
            var secondLightEdges = edgeResult.Item2;

            // 建图
            firstLightEdges.Select(o => o.Edge).Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 1);

            Graphs = CreateGraphs(firstLightEdges);

            // 编号
            var firstSecondPairService = new ThFirstSecondPairService(
                firstLightEdges.Select(o => o.Edge).ToList(),
                 secondLightEdges.Select(o => o.Edge).ToList(),
                 ArrangeParameter.DoubleRowOffsetDis);

            var secondGraphs = new List<ThLightGraphService>();
            Graphs.ForEach(g =>
            {
                int loopNumber = GetLoopNumber(g.CalculateLightNumber());
                g.Number(loopNumber, false, ArrangeParameter.DefaultStartNumber);

                var subSecondEdges = PassFirstNumberToSecond(g.GraphEdges, 
                    secondLightEdges, firstSecondPairService,loopNumber);
                
                var secondStart = firstSecondPairService.FindSecondStart(g.StartPoint, ArrangeParameter.DoubleRowOffsetDis);
                if (secondStart.HasValue)
                {
                    var res = CreateGraphs(subSecondEdges, new List<Point3d> { secondStart.Value });
                    secondGraphs.AddRange(res);
                }
                else
                {
                    var res = CreateGraphs(subSecondEdges);
                    secondGraphs.AddRange(res);
                }
            });

            // 对2号线未编号的灯，查找其邻近的灯                
            ReNumberSecondEdges(secondGraphs.SelectMany(g => g.GraphEdges).ToList());

            //Graphs.ForEach(g => Service.Print.ThPrintService.Print(g));
            //secondGraphs.ForEach(g => Service.Print.ThPrintService.Print(g));
            Graphs.AddRange(secondGraphs);
        }
        
        public void NewArrange(ThRegionBorder regionBorder)
        {
            // 按连通性对中心线分组
            var groupLines = FindConnectedLine(regionBorder.DxCenterLines);
            CenterGroupLines = groupLines; // 返回用于创建十字型线槽

            // 分组+创建边
            var firstSeondEdges = new List<Tuple<List<ThLightEdge>, List<ThLightEdge>>>();
            groupLines.ForEach(g =>
            {
                // 创建1号线
                var firstSecondEngine = new ThFirstSecondRecognitionEngine();
                var startPt = g.Item1;
                var centerLines = g.Item2.Keys.ToList();
                firstSecondEngine.Recognize(g.Item1, centerLines, ArrangeParameter.DoubleRowOffsetDis);
                firstSecondEngine.CenterSideDict.ForEach(o => CenterSideDicts.Add(o.Key, o.Value));
                
                // 对获得1、2号进行Noding
                var firstLines = firstSecondEngine.FirstLines.Preprocess();
                var secondLines = firstSecondEngine.SecondLines.Preprocess();
                //firstLines.OfType<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 1);
                //secondLines.OfType<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 6);

                // 通过1、2号线布点，返回带点的边
                var edges = CreateDistributePointEdges(regionBorder, firstLines, secondLines);
                firstSeondEdges.Add(edges);
            });

            // 计算回路数量
            int lightNumber = firstSeondEdges.Sum(o => o.Item1.SelectMany(a1 => a1.LightNodes).Count() + 
            o.Item2.SelectMany(a2 => a2.LightNodes).Count());
            base.LoopNumber = GetLoopNumber(lightNumber);

            firstSeondEdges.ForEach(o =>
            {
                // 建图
                var firstGraphs = CreateGraphs(o.Item1);

                // 建立1、2的配对查询
                var firstSecondPairService = new ThFirstSecondPairService(o.Item1.Select(e => e.Edge).ToList(),
                        o.Item2.Select(e => e.Edge).ToList(), ArrangeParameter.DoubleRowOffsetDis);

                // 为1号线编号,传递到2号线
                firstGraphs.ForEach(f =>
                {
                    f.Number(base.LoopNumber, false, base.DefaultStartNumber);
                    PassFirstNumberToSecond(f.GraphEdges, o.Item2, firstSecondPairService, base.LoopNumber);
                });

                // 对于2号线未编号的,再编号
                ReNumberSecondEdges(o.Item2);

                // 对2号线建图
                var secondGraphs = CreateGraphs(o.Item2);
                Graphs.AddRange(firstGraphs);
                Graphs.AddRange(secondGraphs);
            });
        }

        private List<Tuple<Point3d,Dictionary<Line,Vector3d>>> FindConnectedLine(List<Line> lines)
        {
            var results = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
            var centerEdges = lines.Select(o => new ThLightEdge(o)).ToList();
            var graphs = CreateGraphs(centerEdges);
            graphs.ForEach(g =>
            {
                var dict = new Dictionary<Line, Vector3d>();
                g.GraphEdges.ForEach(o => dict.Add(o.Edge, o.Direction));
                results.Add(Tuple.Create(g.StartPoint, dict));
            });
            return results;
        }
      
        private List<Line> Union(List<Line> firstLines,List<Line> secondLines)
        {
            var results = new List<Line>();
            results.AddRange(firstLines);
            results.AddRange(secondLines);
            return results;
        }

        private List<Point3d> LayoutPoints(
            List<Line> firstLines,
            List<Line> secondLines,
            DBObjectCollection beams,
            DBObjectCollection columns)
        {
            // Curve 仅支持Line，和Line组成的多段线
            var results = new List<Point3d>();
            ThLayoutPointService layoutPointService = null;
            switch (ArrangeParameter.LayoutMode)
            {
                case LayoutMode.AvoidBeam:
                    layoutPointService = new ThAvoidBeamLayoutPointService(beams);
                    break;
                case LayoutMode.ColumnSpan:
                    layoutPointService = new ThColumnSpanLayoutPointService(columns,
                        ArrangeParameter.NearByDistance);
                    break;
                case LayoutMode.SpanBeam:
                    layoutPointService = new ThSpanBeamLayoutPointService(beams);
                    break;
                default:
                    layoutPointService = new ThEqualDistanceLayoutPointService();
                    break;
            }
            if (layoutPointService != null)
            {
                layoutPointService.Margin = ArrangeParameter.Margin;
                layoutPointService.Interval = ArrangeParameter.Interval;
                layoutPointService.LampLength = ArrangeParameter.LampLength;
                layoutPointService.DoubleRowOffsetDis = ArrangeParameter.DoubleRowOffsetDis;
                results = layoutPointService.Layout(firstLines, secondLines);
            }
            return results;
        }

        private List<ThLightEdge> BuildEdges(List<Line> lines,EdgePattern edgePattern)
        {
            var edges = new List<ThLightEdge>();           
            lines.ForEach(o => edges.Add(new ThLightEdge(o) { EdgePattern = edgePattern }));
            return edges;
        }

        private int GetLoopNumber(int lightNumber)
        {
            if (ArrangeParameter.AutoCalculate)
            {
                return CalculateLoopNumber(lightNumber, ArrangeParameter.LightNumberOfLoop);
            }
            else
            {
                return GetUILoopNumber(ArrangeParameter.LoopNumber);
            }
        }

        /// <summary>
        /// 自动计算灯回路编号
        /// </summary>
        /// <param name="lightGraph"></param>
        private int GetUILoopNumber(int uiLoopNumber)
        {
            int result = 0;
            if (uiLoopNumber < 4)
            {
                result = 4;
            }
            else
            {
                result = uiLoopNumber;
            }
            if(result%2==1)
            {
                result += 1;
            }
            return result / 2; // 计算单回路数量
        }
        
        /// <summary>
        /// 根据灯的数量和每一个回路包含灯的数量，计算灯回路
        /// eg. 灯的数量为100,每个回路25盏灯，计算得出4个回路
        /// </summary>
        /// <param name="lightNumbers">灯的数量</param>
        /// <param name="lightNumberOfLoop">每一个回路包含多少盏灯</param>
        /// <returns></returns>
        private int CalculateLoopNumber(int lightNumbers,int lightNumberOfLoop)
        {
            var value = lightNumbers * 1.0 / lightNumberOfLoop;
            double number = Math.Ceiling(value);
            int intNumber = (int)number;
            if (intNumber < 4)
            {
                intNumber = 4;
            }
            if (intNumber % 2 == 1)
            {
                intNumber += 1;
            }
            return intNumber / 2; // 计算单回路数量
        }

        private List<ThLightEdge> PassFirstNumberToSecond(
            List<ThLightEdge> firstEdges, 
            List<ThLightEdge> secondEdges,
            ThFirstSecondPairService firstSecondPairService,int loopNumber)
        {
            var results = new List<ThLightEdge>();
            var firstLines = firstEdges.Select(o => o.Edge).ToList();
            var firstSecondDict = firstSecondPairService.FindSecondLines(firstLines);
            firstSecondDict.ForEach(o =>
            {
                var firstEdge = firstEdges[firstLines.IndexOf(o.Key)];
                var secondPairEdges = secondEdges.Where(e => o.Value.Contains(e.Edge)).ToList();
                results.AddRange(secondPairEdges);
                // 把1号线编号传递到2号线
                PassFirstNumberToSecond(firstEdge, secondPairEdges, loopNumber);
            });
            return results;
        }
        /// <summary>
        /// 把1号布灯的编号传递到2号边
        /// </summary>
        /// <param name="firstEdge">已编号的1号边</param>
        /// <param name="secondEdge">对应的二号边</param>
        /// <param name="loopNumber">回路编号</param>
        private void PassFirstNumberToSecond(ThLightEdge firstEdge,List<ThLightEdge> secondEdges,int loopNumber)
        {
            int loopCharLength = loopNumber.GetLoopCharLength();
            firstEdge.LightNodes.ForEach(m =>
            {
                if (!string.IsNullOrEmpty(m.Number) && m.GetIndex() != -1)
                {
                    foreach (var secondEdge in secondEdges)
                    {
                        secondEdge.Direction = firstEdge.Direction; // 传递编号时，把1号边的方向传给2号边
                        var position = m.Position.GetProjectPtOnLine(
                            secondEdge.Edge.StartPoint, secondEdge.Edge.EndPoint);
                        if (position.IsPointOnCurve(secondEdge.Edge, 5.0)) // 5.0 
                        {
                            var findSecondNodes = secondEdge.LightNodes
                            .Where(k => k.Position.DistanceTo(position) <= 1.0)
                            .OrderBy(k => k.Position.DistanceTo(position));

                            if (findSecondNodes.Count() > 0)
                            {
                                int secondLightIndex = m.GetIndex() + 1;    
                                var secondNode = findSecondNodes.First();
                                secondNode.Number = ThNumberService.FormatNumber(secondLightIndex,loopCharLength);
                            }
                            break;
                        }
                    }
                }
            });
        }

        private void ReNumberSecondEdges(List<ThLightEdge> secondEdges)
        {
            var secondNumberService = new ThSecondNumberService(
                secondEdges, this.LoopNumber, this.DefaultStartNumber + 1);
            secondNumberService.Number();
        }
        private List<ThLightGraphService> CreateGraphs(List<ThLightEdge> lightEdges, List<Point3d> referencePoints)
        {
            var results = new List<ThLightGraphService>();
            while(lightEdges.Count>0)
            {
                var startPt = lightEdges[0].Edge.StartPoint;
                if (referencePoints.Count>0)
                {
                    startPt = referencePoints[0];
                    referencePoints.RemoveAt(0);
                }
                var lightGraph = new ThCdzmLightGraphService(lightEdges, startPt);
                lightGraph.Build();
                results.Add(lightGraph);
                lightEdges = lightEdges.Where(o=> !lightGraph.GraphEdges.Select(e=>e.Id).Contains(o.Id)).ToList();
            }
            return results;
        }
        
        private Tuple<List<ThLightEdge>, List<ThLightEdge>> CreateDistributePointEdges(
            ThRegionBorder regionBorder,List<Line> firstLines,List<Line> secondLines)
        {
            
            var firstLightEdges = BuildEdges(firstLines,EdgePattern.First);
            var secondLightEdges = BuildEdges(secondLines,EdgePattern.Second);

            // 布点
            var linePoints = new Dictionary<Line, List<Point3d>>();
            if (ArrangeParameter.AutoGenerate)
            {
                var beams = ArrangeParameter.LayoutMode==LayoutMode.AvoidBeam || 
                    ArrangeParameter.LayoutMode == LayoutMode.SpanBeam ?
                    regionBorder.Beams.Select(b => b.Outline).ToCollection():new DBObjectCollection();
                var columns = ArrangeParameter.LayoutMode == LayoutMode.ColumnSpan ? 
                    regionBorder.Columns.Select(c => c.Outline).ToCollection() : new DBObjectCollection();
                var points = LayoutPoints(firstLines, secondLines, beams, columns);
                linePoints = ThQueryPointService.Query(points, Union(firstLines, secondLines));
            }
            else
            {
                linePoints = ThQueryPointService.Query(regionBorder.Lights, Union(firstLines, secondLines));
            }

            // Sort
            linePoints = Sort(linePoints);

            // 优化布置的点
            var optimizer = new ThLayoutPointOptimizeService(linePoints, FilterPointDistance);
            optimizer.Optimize();

            firstLightEdges.ForEach(f =>
            {
                linePoints[f.Edge].ForEach(p =>
                {
                    f.LightNodes.Add(new ThLightNode() { Position = p });
                });
            });
            secondLightEdges.ForEach(f =>
            {
                linePoints[f.Edge].ForEach(p =>
                {
                    f.LightNodes.Add(new ThLightNode() { Position = p });
                });
            });
            return Tuple.Create(firstLightEdges, secondLightEdges);
        }

        private Dictionary<Line, List<Point3d>> Sort(Dictionary<Line, List<Point3d>> linePoints)
        {
            var result = new Dictionary<Line, List<Point3d>>();
            linePoints.ForEach(o =>
            {
                var pts = o.Value.OrderBy(p => p.DistanceTo(o.Key.StartPoint)).ToList();
                result.Add(o.Key,pts);
            });
            return result;
        }
    }
}
