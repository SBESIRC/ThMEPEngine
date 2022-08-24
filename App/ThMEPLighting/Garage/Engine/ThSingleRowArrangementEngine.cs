using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using ThMEPLighting.Common;
using Autodesk.AutoCAD.Geometry;
using ThMEPLighting.Garage.Model;
using System.Collections.Generic;
using ThMEPLighting.Garage.Service;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Garage.Service.Number;
using ThMEPLighting.Garage.Service.LayoutPoint;
using System;

namespace ThMEPLighting.Garage.Engine
{
    public class ThSingleRowArrangementEngine : ThArrangementEngine
    {
        public ThSingleRowArrangementEngine(
            ThLightArrangeParameter arrangeParameter)
            : base(arrangeParameter)
        {
        }
        public override void Arrange(ThRegionBorder regionBorder)
        {
            // 预处理
            if(IsPreProcess)
            {
                Preprocess(regionBorder);
            }
            
            // 布点
            var linePoints = new Dictionary<Line, List<Point3d>>();
            var points = LayoutPoints(regionBorder);
            linePoints = ThQueryPointService.Query(points, regionBorder.DxCenterLines);

            // 优化布置的点
            var optimizer = new ThLayoutPointOptimizeService(linePoints, ArrangeParameter.FilterPointDistance);
            optimizer.Optimize();

            // 计算回路数量
            var lightNumber = linePoints.Sum(o => o.Value.Count);
            LoopNumber = ArrangeParameter.GetLoopNumber(lightNumber);

            // 创建边
            var lightEdges = BuildEdges(linePoints);

            var pts = lightEdges.Select(o => o.Edge.StartPoint.ToString() + "  " + o.Edge.EndPoint.ToString());
                 
            // 编号
            Graphs = lightEdges.CreateGraphs();
            Graphs.ForEach(g =>
            {
                g.Number(LoopNumber, true, base.DefaultStartNumber, ArrangeParameter.IsDoubleRow);
            });
        }
        protected override void Preprocess(ThRegionBorder regionBorder )
        {
            regionBorder.Trim(); // 裁剪           
            regionBorder.Shorten(ThGarageLightCommon.RegionBorderBufferDistance); // 缩短
            regionBorder.Noding(); //
            regionBorder.HandleSharpAngle(); //
            regionBorder.CleanNoding();
            Filter(regionBorder); // 过滤
            regionBorder.Normalize(); //单位化
            regionBorder.Sort(); // 排序
        }
        private void Filter(ThRegionBorder regionBorder)
        {
            // 对于较短的灯线且一端未连接任何线，另一端连接在线上
            var limitLength = ArrangeParameter.LampLength + ArrangeParameter.Margin * 2;

            var filter1 = new ThFilterShortLinesService(regionBorder.DxCenterLines, limitLength);
            regionBorder.DxCenterLines = filter1.FilterIsolatedLine();
        }
        private List<ThLightEdge> BuildEdges(Dictionary<Line, List<Point3d>> edgePoints)
        {
            var lightEdges = new List<ThLightEdge>();
            edgePoints.ForEach(e =>
            {
                var lightEdge = new ThLightEdge(e.Key);
                e.Value.ForEach(p => lightEdge.LightNodes.Add(new ThLightNode { Position = p }));
                lightEdges.Add(lightEdge);
            });
            return lightEdges;
        }
        private List<Tuple<Point3d, Vector3d>> LayoutPoints(ThRegionBorder regionBorder)
        {
            // Curve 仅支持Line，和Line组成的多段线
            var results = new List<Tuple<Point3d, Vector3d>>();
            ThLayoutPointService layoutPointService = null;
            switch (ArrangeParameter.LayoutMode)
            {
                case LayoutMode.AvoidBeam:
                    layoutPointService = new ThAvoidBeamLayoutPointService(
                        regionBorder.Beams.Select(b => b.Outline).ToCollection());
                    layoutPointService.LampLength = ArrangeParameter.LampLength;
                    break;
                case LayoutMode.ColumnSpan:
                    layoutPointService = new ThColumnSpanLayoutPointService(
                        regionBorder.Columns.Select(c => c.Outline).ToCollection(),
                        ArrangeParameter.NearByDistance);
                    layoutPointService.LampLength = ArrangeParameter.LampLength;
                    break;
                case LayoutMode.SpanBeam:
                    layoutPointService = new ThSpanBeamLayoutPointService(
                        regionBorder.Beams.Select(b => b.Outline).ToCollection());
                    break;
                default:
                    layoutPointService = new ThEqualDistanceLayoutPointService();
                    break;
            }
            if (layoutPointService != null)
            {
                layoutPointService.Margin = ArrangeParameter.Margin;
                layoutPointService.Interval = ArrangeParameter.Interval;
                var canLayoutLines = ThLightingLineCorrectionService.SingleRowCorrect(
                    regionBorder.DxCenterLines,new List<Line>(), ArrangeParameter.ShortenDistance);
                results = layoutPointService.Layout(canLayoutLines);
            }
            return results;
        }
    }
}
