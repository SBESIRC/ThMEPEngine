using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public class ThColumnSpanLayoutPointService : ThLayoutPointService
    {
        private double NearbyDistance { get; set; }
        private ThCADCoreNTSSpatialIndex SpatialIndex { get; set; }
        public ThColumnSpanLayoutPointService(DBObjectCollection columns,double nearbyDistance)
        {
            NearbyDistance = nearbyDistance;
            SpatialIndex = new ThCADCoreNTSSpatialIndex(columns);
        }
        public override List<Tuple<Point3d,Vector3d>> Layout(List<Line> dxLines)
        {
            var results = new List<Tuple<Point3d, Vector3d>>();
            var newDxLines = ThMergeLightLineService.Merge(dxLines);
            newDxLines.ForEach(link =>
            {
                var unLayoutLines = link.SelectMany(l => GetProjectionLines(l)).ToList();
                var path = link.ToPolyline();
                var pts = PolylineDistribute(path, unLayoutLines, this.Interval, this.Margin,this.LampLength);
                results.AddRange(DistributeLaytoutPoints(pts,link));
                path.Dispose();
                unLayoutLines.ForEach(l => l.Dispose());
            });
            return results;
        }
        private List<Line> GetProjectionLines(Line l)
        {
            var results = new List<Line>();
            var columns = Query(l);
            return columns.OfType<Polyline>().Select(p => GetProjectionLine(p, l)).ToList();
        }

        private Line GetProjectionLine(Polyline poly,Line line)
        {
            var pts = poly.Vertices();
            var projectionPts = pts.OfType<Point3d>().Select(p => p.GetProjectPtOnLine(line.StartPoint, line.EndPoint)).ToList();
            var pair = ThGeometryTool.GetCollinearMaxPts(projectionPts);
            return new Line(pair.Item1, pair.Item2);
        }

        public override List<Tuple<Point3d, Vector3d>> Layout(List<Line> L1Lines, List<Line> L2Lines)
        {
            var results = new List<Tuple<Point3d, Vector3d>>();
            var l1l2PubExclusiveLines = CalculatePubExclusiveLines(L1Lines, L2Lines);
            var l1LayoutPoints = Layout(L1Lines); // L1上创建的点
            var l2PassPoints = GetL2LayoutPointByPass(l1LayoutPoints, L1Lines, L2Lines); // L1 传递到 L2上的点
            var l2ExclusiveLayoutPoints = Layout(l1l2PubExclusiveLines.L2Exclusives);
            results.AddRange(l1LayoutPoints);
            results.AddRange(l2PassPoints);
            results.AddRange(l2ExclusiveLayoutPoints);
            return results;
        }

        private List<Line> Split(List<Line> lines,double ignoreDis =1.0)
        {
            var results = new List<Line>();
            lines.ForEach(l =>
            {
                var columns = Query(l);
                var projectionPoints = columns
                .OfType<Polyline>()
                .Select(p => p.GetMinimumRectangle())
                .Select(p => p.GetPoint3dAt(0).GetMidPt(p.GetPoint3dAt(2)))
                .Select(p => p.GetProjectPtOnLine(l.StartPoint, l.EndPoint))
                .ToList();
                results.AddRange(l.Split(projectionPoints, ignoreDis));
            });
            return results;
        }

        private DBObjectCollection Query(Line line)
        {
            var rec = line.Buffer(NearbyDistance);
            return SpatialIndex.SelectCrossingPolygon(rec);
        }
    }
}
