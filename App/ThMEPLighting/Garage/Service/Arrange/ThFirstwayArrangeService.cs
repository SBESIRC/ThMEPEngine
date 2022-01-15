using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Engine;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service.Number;

namespace ThMEPLighting.Garage.Service.Arrange
{
    public class ThFirstwayArrangeService : ThArrangeService
    {
        public ThFirstwayArrangeService(
            ThRegionBorder regionBorder, 
            ThLightArrangeParameter arrangeParameter)
            :base(regionBorder, arrangeParameter)
        {
        }
        public override void Arrange()
        {
            // 合并车道线,可考虑合并掉间距小于 3/4倍输入线槽间距 的线槽
            var mergeDxLines = RegionBorder.Merge(0.75 * ArrangeParameter.DoubleRowOffsetDis); // 合并    

            // 识别内外圈
            // 产品对1、2号线的提出了新需求（1号线延伸到1号线，2号线延伸到2号线 -> ToDO
            var innerOuterCircles = new List<ThWireOffsetData>();
            using (var innerOuterEngine = new ThInnerOuterCirclesEngine())
            {
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
            var firstLightEdges = BuildEdges(firstLines, Common.EdgePattern.First);
            var secondLightEdges = BuildEdges(secondLines, Common.EdgePattern.Second);
            CreateDistributePointEdges(firstLightEdges, secondLightEdges);
         
            // 计算回路
            int lightNumber = firstLightEdges.CalculateLightNumber() +
            secondLightEdges.CalculateLightNumber();
            base.LoopNumber = ArrangeParameter.GetLoopNumber(lightNumber);

            // 建图
            var firstGraphs = firstLightEdges.CreateGraphs();

            // 编号
            var firstSecondPairService = new ThFirstSecondPairService(
                firstLightEdges.Select(o => o.Edge).ToList(),
                 secondLightEdges.Select(o => o.Edge).ToList(),
                 ArrangeParameter.DoubleRowOffsetDis);

            // 为1号线编号
            firstGraphs.ForEach(g => g.Number(LoopNumber, false, ArrangeParameter.DefaultStartNumber));

            // 把1号线编号传递到2号线
            PassNumber(firstGraphs.SelectMany(o => o.GraphEdges).ToList(), secondLightEdges);

            // 对于2号线未编号的,再编号
            ReNumberSecondEdges(secondLightEdges);

            // 对2号线建图
            var secondGraphs = secondLightEdges.CreateGraphs();
            Graphs.AddRange(firstGraphs);
            Graphs.AddRange(secondGraphs);
        }
    }
}
