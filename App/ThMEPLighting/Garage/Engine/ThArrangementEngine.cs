using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;

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
            // Clip的结果中可能有点（DBPoint)，这里可以忽略点
            var results = ThCADCoreNTSGeometryClipper.Clip(regionBorder, lines.ToCollection());
            var curves = results.OfType<Curve>().ToCollection();
            return ThLaneLineEngine.Explode(curves).Cast<Line>().ToList();
        }
        protected void TrimAndShort(ThRegionBorder regionBorder)
        {
            //裁剪 和 缩短
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
            
            // 保持车道线和非车道线
            DxLines.AddRange(dxLines);
            FdxLines.AddRange(fdxLines);
        }

        protected void CleanAndFilter()
        {
            // 将车道线规整
            DxLines = ThPreprocessLineService.Preprocess(DxLines);
            if (DxLines.Count == 0)
            {
                return;
            }
            double tTypeBranchFilterLength = Math.Max(ArrangeParameter.MinimumEdgeLength,
                ArrangeParameter.Margin*2.0+ ArrangeParameter.Interval / 2.0);
            DxLines = ThFilterTTypeCenterLineService.Filter(DxLines, tTypeBranchFilterLength);
            // 过滤车道线
            if (!ArrangeParameter.IsSingleRow)
            {                
                DxLines = ThFilterMainCenterLineService.Filter(DxLines, ArrangeParameter.RacywaySpace / 2.0);
                DxLines = ThFilterElbowCenterLineService.Filter(DxLines, ArrangeParameter.MinimumEdgeLength);
            }            
        }

        protected virtual List<Curve> MergeDxLine(Polyline border, List<Line> dxLines)
        {
            //单位化、修正方向
            var dxNomalLines = new List<Line>();
            dxLines.ForEach(o => dxNomalLines.Add(ThGarageLightUtils.NormalizeLaneLine(o)));
            //从小汤车道线合并服务中获取合并的主道线，辅道线            
            return ThMergeLightCenterLines.Merge(border, dxNomalLines, ThGarageLightCommon.LaneMergeRange);
        }
        protected List<Line> Explode(List<Curve> curves)
        {
            var results = new List<Line>();
            curves.ForEach(c =>
            {
                if(c is Line line)
                {
                    results.Add(line.Clone() as Line);
                }
                else if(c is Polyline poly)
                {
                    results.AddRange(poly.ToLines());
                }
                else
                {
                    throw new NotSupportedException();
                }
            });
            return results;
        }
    }
}
