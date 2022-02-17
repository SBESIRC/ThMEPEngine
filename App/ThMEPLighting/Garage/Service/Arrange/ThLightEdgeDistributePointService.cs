using System.Linq;
using System.Collections.Generic;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using ThMEPLighting.Garage.Service.LayoutPoint;
using System;

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
            var linePoints = new Dictionary<Line, List<Point3d>>();
            var points = LayoutPoints();
            linePoints = ThQueryPointService.Query(points, Union(GetEdges(FirstLightEdges), GetEdges(SecondLightEdges)));
            linePoints = Sort(linePoints);

            // 优化布置的点
            var optimizer = new ThLayoutPointOptimizeService(linePoints, ArrangeParameter.FilterPointDistance);
            optimizer.Optimize();

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
                results = layoutPointService.Layout(GetEdges(FirstLightEdges), GetEdges(SecondLightEdges));
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
    }
}
