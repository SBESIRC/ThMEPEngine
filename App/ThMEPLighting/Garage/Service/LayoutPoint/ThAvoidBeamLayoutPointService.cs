using System;
using System.Linq;
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
        public override List<Tuple<Point3d,Vector3d>> Layout(List<Line> dxLines)
        {
            var results = new List<Tuple<Point3d, Vector3d>>();
            var newDxLines = ThMergeLightLineService.Merge(dxLines);
            newDxLines.ForEach(link =>
            {
                var unLayoutLines = link.CalculateUnLayoutParts(Beams);
                var path = link.ToPolyline();
                var pts = PolylineDistribute(path, unLayoutLines, this.Interval, this.Margin,this.LampLength);
                results.AddRange(DistributeLaytoutPoints(pts, link));
                unLayoutLines.ForEach(l => l.Dispose());
            });
            return results;
        }

        public override List<Tuple<Point3d, Vector3d>> Layout(List<Line> L1Lines, List<Line> L2Lines)
        {
            var results = new List<Tuple<Point3d, Vector3d>>();
            // 计算L1,L2不可布区域
            var l1UnLayoutLines = L1Lines.CalculateUnLayoutParts(Beams);
            var l2UnLayoutLines = L2Lines.CalculateUnLayoutParts(Beams);

            // 计算L1上布置的点
            var newL1Lines = ThMergeLightLineService.Merge(L1Lines);
            // 把L2不可布区域投影到L1上
            var l1NewUnLayoutLines = GetProjectionLinesByPass(l2UnLayoutLines, L2Lines, L1Lines);
            var l1UnLayoutQuery = ThQueryLineService.Create(l1UnLayoutLines.Union(l1NewUnLayoutLines).ToList());
            var l1LayoutPoints =new List<Tuple<Point3d, Vector3d>>();
            newL1Lines.ForEach(link =>
            {
                var unLayoutLines = link.SelectMany(o=> l1UnLayoutQuery.QueryCollinearLines(o.StartPoint,o.EndPoint)).ToList();
                var path = link.ToPolyline();
                var pts = PolylineDistribute(path, unLayoutLines, this.Interval, this.Margin,this.LampLength);
                l1LayoutPoints.AddRange(DistributeLaytoutPoints(pts,link));
            });

            // 把L1上布置的点投影到L2上
            var l2LayoutPoints = GetL2LayoutPointByPass(l1LayoutPoints, L1Lines, L2Lines);
            var l1LayoutLines = L1Lines.CalculateLayoutParts(Beams);
            var l2LayoutLines = L2Lines.CalculateLayoutParts(Beams);
            var l1l2PubExclusiveInfo = CalculatePubExclusiveLines(l1LayoutLines, l2LayoutLines);
            var newL2Exclusives = ThMergeLightLineService.Merge(l1l2PubExclusiveInfo.L2Exclusives);
            var l2UnLayoutQuery = ThQueryLineService.Create(l2UnLayoutLines);
            newL2Exclusives.ForEach(link =>
            {
                var unLayoutLines = link.SelectMany(o => l2UnLayoutQuery.QueryCollinearLines(o.StartPoint, o.EndPoint)).ToList();
                var path = link.ToPolyline();
                var pts = PolylineDistribute(path, unLayoutLines, this.Interval, this.Margin,this.LampLength);
                l2LayoutPoints.AddRange(DistributeLaytoutPoints(pts, link));
            });
            
            results.AddRange(l1LayoutPoints);
            results.AddRange(l2LayoutPoints);
            return results;
        }
    }
}
