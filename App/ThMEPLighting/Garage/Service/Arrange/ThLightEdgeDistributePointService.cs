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
using ThMEPLighting.Garage.Service.LayoutPoint;

namespace ThMEPLighting.Garage.Service.Arrange
{
    /// <summary>
    /// 给1、2号灯线的边布点
    /// </summary>
    internal class ThLightEdgeDistributePointService
    {
        #region ---------- input ----------
        public List<ThLightEdge> FirstLightEdges { get; set; } = new List<ThLightEdge>();
        public List<ThLightEdge> SecondLightEdges { get; set; } = new List<ThLightEdge>();
        public DBObjectCollection Beams { get; set; } = new DBObjectCollection();
        public DBObjectCollection Columns { get; set; } = new DBObjectCollection();
        public ThLightArrangeParameter ArrangeParameter { get; set; } = new ThLightArrangeParameter();
        #endregion
        public ThLightEdgeDistributePointService()
        {
        }

        public void Distribute()
        {
            // 布点
            var points = LayoutPoints();
            var linePoints = new Dictionary<Line, List<Point3d>>();
            linePoints = ThQueryPointService.Query(points, Union(GetEdges(FirstLightEdges), GetEdges(SecondLightEdges)));
            linePoints = Sort(linePoints);

            // 优化布置的点
            // 因为1、2号线在拐弯和分支的地方被裁剪了，无需再处理了
            //var optimizer = new ThLayoutPointOptimizeService(linePoints, ArrangeParameter.FilterPointDistance);
            //optimizer.Optimize();

            FirstLightEdges.ForEach(f =>
            {
                linePoints[f.Edge].ForEach(p =>
                {
                    f.LightNodes.Add(new ThLightNode() { Position = p });
                });
            });
            SecondLightEdges.ForEach(f =>
            {
                linePoints[f.Edge].ForEach(p =>
                {
                    f.LightNodes.Add(new ThLightNode() { Position = p });
                });
            });
        }

        private List<Line> GetEdges(List<ThLightEdge> edges)
        {
            return edges.Select(o => o.Edge).ToList();
        }

        private List<Tuple<Point3d, Vector3d>> LayoutPoints()
        {
            // Curve 仅支持Line，和Line组成的多段线
            var results = new List<Tuple<Point3d, Vector3d>>();
            ThLayoutPointService layoutPointService = null;
            switch (ArrangeParameter.LayoutMode)
            {
                case LayoutMode.AvoidBeam:
                    layoutPointService = new ThAvoidBeamLayoutPointService(Beams);
                    layoutPointService.LampLength = ArrangeParameter.LampLength;
                    break;
                case LayoutMode.ColumnSpan:
                    layoutPointService = new ThColumnSpanLayoutPointService(Columns,
                        ArrangeParameter.NearByDistance);
                    layoutPointService.LampLength = ArrangeParameter.LampLength;
                    break;
                case LayoutMode.SpanBeam:
                    layoutPointService = new ThSpanBeamLayoutPointService(Beams);
                    break;
                default:
                    layoutPointService = new ThEqualDistanceLayoutPointService();
                    break;
            }
            if (layoutPointService != null)
            {
                layoutPointService.Margin = ArrangeParameter.Margin;
                layoutPointService.Interval = ArrangeParameter.Interval;
                layoutPointService.DoubleRowOffsetDis = ArrangeParameter.DoubleRowOffsetDis;

                // 先做内缩处理
                var firstShortenLines = ThLightingLineCorrectionService.SingleRowCorrect(
                GetEdges(FirstLightEdges), GetEdges(SecondLightEdges), ArrangeParameter.ShortenDistance);
                //Print.ThPrintService.Print(firstShortenLines.Select(o => o.Clone() as Curve).ToList(), 1);

                // 将1号线映射到
                var secondShortenLines = GetSecondShortLines(firstShortenLines, GetEdges(SecondLightEdges));
                //Print.ThPrintService.Print(secondShortenLines.Select(o => o.Clone() as Curve).ToList(), 2);
                
                results = layoutPointService.Layout(firstShortenLines, secondShortenLines);
            }
            return results;
        }

        private List<Line> GetSecondShortLines(List<Line> firstShortenLines, List<Line> secondLines)
        {
            var results = new List<Line>();
            var firstPairService = new ThFirstSecondPairService(firstShortenLines, secondLines, ArrangeParameter.DoubleRowOffsetDis);
            firstShortenLines.ForEach(l =>
            {
                firstPairService.Query(l).ForEach(s =>
                {
                    var pts = new List<Point3d> { s.StartPoint, s.EndPoint };
                    var pt1 = l.StartPoint.GetProjectPtOnLine(s.StartPoint, s.EndPoint);
                    if (pt1.IsPointOnCurve(s, 1.0))
                    {
                        pts.Add(pt1);
                    }
                    var pt2 = l.EndPoint.GetProjectPtOnLine(s.StartPoint, s.EndPoint);
                    if (pt2.IsPointOnCurve(s, 1.0))
                    {
                        pts.Add(pt2);
                    }
                    pts = pts.OrderBy(p => p.DistanceTo(s.StartPoint)).ToList();
                    results.Add(new Line(pts.First(), pts.Last()));
                });
            });
            return results;
        }

        private Dictionary<Line, List<Point3d>> Sort(Dictionary<Line, List<Point3d>> linePoints)
        {
            var result = new Dictionary<Line, List<Point3d>>();
            linePoints.ForEach(o =>
            {
                var pts = o.Value.OrderBy(p => p.DistanceTo(o.Key.StartPoint)).ToList();
                result.Add(o.Key, pts);
            });
            return result;
        }

        private List<Line> Union(List<Line> firstLines, List<Line> secondLines)
        {
            var results = new List<Line>();
            results.AddRange(firstLines);
            results.AddRange(secondLines);
            return results;
        }

        private DBObjectCollection Query(ThCADCoreNTSSpatialIndex spatialIndex, Line line)
        {
            var outline = ThDrawTool.ToRectangle(line.StartPoint, line.EndPoint, 1.0);
            var results = spatialIndex.SelectCrossingPolygon(outline)
                .OfType<Line>()
                .Where(o => ThGeometryTool.IsCollinearEx(line.StartPoint, line.EndPoint, o.StartPoint, o.EndPoint))
                .ToCollection();
            outline.Dispose();
            return results;
        }
    }
}
