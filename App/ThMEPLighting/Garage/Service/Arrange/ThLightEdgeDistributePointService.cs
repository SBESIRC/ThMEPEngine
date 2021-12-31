using System;
using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
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
        public List<Line> FirstLines { get; set; } = new List<Line>();
        public List<Line> SecondLines { get; set; } = new List<Line>();
        public DBObjectCollection Beams { get; set; } = new DBObjectCollection();
        public DBObjectCollection Columns { get; set; } = new DBObjectCollection();
        public ThLightArrangeParameter ArrangeParameter { get; set; } = new ThLightArrangeParameter();
        #endregion
        public ThLightEdgeDistributePointService()
        {
        }

        public Tuple<List<ThLightEdge>, List<ThLightEdge>> Distribute()
        {
            // 创建边
            var firstLightEdges = BuildEdges(FirstLines, EdgePattern.First);
            var secondLightEdges = BuildEdges(SecondLines, EdgePattern.Second);

            // 布点
            var linePoints = new Dictionary<Line, List<Point3d>>();
            var points = LayoutPoints();
            linePoints = ThQueryPointService.Query(points, Union(FirstLines, SecondLines));
            linePoints = Sort(linePoints);

            // 优化布置的点
            var optimizer = new ThLayoutPointOptimizeService(linePoints, ArrangeParameter.FilterPointDistance);
            optimizer.Optimize();

            firstLightEdges.ForEach(f =>
            {
                linePoints[f.Edge].ForEach(p =>
                {
                    f.LightNodes.Add(new ThLightNode() { Position = p });
                });
            });
            secondLightEdges.ForEach(f =>
            {
                linePoints[f.Edge].ForEach(p =>
                {
                    f.LightNodes.Add(new ThLightNode() { Position = p });
                });
            });
            return Tuple.Create(firstLightEdges, secondLightEdges);
        }

        private List<Point3d> LayoutPoints()
        {
            // Curve 仅支持Line，和Line组成的多段线
            var results = new List<Point3d>();
            ThLayoutPointService layoutPointService = null;
            switch (ArrangeParameter.LayoutMode)
            {
                case LayoutMode.AvoidBeam:
                    layoutPointService = new ThAvoidBeamLayoutPointService(Beams);
                    break;
                case LayoutMode.ColumnSpan:
                    layoutPointService = new ThColumnSpanLayoutPointService(Columns,
                        ArrangeParameter.NearByDistance);
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
                layoutPointService.LampLength = ArrangeParameter.LampLength;
                layoutPointService.DoubleRowOffsetDis = ArrangeParameter.DoubleRowOffsetDis;
                results = layoutPointService.Layout(FirstLines, SecondLines);
            }
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
        private List<ThLightEdge> BuildEdges(List<Line> lines, EdgePattern edgePattern)
        {
            var edges = new List<ThLightEdge>();
            lines.ForEach(o => edges.Add(new ThLightEdge(o) { EdgePattern = edgePattern }));
            return edges;
        }
    }
}
