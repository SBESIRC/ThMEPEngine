using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using ThMEPLighting.Garage.Service.Arrange;

namespace ThMEPLighting.Garage.Engine
{
    public class ThDoubleRowArrangementEngine : ThArrangementEngine
    {
        public ThDoubleRowArrangementEngine(ThLightArrangeParameter arrangeParameter)
            : base(arrangeParameter)
        {
        }

        public override void Arrange(ThRegionBorder regionBorder)
        {
            // 预处理，将车道中心线处理成多根直线
            if (IsPreProcess)
            {
                Preprocess(regionBorder);
            }
            // 创建双排灯线            
            var lightingLineService = new ThDoubleRowLightingLineService(ArrangeParameter.DoubleRowOffsetDis);
            lightingLineService.LightingLineBuffer(regionBorder);

            //regionBorder.PrintDxLines();
            //regionBorder.PrintFirstLines();
            //regionBorder.PrintSecondLines();
            //regionBorder.PrintExtendLines();

            // 布点
            var arrange = new ThLightingArrangeService(regionBorder, ArrangeParameter);
            arrange.Arrange();
            Graphs = arrange.Graphs;
            LoopNumber = arrange.LoopNumber;

            // 对单排线槽中心线布灯、编号
            var singleGraphs = SingleRowCableTrunkingLayout(regionBorder.RegionBorder, regionBorder.SingleRowLines, LoopNumber);
            Graphs.AddRange(singleGraphs);
        }

        protected override void Preprocess(ThRegionBorder regionBorder)
        {
            regionBorder.Trim(); // 裁剪
            regionBorder.TrimOffsetLines(ArrangeParameter.DoubleRowOffsetDis / 2.0);
            regionBorder.Shorten(ThGarageLightCommon.RegionBorderBufferDistance); // 缩短
            regionBorder.Noding(); //
            regionBorder.HandleSharpAngle(); //
            Filter(regionBorder); // 过滤
            regionBorder.Merge(); // 清理
            regionBorder.Normalize(); //单位化
            regionBorder.Sort(); // 排序
        }

        private void Filter(ThRegionBorder regionBorder)
        {
            // 对于较短的灯线且一段未连接任何线，另一端连接在线上
            var limitLength = ArrangeParameter.LampLength + ArrangeParameter.Margin * 2;
            var filter1 = new ThFilterShortLinesService(regionBorder.DxCenterLines, limitLength);
            regionBorder.DxCenterLines = filter1.FilterIsolatedLine();
        }

        private List<ThLightGraphService> SingleRowCableTrunkingLayout(Entity border, List<Line> singleRowLines, int loopNumber)
        {
            if (ArrangeParameter.InstallWay != InstallWay.CableTray || singleRowLines.Count == 0)
            {
                return new List<ThLightGraphService>();
            }
            var singleRowRegionBorder = new ThRegionBorder()
            {
                RegionBorder = border,
                DxCenterLines = singleRowLines,
            };
            var lightGraphs = new List<ThLightGraphService>();
            var arrangeEngine = new ThSingleRowArrangementEngine(ArrangeParameter)
            {
                IsPreProcess = false,
            };
            arrangeEngine.SetDefaultStartNumber(loopNumber * 2 + 1);
            arrangeEngine.Arrange(singleRowRegionBorder);
            lightGraphs = arrangeEngine.Graphs;
            return lightGraphs;
        }
    }
}
