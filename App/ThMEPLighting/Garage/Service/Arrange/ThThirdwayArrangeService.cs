using System;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Factory;
using ThMEPLighting.Garage.Service.Number;
using ThMEPLighting.Garage.Service.LayoutResult;

namespace ThMEPLighting.Garage.Service.Arrange
{
    public class ThThirdwayArrangeService : ThArrangeService
    {
        public ThThirdwayArrangeService(
            ThRegionBorder regionBorder, 
            ThLightArrangeParameter arrangeParameter)
            :base(regionBorder, arrangeParameter)
        {
        }
        public override void Arrange()
        {
            // 对中心线建图，把连通的线分组
            CenterGroupLines = FindConnectedLine(RegionBorder.DxCenterLines);

            // 创建边(边上有布灯的点，且方向是跟随中心线方向的)
            var firstSeondEdges = CreateEdges();

            // 计算回路数量
            // 计算此regionBorder中的回路数（此值要返回）
            int lightNumber = firstSeondEdges.Sum(o => o.Item1.CalculateLightNumber() +
            o.Item2.CalculateLightNumber());
            base.LoopNumber = ArrangeParameter.GetLoopNumber(lightNumber);

            // 编号
            firstSeondEdges.ForEach(o =>
            {
                // 建图
                var firstGraphs = o.Item1.CreateGraphs();

                // 为1号线编号
                firstGraphs.ForEach(f => f.Number1(base.LoopNumber, false, ArrangeParameter.DefaultStartNumber));

                // 把1号线编号,传递到2号线
                PassNumber(firstGraphs.SelectMany(g => g.GraphEdges).ToList(), o.Item2);

                // 对于2号线未编号的,再编号
                ReNumberSecondEdges(o.Item2);

                // 对2号线建图
                var secondGraphs = o.Item2.CreateGraphs();
                Graphs.AddRange(firstGraphs);
                Graphs.AddRange(secondGraphs);
            });
        }

        private List<Tuple<List<ThLightEdge>, List<ThLightEdge>>> CreateEdges()
        {
            var firstSeondEdges = new List<Tuple<List<ThLightEdge>, List<ThLightEdge>>>();
            CenterGroupLines.ForEach(g =>
            {
                var factory = BuildFirstSecondLines(g.Item2.Keys.ToList(), ArrangeParameter.DoubleRowOffsetDis);
                // 把中心线对应的两边要记录下来，供后续交叉连接调用
                factory.CenterSideDict.ForEach(o => CenterSideDicts.Add(o.Key, o.Value));
                var firstLightEdges = BuildEdges(factory.FirstLines, EdgePattern.First);
                var secondLightEdges  = BuildEdges(factory.SecondLines, EdgePattern.Second);

                // 连接T型、十字型
                if(ArrangeParameter.InstallWay != InstallWay.CableTray)
                {
                    var newAddEdges = AddLinkCrossEdges(firstLightEdges.Union(secondLightEdges).ToList(), factory.CenterSideDict);
                    firstLightEdges.AddRange(newAddEdges.Where(o => o.EdgePattern == EdgePattern.First).ToList());
                    secondLightEdges.AddRange(newAddEdges.Where(o => o.EdgePattern == EdgePattern.Second).ToList());
                }
                
                // 通过1、2号线布点，返回带点的边
                CreateDistributePointEdges(firstLightEdges, secondLightEdges);
                firstSeondEdges.Add(Tuple.Create(firstLightEdges, secondLightEdges));
            });

             // 连边
            return firstSeondEdges;
        }

        private List<ThLightEdge> AddLinkCrossEdges(List<ThLightEdge> edges, 
            Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDicts)
        {
            // 将十字处、T字处具有相同EdgePattern的边直接连接
            var results = new List<ThLightEdge>();
            var calculator = new ThCrossLinkCalculator(edges, centerSideDicts);
            calculator.BuildCrossLinkEdges().ForEach(o =>
            {
                if (!results.Select(e => e.Edge).ToList().GeometryContains(o.Edge, ThMEPEngineCoreCommon.GEOMETRY_TOLERANCE))
                {
                    results.Add(o);
                }
            });
            calculator.BuildThreeWayLinkEdges().ForEach(o =>
            {
                if (!results.Select(e => e.Edge).ToList().GeometryContains(o.Edge, ThMEPEngineCoreCommon.GEOMETRY_TOLERANCE))
                {
                    results.Add(o);
                }
            });
            return results;
        }

        private ThFirstSecondFactory BuildFirstSecondLines(List<Line> lines,double doubleRowOffsetDis)
        {
            // 创建1号线
            var factory = new ThFirstSecondBFactory(
                lines,doubleRowOffsetDis);
            factory.Produce();
            return factory;
        }
        private Vector3d? FindDirection(Line center, Dictionary<Line, Vector3d> centerLineDirDict)
        {
            // 将中心线遍历的方向传递给边线
            if (centerLineDirDict.ContainsKey(center))
            {
                return centerLineDirDict[center];
            }
            return null;
        }
        private Line FindCenter(Line side, Dictionary<Line, Tuple<List<Line>, List<Line>>> centerSideDict)
        {
            var res = centerSideDict
               .Where(o => o.Value.Item1.Contains(side) || o.Value.Item2.Contains(side));
            if (res.Count() == 1)
            {
                return res.First().Key;
            }
            else
            {
                return new Line();
            }
        }
    }
}
