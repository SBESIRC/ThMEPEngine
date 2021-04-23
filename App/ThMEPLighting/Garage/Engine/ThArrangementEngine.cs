using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.Garage.Engine
{
    public abstract class ThArrangementEngine : IDisposable
    {
        protected List<Line> DxLines { get; set; }
        protected List<Line> FdxLines { get; set; }
        protected ThRacewayParameter RacewayParameter { get; set; }
        protected ThLightArrangeParameter ArrangeParameter { get; set; }
        public ThArrangementEngine(
            ThLightArrangeParameter arrangeParameter,
            ThRacewayParameter racewayParameter)
        {
            DxLines = new List<Line>();
            FdxLines = new List<Line>();
            ArrangeParameter = arrangeParameter;
            RacewayParameter = racewayParameter;
        }
        public void Dispose()
        {
        }
        public abstract void Arrange(List<ThRegionBorder> regionBorders);
        protected List<Line> Trim(List<Line> lines, Polyline regionBorder)
        {
            var results = ThCADCoreNTSGeometryClipper.Clip(regionBorder, lines.ToCollection());
            return ThLaneLineEngine.Explode(results).Cast<Line>().ToList();
        }
        protected void Preprocess(ThRegionBorder regionBorder)
        {
            DxLines = new List<Line>();
            FdxLines = new List<Line>();
            // 裁剪并获取框内的车道线
            var dxTrimLines = Trim(regionBorder.DxCenterLines, regionBorder.RegionBorder);
            var fdxTrimLines = Trim(regionBorder.FdxCenterLines, regionBorder.RegionBorder);
            if (dxTrimLines.Count == 0)
            {
                return;
            }            
            // 为了避免线槽和防火卷帘冲突
            // 缩短车道线，和框线保持500的间隙
            var shortenPara = new ThShortenParameter
            {
                Border = regionBorder.RegionBorder,
                DxLines = dxTrimLines,
                FdxLines = fdxTrimLines,
                Distance = ThGarageLightCommon.RegionBorderBufferDistance
            };
            var dxLines = ThShortenLineService.Shorten(shortenPara);
            var fdxLines = fdxTrimLines;
            if (dxLines.Count == 0)
            {
                return;
            }            
            // 将车道线规整
            dxLines = ThPreprocessLineService.Preprocess(dxLines);
            if (dxLines.Count == 0)
            {
                return;
            }

            // 过滤车道线
            if (!ArrangeParameter.IsSingleRow)
            {
                dxLines = ThFilterTTypeCenterLineService.Filter(dxLines, ArrangeParameter.MinimumEdgeLength);
                dxLines = ThFilterMainCenterLineService.Filter(dxLines, ArrangeParameter.RacywaySpace / 2.0);
                dxLines = ThFilterElbowCenterLineService.Filter(dxLines, ArrangeParameter.MinimumEdgeLength);
            }
            if (dxLines.Count == 0)
            {
                return;
            }

            // 保持车道线和非车道线
            DxLines.AddRange(dxLines);
            FdxLines.AddRange(fdxLines);
        }
    }
}
