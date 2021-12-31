using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using ThMEPLighting.Garage.Service.Number;


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
        }
        protected override void Preprocess(ThRegionBorder regionBorder)
        {
            regionBorder.Trim(); // 裁剪
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

            //switch(ArrangeParameter.ArrangeEdition)
            //{
            //    case ArrangeEdition.First:
            //        FirstEdtionArrange(regionBorder);
            //        break;
            //        case ArrangeEdition.Second:
            //        SecondEdtionArrange(regionBorder);
            //        break;
            //    case ArrangeEdition.Third:
            //        ThirdEdtionArrange(regionBorder);
            //        break;
            //    default:
            //        return;
            //}
        }

        //private void FirstEdtionArrange(ThRegionBorder regionBorder)
        //{
        //    // 识别内外圈
        //    // 产品对1、2号线的提出了新需求（1号线延伸到1号线，2号线延伸到2号线 -> ToDO
        //    var innerOuterCircles = new List<ThWireOffsetData>();
        //    using (var innerOuterEngine = new ThInnerOuterCirclesEngine())
        //    {
        //        //需求变化2020.12.23,非灯线不参与编号传递
        //        //创建1、2号线，车道线merge，配对1、2号线
        //        innerOuterCircles = innerOuterEngine.Reconize(
        //            mergeDxLines, ArrangeParameter.DoubleRowOffsetDis / 2.0);
        //    }

        //    // 把创建1、2号线的结果返回
        //    innerOuterCircles.ForEach(o =>
        //    {
        //        CenterSideDicts.Add(o.Center, Tuple.Create(new List<Line> { o.First }, new List<Line> { o.Second }));
        //    });

        //    // 布点 + 返回1、2号边（包括布灯的点）
        //    var firstLines = innerOuterCircles.Select(o => o.First).ToList().Preprocess();
        //    var secondLines = innerOuterCircles.Select(o => o.Second).ToList().Preprocess();
        //    var edgeResult = CreateDistributePointEdges(regionBorder, firstLines,secondLines);
        //    var firstLightEdges = edgeResult.Item1;
        //    var secondLightEdges = edgeResult.Item2;

        //    // 建图
        //    firstLightEdges.Select(o => o.Edge).Cast<Entity>().ToList().CreateGroup(AcHelper.Active.Database, 1);

        //    Graphs = CreateGraphs(firstLightEdges);

        //    // 编号
        //    var firstSecondPairService = new ThFirstSecondPairService(
        //        firstLightEdges.Select(o => o.Edge).ToList(),
        //         secondLightEdges.Select(o => o.Edge).ToList(),
        //         ArrangeParameter.DoubleRowOffsetDis);

        //    var secondGraphs = new List<ThLightGraphService>();
        //    Graphs.ForEach(g =>
        //    {
        //        int loopNumber = GetLoopNumber(g.CalculateLightNumber());
        //        g.Number(loopNumber, false, ArrangeParameter.DefaultStartNumber);

        //        var subSecondEdges = PassFirstNumberToSecond(g.GraphEdges, 
        //            secondLightEdges, firstSecondPairService,loopNumber);
                
        //        var secondStart = firstSecondPairService.FindSecondStart(g.StartPoint, ArrangeParameter.DoubleRowOffsetDis);
        //        if (secondStart.HasValue)
        //        {
        //            var res = CreateGraphs(subSecondEdges, new List<Point3d> { secondStart.Value });
        //            secondGraphs.AddRange(res);
        //        }
        //        else
        //        {
        //            var res = CreateGraphs(subSecondEdges);
        //            secondGraphs.AddRange(res);
        //        }
        //    });

        //    // 对2号线未编号的灯，查找其邻近的灯                
        //    ReNumberSecondEdges(secondGraphs.SelectMany(g => g.GraphEdges).ToList());

        //    //Graphs.ForEach(g => Service.Print.ThPrintService.Print(g));
        //    //secondGraphs.ForEach(g => Service.Print.ThPrintService.Print(g));
        //    Graphs.AddRange(secondGraphs);
        //}
    }
}
