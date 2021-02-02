using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    public class ThAlignLineHeadService
    {
        private List<Line> Lines { get; set; }
        private Line SideLine { get; set; }
        private Line Center { get; set; }
        public List<Line> OldEdgeLines { get; private set; }
        public List<Line> NewEdgeLines { get; private set; }
        public List<Line> OldSideLines { get; private set; }
        public List<Line> NewSideLines { get; private set; }
        /// <summary>
        /// 偏移长度
        /// </summary>
        private double Distance { get; set; }
        private ThAlignLineHeadService(List<Line> lines, Line center, Line sideLine,double distance)
        {
            Lines = lines;
            Center = center;
            SideLine = sideLine;
            Distance = distance;
            OldEdgeLines = new List<Line>();
            NewEdgeLines = new List<Line>();
            OldSideLines = new List<Line>();
            NewSideLines = new List<Line>();
        }
        public static ThAlignLineHeadService Align(List<Line> lines, Line center, Line sideLine, double distance)
        {
            var instance = new ThAlignLineHeadService(lines, center, sideLine, distance);
            instance.Align();
            return instance;
        }
        private void Align()
        {
            DBObjectCollection dbObjs = new DBObjectCollection();
            Lines.ForEach(o => dbObjs.Add(o));
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            var outline = ThDrawTool.ToOutline(SideLine.StartPoint,SideLine.EndPoint,1.0);
            var objs=spatialIndex.SelectCrossingPolygon(outline);
            var parallelLines = GetQualifiedLines(objs);
            if (parallelLines.Count == 2)
            {
                OldEdgeLines = parallelLines;
                var pairPts = GetHeadAlignPairPts(parallelLines[0], parallelLines[1]);
                var first = Extend(parallelLines[0], pairPts.Select(o => o.Item1).ToList());
                var second = Extend(parallelLines[1], pairPts.Select(o => o.Item2).ToList());
                NewEdgeLines.Add(first);
                NewEdgeLines.Add(second);
                OldSideLines.Add(SideLine);
                NewSideLines = CreateNewSideLines(pairPts);
            }
        }
        private List<Line> CreateNewSideLines(List<Tuple<Point3d, Point3d>> pairPts)
        {
            List<Line> sideLines = new List<Line>();
            pairPts.ForEach(o =>
            {
                Point3d midPt = ThGeometryTool.GetMidPt(o.Item1, o.Item2);
                Point3d portPt = midPt.DistanceTo(Center.StartPoint) < midPt.DistanceTo(Center.EndPoint)
                ? Center.StartPoint : Center.EndPoint;
                Vector3d vec = Center.LineDirection().GetPerpendicularVector();
                Point3d sp = portPt + vec.MultiplyBy(Distance);
                Point3d ep = portPt - vec.MultiplyBy(Distance);
                sideLines.Add(new Line(sp, ep));
            });
            return sideLines;
        }
        private List<Tuple<Point3d, Point3d>> GetHeadAlignPairPts(Line first, Line second)
        {
            List<Tuple<Point3d, Point3d>> pairPts = new List<Tuple<Point3d, Point3d>>();           
            pairPts.Add(Tuple.Create(first.StartPoint, second.StartPoint));
            pairPts.Add(Tuple.Create(first.EndPoint, second.EndPoint));
            pairPts.Add(Tuple.Create(first.StartPoint, second.EndPoint));
            pairPts.Add(Tuple.Create(first.EndPoint, second.StartPoint));
            return pairPts
                .Where(o => IsLinePortClosed(o.Item1) && 
                IsLinePortClosed(o.Item2)).ToList();
        }
        private Line Extend(Line oldLine,List<Point3d> extendPts)
        {
            if(extendPts.Count==0)
            {
                return new Line();
            }
            var newLine = new Line(oldLine.StartPoint, oldLine.EndPoint);
            extendPts.ForEach(o =>
            {
                if (newLine.StartPoint.DistanceTo(o) <
                newLine.EndPoint.DistanceTo(o))
                {
                    newLine.StartPoint = newLine.StartPoint + newLine.LineDirection().MultiplyBy(Distance);
                }
                else
                {
                    newLine.EndPoint = newLine.EndPoint - newLine.LineDirection().MultiplyBy(Distance);
                }
            });
            return newLine;
        }
        private List<Line> GetSideLines(ThCADCoreNTSSpatialIndex spatialIndex, List<Tuple<Point3d, Point3d>> pairPts)
        {
            List<Line> sideLines = new List<Line>();
            pairPts.ForEach(m =>
            {
                var outline = ThDrawTool.ToOutline(m.Item1, m.Item2, 1.0);
                var dbObjs = spatialIndex.SelectCrossingPolygon(outline);
                var lines = dbObjs.Cast<Line>().Where(n => IsInRange(n.Length / 2.0) &&
                     ThGeometryTool.IsCollinearEx(m.Item1, m.Item2, n.StartPoint, n.EndPoint)).ToList();
                sideLines.AddRange(lines);
            });
            return sideLines;
        }
        private List<Line> GetQualifiedLines(DBObjectCollection objs)
        {
            //获取与中心线符合条件的线
            //与中心线平行
            //与中心线的距离为偏移距离
            //与中心线的头部距离为偏移距离
            var vec = SideLine.LineDirection().GetPerpendicularVector();
            return objs.Cast<Line>()
                .Where(o => vec.IsParallelToEx(o.LineDirection()))
                .Where(o=> o.IsIntersects(SideLine))
                .Where(o => IsLinePortClosed(o.StartPoint) ||
                IsLinePortClosed(o.EndPoint)).ToList();
        }
        private bool IsLinePortClosed(Point3d pt)
        {
            return SideLine.StartPoint.DistanceTo(pt) <= 1.0 ||
                SideLine.EndPoint.DistanceTo(pt) <= 1.0;
        }

        private bool IsInRange(double distance,double tolerance=1.0)
        {
            return distance >= (Distance - tolerance) &&
                distance <= (Distance + tolerance);
        }
    }
}
