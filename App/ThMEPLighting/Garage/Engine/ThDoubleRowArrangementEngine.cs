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
        }

        public override void Arrange(ThRegionBorder regionBorder)
        {
            // 预处理
            Preprocess(regionBorder);

            ThArrangeService arrange;
            switch (ArrangeParameter.ArrangeEdition)
            {
                case ArrangeEdition.First:
                    arrange = new ThFirstwayArrangeService(regionBorder, ArrangeParameter);
                    break;
                case ArrangeEdition.Second:
                    arrange = new ThSecondwayArrangeService(regionBorder, ArrangeParameter);
                    break;
                case ArrangeEdition.Third:
                    arrange = new ThThirdwayArrangeService(regionBorder, ArrangeParameter);
                    break;
                default:
                    return;
            }

            if(arrange!=null)
            {
                arrange.Arrange();
                Graphs = arrange.Graphs;
                LoopNumber = arrange.LoopNumber;
                CenterSideDicts = arrange.CenterSideDicts;
                CenterGroupLines = arrange.CenterGroupLines;
            }
        }
    }
}
