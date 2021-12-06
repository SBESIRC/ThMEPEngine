using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public class ThEqualDistanceLayoutPointService : ThLayoutPointService
    {
        public override List<Point3d> Layout(List<Line> L0Lines)
        {
            //传入的灯线是被清洗过的，请参照预处理过程
            var results = new List<Point3d>();
            var mergeLines = ThMergeLightLineService.Merge(L0Lines);
            mergeLines.ForEach(link =>
            {
                results.AddRange(LinearDistribute(link, this.Margin, this.Interval));
            });
            return results;
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
    }
}
