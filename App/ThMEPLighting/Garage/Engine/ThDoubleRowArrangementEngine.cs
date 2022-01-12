using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using ThMEPLighting.Garage.Service.Arrange;

namespace ThMEPLighting.Garage.Engine
{
    public class ThDoubleRowArrangementEngine : ThArrangementEngine
    {
        public Dictionary<Line, Tuple<List<Line>, List<Line>>> CenterSideDicts { get; private set; }
        public List<Tuple<Point3d, Dictionary<Line, Vector3d>>> CenterGroupLines { get; private set; }

        public ThDoubleRowArrangementEngine(ThLightArrangeParameter arrangeParameter)
            : base(arrangeParameter)
        {
            CenterSideDicts = new Dictionary<Line, Tuple<List<Line>, List<Line>>>();
            CenterGroupLines = new List<Tuple<Point3d, Dictionary<Line, Vector3d>>>();
        }
        public override void Arrange(ThRegionBorder regionBorder)
        {
            // 预处理
            Preprocess(regionBorder);
            var arrange = new ThThirdwayArrangeService(regionBorder, ArrangeParameter);
            arrange.Arrange();
            Graphs = arrange.Graphs;
            LoopNumber = arrange.LoopNumber;
            CenterSideDicts = arrange.CenterSideDicts;
            CenterGroupLines = arrange.CenterGroupLines;
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
        }
        private void Filter(ThRegionBorder regionBorder)
        {
            // 对于较短的灯线且一段未连接任何线，另一端连接在线上
            double filterLength = ArrangeParameter.LampLength + ArrangeParameter.DoubleRowOffsetDis / 2.0;
            regionBorder.DxCenterLines = ThFilterTTypeCenterLineService.Filter(
                regionBorder.DxCenterLines, filterLength);
            regionBorder.DxCenterLines = ThFilterMainCenterLineService.Filter(
                regionBorder.DxCenterLines, filterLength);
            regionBorder.DxCenterLines = ThFilterElbowCenterLineService.Filter(
                regionBorder.DxCenterLines, filterLength);
        }
    }
}
