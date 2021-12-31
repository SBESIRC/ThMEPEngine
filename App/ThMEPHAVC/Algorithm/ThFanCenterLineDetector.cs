using System.Collections.Generic;
using DotNetARX;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPHVAC.Model;

namespace ThMEPHVAC.Algorithm
{
    public enum SearchBreakType
    {
        breakWithEndline = 0,
        breakWithElbow = 1,
        breakWithTee = 2,
        breakWithCross = 3,
        breakWithTeeAndCross = 4
    }
    public class ThFanCenterLineDetector
    {
        public Line srtLine;
        public Line lastLine;
        public Dictionary<Point3d, Point3d> endPoints;// key->endPoint(同时保证唯一性) value->otherPoint
        public DBObjectCollection connectLines;
        private Tolerance tor;
        private bool isOrigine;
        private HashSet<int> identifier;
        private ThCADCoreNTSSpatialIndex index;
        public ThFanCenterLineDetector(bool isOrigine)
        {
            this.isOrigine = isOrigine;
            tor = new Tolerance(1.5, 1.5);
            identifier = new HashSet<int>();
            endPoints = new Dictionary<Point3d, Point3d>();
            connectLines = new DBObjectCollection();
        }
        private void FixFirstLine(Point3d startPoint, DBObjectCollection res, DBObjectCollection lines)
        {
            var l = res[0] as Line;
            srtLine = l;
            if (!startPoint.IsEqualTo(l.StartPoint, tor) && !startPoint.IsEqualTo(l.EndPoint, tor))
            {
                // 需要更新
                lines.Remove(l);
                var updateLine = (startPoint.DistanceTo(l.StartPoint) > startPoint.DistanceTo(l.EndPoint)) ?
                    new Line(startPoint, l.StartPoint) : new Line(startPoint, l.EndPoint);
                lines.Add(updateLine);
                srtLine = updateLine;
                index = new ThCADCoreNTSSpatialIndex(lines);
            }
        }
        public void SearchCenterLine(DBObjectCollection lines, Point3d startPoint, SearchBreakType type)
        {
            index = new ThCADCoreNTSSpatialIndex(lines);
            var pl = new Polyline();
            // 起始搜索点容差为100
            pl.CreatePolygon(startPoint.ToPoint2D(), 4, 100);
            var res = index.SelectCrossingPolygon(pl);
            if (res.Count != 1)
                return;
            FixFirstLine(startPoint, res, lines);
            var detectPoint = ThMEPHVACService.GetOtherPoint(srtLine, startPoint, tor);
            if (identifier.Add(srtLine.GetHashCode()))
            {
                //if (isOrigine)
                //    connectLines.Add(l);
                //else
                //    connectLines.Add(new Line(startPoint, detectPoint));
            }
            else
                return; // The fan's in & out line is connected
            switch (type)
            {
                case SearchBreakType.breakWithEndline: searchConnLine(detectPoint, srtLine);break;
                case SearchBreakType.breakWithElbow: searchConnLineBreakByElbow(detectPoint, srtLine); break;
                case SearchBreakType.breakWithTee: searchConnLineBreakByTee(detectPoint, srtLine); break;
                case SearchBreakType.breakWithCross: searchConnLineBreakByCross(detectPoint, srtLine); break;
                case SearchBreakType.breakWithTeeAndCross: searchConnLineBreakByTeeAndCross(detectPoint, srtLine); break;
            }
            if (isOrigine)
                connectLines.Add(srtLine);
            else
                connectLines.Add(new Line(startPoint, detectPoint));
        }
        private void searchConnLineBreakByElbow(Point3d detectPoint, Line currentLine)
        {
            var res = detect(detectPoint, currentLine);
            identifier.Add(currentLine.GetHashCode());
            if (res.Count == 0 || res.Count == 1)
            {
                lastLine = currentLine;// 连向弯头的最后一条线
                return;
            }
            foreach (Line l in res)
            {
                if (!identifier.Add(l.GetHashCode()))
                    continue;
                var otherPoint = ThMEPHVACService.GetOtherPoint(l, detectPoint, tor);
                searchConnLineBreakByElbow(otherPoint, l);
                if (isOrigine)
                    connectLines.Add(l);
                else
                    connectLines.Add(new Line(detectPoint, otherPoint));
            }
        }
        private void searchConnLineBreakByTee(Point3d detectPoint, Line currentLine)
        {
            var res = detect(detectPoint, currentLine);
            identifier.Add(currentLine.GetHashCode());
            if (res.Count == 0 || res.Count == 2)
            {
                lastLine = currentLine;// 连向三通的最后一条线
                return;
            }
            foreach (Line l in res)
            {
                if (!identifier.Add(l.GetHashCode()))
                    continue;
                var otherPoint = ThMEPHVACService.GetOtherPoint(l, detectPoint, tor);
                searchConnLineBreakByTee(otherPoint, l);
                if (isOrigine)
                    connectLines.Add(l);
                else
                    connectLines.Add(new Line(detectPoint, otherPoint));
            }
        }
        private void searchConnLineBreakByCross(Point3d detectPoint, Line currentLine)
        {
            var res = detect(detectPoint, currentLine);
            identifier.Add(currentLine.GetHashCode());
            if (res.Count == 0 || res.Count == 3)
            {
                lastLine = currentLine;// 连向四通的最后一条线
                return;
            }
            foreach (Line l in res)
            {
                if (!identifier.Add(l.GetHashCode()))
                    continue;
                var otherPoint = ThMEPHVACService.GetOtherPoint(l, detectPoint, tor);
                searchConnLineBreakByCross(otherPoint, l);
                if (isOrigine)
                    connectLines.Add(l);
                else
                    connectLines.Add(new Line(detectPoint, otherPoint));
            }
        }
        private void searchConnLineBreakByTeeAndCross(Point3d detectPoint, Line currentLine)
        {
            var res = detect(detectPoint, currentLine);
            identifier.Add(currentLine.GetHashCode());
            if (res.Count == 0 || res.Count == 2 || res.Count == 3)
            {
                lastLine = currentLine;
                return;
            }
            foreach (Line l in res)
            {
                if (!identifier.Add(l.GetHashCode()))
                    continue;
                var otherPoint = ThMEPHVACService.GetOtherPoint(l, detectPoint, tor);
                searchConnLineBreakByTeeAndCross(otherPoint, l);
                if (isOrigine)
                    connectLines.Add(l);
                else
                    connectLines.Add(new Line(detectPoint, otherPoint));
            }
        }
        private void searchConnLine(Point3d detectPoint, Line currentLine)
        {
            var res = detect(detectPoint, currentLine);
            identifier.Add(currentLine.GetHashCode());
            if (res.Count == 0)
            {
                lastLine = currentLine;
                var otherPoint = ThMEPHVACService.GetOtherPoint(currentLine, detectPoint, tor);
                endPoints.Add(detectPoint, otherPoint);
                return;
            }
            foreach (Line l in res)
            {
                if (!identifier.Add(l.GetHashCode()))
                    continue;
                var otherPoint = ThMEPHVACService.GetOtherPoint(l, detectPoint, tor);
                searchConnLine(otherPoint, l);
                if (isOrigine)
                    connectLines.Add(l);
                else
                    connectLines.Add(new Line(detectPoint, otherPoint));
            }
        }
        private DBObjectCollection detect(Point3d detectPoint, Line currentLine)
        {
            var pl = new Polyline();
            pl.CreatePolygon(detectPoint.ToPoint2D(), 4, 10);
            var res = index.SelectCrossingPolygon(pl);
            res.Remove(currentLine);
            return res;
        }
    }
}