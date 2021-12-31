using System;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Factory;
using ThMEPLighting.Garage.Service.Number;

namespace ThMEPLighting.Garage.Service.Arrange
{
    public class ThSecondwayArrangeService : ThArrangeService
    {
        public ThSecondwayArrangeService(
            ThRegionBorder regionBorder, 
            ThLightArrangeParameter arrangeParameter)
            :base(regionBorder, arrangeParameter)
        {
        }

        public override void Arrange()
        {
            // 对中心线建图，把连通的线分组
            CenterGroupLines = FindConnectedLine(RegionBorder.DxCenterLines); // 返回用于创建十字型线槽

            // 创建边(边上有布灯的点)
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

                // 建立1、2的配对查询
                var firstSecondPairService = new ThFirstSecondPairService(o.Item1.Select(e => e.Edge).ToList(),
                        o.Item2.Select(e => e.Edge).ToList(), ArrangeParameter.DoubleRowOffsetDis);

                // 为1号线编号,传递到2号线
                firstGraphs.ForEach(f => f.Number(base.LoopNumber, false, ArrangeParameter.DefaultStartNumber));

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
            // 分组+创建边
            var firstSeondEdges = new List<Tuple<List<ThLightEdge>, List<ThLightEdge>>>();
            CenterGroupLines.ForEach(g =>
            {
                // 创建1号线
                var firstSecondEngine = new ThFirstSecondAFactory(
                    g.Item2.Keys.ToList(),
                    ArrangeParameter.DoubleRowOffsetDis,
                    g.Item1);
                firstSecondEngine.Produce();
                firstSecondEngine.CenterSideDict.ForEach(o => CenterSideDicts.Add(o.Key, o.Value));

                // 对获得1、2号进行Noding
                var firstLines = firstSecondEngine.FirstLines.Preprocess();
                var secondLines = firstSecondEngine.SecondLines.Preprocess();
         
                // 通过1、2号线布点，返回带点的边
                var edges = CreateDistributePointEdges(RegionBorder, firstLines, secondLines);
                firstSeondEdges.Add(edges);
            });
            return firstSeondEdges;
        }

        private ThFirstSecondFactory BuildFirstSecondLines(List<Line> lines,Point3d startPt,double doubleRowOffsetDis)
        {
            // 创建1号线
            var factory = new ThFirstSecondAFactory(
                lines, doubleRowOffsetDis, startPt);
            factory.Produce();
            return factory;
        }
    }
}
