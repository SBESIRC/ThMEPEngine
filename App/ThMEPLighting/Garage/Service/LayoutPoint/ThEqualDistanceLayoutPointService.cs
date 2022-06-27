using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service.LayoutPoint
{
    public class ThEqualDistanceLayoutPointService : ThLayoutPointService
    {
        public override List<Tuple<Point3d,Vector3d>> Layout(List<Line> L0Lines)
        {
            //传入的灯线是被清洗过的，请参照预处理过程
            var results = new List<Tuple<Point3d, Vector3d>>();
            var mergeLines = ThMergeLightLineService.Merge(L0Lines);
            mergeLines.ForEach(link =>
            {
                results.AddRange(LinearDistribute(link, this.Margin, this.Interval));
            });
            return results;
        }

        public override List<Tuple<Point3d, Vector3d>> Layout(List<Line> L1Lines, List<Line> L2Lines)
        {
            var results = new List<Tuple<Point3d, Vector3d>>();
            var newL1Lines = Merge(L1Lines);
            var newL2Lines = Merge(L2Lines);
            var l1LayoutPoints = Layout(newL1Lines); // L1上创建的点
            var l2LayoutPoints = GetL2LayoutPointByPass(l1LayoutPoints, newL1Lines, newL2Lines);
            results.AddRange(l1LayoutPoints);
            results.AddRange(l2LayoutPoints);        
            return results;
        }
    }
}
