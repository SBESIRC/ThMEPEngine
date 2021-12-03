using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public class ThAvoidBeamLayoutPointService : ThLayoutPointService
    {
        private DBObjectCollection Beams { get; set; }
        public ThAvoidBeamLayoutPointService(DBObjectCollection beams)
        {
            Beams = beams;
        }
        public override List<Point3d> Layout(List<Line> dxLines)
        {
            var layoutLines = dxLines.Calculate(Beams);
            return LinearDistribute(layoutLines,this.Margin,this.Interval);
        }

        public override List<Point3d> Layout(List<Line> L1Lines, List<Line> L2Lines)
        {
            var results = new List<Point3d>();
            var l1LayoutLines = L1Lines.Calculate(Beams);
            var l2LayoutLines = L2Lines.Calculate(Beams);
            var l1l2PubExclusiveInfo = CalculatePubExclusiveLines(l1LayoutLines, l2LayoutLines);
            var l1PubLayoutPoints = LinearDistribute(l1l2PubExclusiveInfo.L1Pubs, this.Margin, this.Interval);
            var l2PubLayoutPoints = GetL2LayoutPointByPass(l1PubLayoutPoints, L1Lines, L2Lines);
            var l1ExclusiveLayoutPoints = LinearDistribute(l1l2PubExclusiveInfo.L1Exclusives, this.Margin, this.Interval);
            var l2ExclusiveLayoutPoints = LinearDistribute(l1l2PubExclusiveInfo.L2Exclusives, this.Margin, this.Interval);
            results.AddRange(l1PubLayoutPoints);
            results.AddRange(l2PubLayoutPoints);
            results.AddRange(l1ExclusiveLayoutPoints);
            results.AddRange(l2ExclusiveLayoutPoints);
            return results;
        }
    }
}
