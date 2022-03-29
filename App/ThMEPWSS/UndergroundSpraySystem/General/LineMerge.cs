using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPWSS.Uitl;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class LineMerge
    {
        public static List<Line> CleanLaneLines(List<Line> lines)
        {
            var rstLines = new List<Line>();

            //Grouping
            var lineSegs = lines.Select(l => new LineSegment2d(l.StartPoint.ToPoint2D(), l.EndPoint.ToPoint2D())).ToList();
            List<HashSet<LineSegment2d>> lineSegGroups = new List<HashSet<LineSegment2d>>();

            while (lineSegs.Count() != 0)
            {
                var tmpLineSeg = lineSegs.First();
                bool alreadyContains = false;
                foreach (var g in lineSegGroups)
                {
                    if (g.Contains(tmpLineSeg))
                    {
                        alreadyContains = true;
                        break;
                    }
                }

                if (alreadyContains) continue;

                var colinerSegs = lineSegs.Where(l => l.IsParallelTo(tmpLineSeg, new Tolerance(0.001, 0.001))).ToHashSet();
                lineSegGroups.Add(colinerSegs);
                lineSegs = lineSegs.Except(colinerSegs).ToList();
            }

            foreach (var lg in lineSegGroups)
            {
                rstLines.AddRange(MergeGroupLines(lg));
            }

            return rstLines;
        }
        private static List<Line> MergeGroupLines(HashSet<LineSegment2d> lineGroup)
        {
            var rstLines = new List<Line>();
            while (lineGroup.Count != 0)
            {
                var l = lineGroup.First();
                lineGroup.Remove(l);
                rstLines.Add(MergeLine(ref l, ref lineGroup));
            }
            return rstLines;

        }
        private static Line MergeLine(ref LineSegment2d l, ref HashSet<LineSegment2d> lineGroup)
        {
            Line rstLine = new Line();

            MergeLineEx(ref l, ref lineGroup);
            rstLine.StartPoint = l.StartPoint.ToPoint3d();
            rstLine.EndPoint = l.EndPoint.ToPoint3d();
            return rstLine;
        }
        private static void MergeLineEx(ref LineSegment2d l, ref HashSet<LineSegment2d> lineGroup)
        {
            //如果 l 与 group里面任何一条线都没有交点，那么就把该l返回
            var overlapLine = IsOverlapLine(l, lineGroup);
            if (overlapLine.Count == 0)//如果没有相交
            {
                return;
            }
            else
            {
                //找到与l相交的线，然后，进行merge,并且把相交的线，从group里面删除
                l = MergeLineEX2(l, overlapLine);
                foreach (var line in overlapLine)
                {
                    lineGroup.Remove(line);
                }
                //merge 以后，继续执行MergeLine;
                MergeLineEx(ref l, ref lineGroup);
            }
        }
        private static HashSet<LineSegment2d> IsOverlapLine(LineSegment2d line, HashSet<LineSegment2d> lineGroup)
        {
            HashSet<LineSegment2d> overlapLine = new HashSet<LineSegment2d>();
            foreach (var l in lineGroup)
            {
                if (IsOverlapLine(line, l))
                {
                    overlapLine.Add(l);
                }
            }

            return overlapLine;
        }
        private static bool IsOverlapLine(LineSegment2d firLine, LineSegment2d secLine)
        {
            var overlapedSeg = firLine.Overlap(secLine, new Tolerance(0.01, 0.01));
            if (overlapedSeg != null)
            {
                return true;
            }
            else
            {
                var ptSet = new HashSet<Point3dEx>();
                var tol = 1E-2;
                ptSet.Add(new Point3dEx(firLine.StartPoint.X, firLine.StartPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(firLine.EndPoint.X, firLine.EndPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(secLine.StartPoint.X, secLine.StartPoint.Y, 0.0, tol));
                ptSet.Add(new Point3dEx(secLine.EndPoint.X, secLine.EndPoint.Y, 0.0, tol));
                if (ptSet.Count() == 3)
                {
                    return true;
                }
            }
            return false;
        }
        private static LineSegment2d MergeLineEX2(LineSegment2d line, HashSet<LineSegment2d> overlapLines)
        {
            List<Point3d> pts = new List<Point3d>();
            pts.Add(line.StartPoint.ToPoint3d());
            pts.Add(line.EndPoint.ToPoint3d());
            foreach (var l in overlapLines)
            {
                pts.Add(l.StartPoint.ToPoint3d());
                pts.Add(l.EndPoint.ToPoint3d());
            }
            var pairPt = pts.GetCollinearMaxPts();
            return new LineSegment2d(pairPt.Item1.ToPoint2d(), pairPt.Item2.ToPoint2d());
        }
        public static Tuple<Point3d, Point3d> GetCollinearMaxPts(this List<Point3d> pts)
        {
            if (pts.Count == 0)
            {
                return Tuple.Create(Point3d.Origin, Point3d.Origin);
            }
            else if (pts.Count == 1)
            {
                return Tuple.Create(pts[0], pts[0]);
            }
            else
            {
                Point3d first = pts[0];
                Point3d second = pts[pts.Count - 1];
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        if (pts[i].DistanceTo(pts[j]) > first.DistanceTo(second))
                        {
                            first = pts[i];
                            second = pts[j];
                        }
                    }
                }
                return Tuple.Create(first, second);
            }
        }
    }
}
