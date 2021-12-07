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
        public void getCenterLine(DBObjectCollection centerlines, Point3d p1, Point3d p2)
        {
            var mat = Matrix3d.Displacement(-p1.GetAsVector());
            foreach (Line l in centerlines)
                l.TransformBy(mat);
            searchCenterLine(centerlines, p1.TransformBy(mat), SearchBreakType.breakWithEndline);
            searchCenterLine(centerlines, p2.TransformBy(mat), SearchBreakType.breakWithEndline);
            mat = Matrix3d.Displacement(p1.GetAsVector());
            foreach (Line l in connectLines)
                l.TransformBy(mat);
        }
        public void searchCenterLine(DBObjectCollection lines, Point3d startPoint, SearchBreakType type)
        {
            index = new ThCADCoreNTSSpatialIndex(lines);
            var pl = new Polyline();
            pl.CreatePolygon(startPoint.ToPoint2D(), 4, 1);
            var res = index.SelectCrossingPolygon(pl);
            if (res.Count != 1)
                return;
            var l = res[0] as Line;
            srtLine = l;
            var detectPoint = ThMEPHVACService.GetOtherPoint(l, startPoint, tor);
            if (identifier.Add(l.GetHashCode()))
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
                case SearchBreakType.breakWithEndline: searchConnLine(detectPoint, l);break;
                case SearchBreakType.breakWithElbow: searchConnLineBreakByElbow(detectPoint, l); break;
                case SearchBreakType.breakWithTee: searchConnLineBreakByTee(detectPoint, l); break;
                case SearchBreakType.breakWithCross: searchConnLineBreakByCross(detectPoint, l); break;
                case SearchBreakType.breakWithTeeAndCross: searchConnLineBreakByTeeAndCross(detectPoint, l); break;
            }
            if (isOrigine)
                connectLines.Add(l);
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
