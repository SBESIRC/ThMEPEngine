using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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
        public override List<Point3d> Layout(List<Line> dxLines)
        {
            var results = new List<Point3d>();
            var splitLines = Split(dxLines);
            return LinearDistribute(splitLines,this.Margin,this.Interval);
        }

        public override List<Point3d> Layout(List<Line> L1Lines, List<Line> L2Lines)
        {
            var results = new List<Point3d>();
            var l1l2PubExclusiveLines = CalculatePubExclusiveLines(L1Lines, L2Lines);
            var l1PubLayoutPoints = Layout(l1l2PubExclusiveLines.L1Pubs); // L1上创建的点
            var l2PubLayoutPoints = GetL2LayoutPointByPass(l1PubLayoutPoints, L1Lines, L2Lines);
            var l1ExclusiveLayoutPoints = Layout(l1l2PubExclusiveLines.L1Exclusives);
            var l2ExclusiveLayoutPoints = Layout(l1l2PubExclusiveLines.L2Exclusives);
            results.AddRange(l1PubLayoutPoints);
            results.AddRange(l2PubLayoutPoints);
            results.AddRange(l1ExclusiveLayoutPoints);
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
