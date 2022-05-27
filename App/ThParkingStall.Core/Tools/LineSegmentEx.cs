using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using ThParkingStall.Core.MPartitionLayout;

namespace ThParkingStall.Core.Tools
{
    public static class LineSegmentEx
    {
        public static LineString GetLineString(this LineSegment line)
        {
            if (line == null) return null;
            var coors = new Coordinate[] { line.P0.Copy(), line.P1.Copy()};
            return new LineString(coors);
        }
        public static List<LineString> ToLineStrings(this List<LineSegment> lines,bool IgnoreNull = true)
        {
            var LineStrings = new List<LineString>();
            foreach (var line in lines)
            {
                var lstr = line.GetLineString();
                if (IgnoreNull && lstr == null) continue;
                LineStrings.Add(lstr);
            }
            return LineStrings;
        }
        public static Vector2D GetVector(this LineSegment line)
        {
            return new Vector2D(line.P0, line.P1);
        }
        public static LineSegment Clone(this LineSegment line)
        {
            return new LineSegment(line.P0.Copy(), line.P1.Copy());
        }
        public static bool IsVertical(this LineSegment line)
        {
            if (Math.Abs(line.P0.X - line.P1.X) < 1e-4) return true;
            else return false;
        }
        public static void ExtendToPoint(this LineSegment line, Coordinate pt)
        {
            var d0 = line.P0.Distance(pt);
            var d1 = line.P1.Distance(pt);
            if (d0 > d1)
            {
                if (d0 > line.Length)
                {
                     line.P1 = pt;
                }
            }
            else
            {
                if (d1 > line.Length)
                {
                    line.P0 = pt;
                }
            }
        }
        public static void _ExtendToPoint(this LineSegment line,Coordinate pt,double tol = 1)
        {
            var d0 = line.P0.Distance(pt);
            var d1 = line.P1.Distance(pt);
            if (d0 > d1)
            {
                if(d0 > line.Length)
                {
                    if (line.IsVertical())
                    {
                        if (line.P0.Y > pt.Y) line.P1 = new Coordinate(pt.X, pt.Y - tol);
                        else line.P1 = new Coordinate(pt.X, pt.Y + tol);
                    }
                    else
                    {
                        if (line.P0.X > pt.X) line.P1 = new Coordinate(pt.X - tol, pt.Y );
                        else line.P1 = new Coordinate(pt.X + tol, pt.Y );
                    }
                }
            }
            else
            {
                if (d1 > line.Length)
                {
                    if (line.IsVertical())
                    {
                        if (line.P1.Y > pt.Y) line.P0 = new Coordinate(pt.X, pt.Y - tol);
                        else line.P0 = new Coordinate(pt.X, pt.Y + tol);
                    }
                    else
                    {
                        if (line.P1.X > pt.X) line.P0 = new Coordinate(pt.X - tol, pt.Y);
                        else line.P0 = new Coordinate(pt.X + tol, pt.Y);
                    }
                }
            }
        }
        public static bool ConnectWithAny(this LineSegment line, List<LineSegment> otherLines)
        {
            foreach (var l2 in otherLines)
            {
                if (line.Intersection(l2) != null) return true;
            }
            return false;
        }
        //获取面域
        public static List<Polygon> GetPolygons(this List<LineSegment> lineSegments,List<LineString> rest = null)
        {
            var linestrings = new List<LineString>();
            lineSegments.ForEach(line => linestrings.Add(line.GetLineString()));
            if(rest != null)
            {
                linestrings.AddRange(rest);
            }
            return linestrings.GetPolygons();
        }
        public static List<Polygon> GetPolygons(this List<LineSegment> lineSegments, LineString rest )
        {
            return lineSegments.GetPolygons(new List<LineString> { rest });
        }

        public static LineSegment Move(this LineSegment lineSegment,double distance)
        {
            var newlineSeg = new LineSegment();
            var P0 = lineSegment.P0;
            var P1 = lineSegment.P1;
            if (lineSegment.IsVertical())
            {
                newlineSeg.P0 = new Coordinate(P0.X + distance, P0.Y);
                newlineSeg.P1 = new Coordinate(P1.X + distance, P1.Y);
            }
            else
            {
                newlineSeg.P0 = new Coordinate(P0.X , P0.Y + distance);
                newlineSeg.P1 = new Coordinate(P1.X , P1.Y + distance);
            }
            return newlineSeg;
        }
        public static LineSegment GetVaildPart(this LineSegment lineSegment,Polygon subArea)
        {
            double tol = 0.1;
            var pts = subArea.Coordinates.Where(x => lineSegment.Distance(x) < tol).OrderBy(x => x.X +x.Y);
            if (pts.Count() ==0) return null;
            return new LineSegment(pts.First(), pts.Last());
        }

        public static bool AlmostEqual(this LineSegment l1, LineSegment l2,double tol = 1)
        {
            if (l1.P0.Distance(l2.P0) < tol && l1.P1.Distance(l2.P1) < tol) return true;
            if (l1.P0.Distance(l2.P1) < tol && l1.P1.Distance(l2.P0) < tol) return true;
            return false;
        }
        public static List<LineSegment> RemoveDuplicated(this List<LineSegment> lines ,double tol = 1)
        {
            var NonDuplicated = new List<int>();
            for(int i = 0; i < lines.Count; i++) NonDuplicated.Add(i);
            var DuplicatedGroup = new List<HashSet<int>>();
            while (true)
            {
                var Duplicated = new HashSet<int>();
                for (int i = 0; i < NonDuplicated.Count ; i++)
                {
                    var idx_i = NonDuplicated[i];
                    for (int j = 0; j < NonDuplicated.Count; j++)
                    {
                        var idx_j = NonDuplicated[j];
                        if(i == j) continue;
                        if(lines[idx_i].AlmostEqual(lines[idx_j],tol))
                        {
                            Duplicated.Add(idx_i);
                            Duplicated.Add(idx_j);
                        }
                    }
                    if (Duplicated.Count > 0) break;
                }
                if (Duplicated.Count == 0) break;
                else
                {
                    DuplicatedGroup.Add(Duplicated);
                    Duplicated.ToList().ForEach(idx => NonDuplicated.Remove(idx));
                }
            }
            var result = new List<LineSegment>();
            NonDuplicated.ForEach(idx => result.Add(lines[idx]));
            foreach(var group in DuplicatedGroup)
            {
                var longestIdx = group.OrderBy(idx => lines[idx].Length).Last();
                result.Add(lines[longestIdx]);
            }
            return result;
        }

        public static List<LineSegment> Merge(this List<LineSegment> lines,double tol = 1)
        {
            bool Finished = false;
            while (!Finished)
            {
                Finished = true;
                foreach(LineSegment line in lines)
                {
                    var lineToMerge = lines.Where(l => !l.Equals(line) && (l.IsVertical() == line.IsVertical()) && (l.Distance(line) < tol)).ToList();
                    if (lineToMerge.Count() != 0)
                    {
                        var pts = new List<Coordinate> { line.P0,line.P1 };
                        lineToMerge.ForEach(l => { pts.Add(l.P0); pts.Add(l.P1); });
                        var ordedpts = pts.OrderBy(coor => coor.X + coor.Y);
                        var mergedLine = new LineSegment(ordedpts.First(), ordedpts.Last());
                        lines.Remove(line);
                        lineToMerge.ForEach(l => lines.Remove(l));
                        lines.Add(mergedLine);
                        Finished = false;
                        break;
                    }
                }
            }

            return lines;
        }
        public static (LineSegment, LineSegment) Split(this LineSegment line,Coordinate coordinate)
        {
            var coors = new List<Coordinate> { line.P0,line.P1,coordinate}.OrderBy(c => c.X+c.Y).ToArray();

            if (line.IsVertical())
            {
                var X = line.P0.X;
                var l1 = new LineSegment(X, coors[0].Y, X, coors[1].Y);
                var l2 = new LineSegment(X, coors[1].Y, X, coors[2].Y);
                return (l1, l2);
            }
            else
            {
                var Y = line.P0.Y;
                var l1 = new LineSegment( coors[0].X,Y, coors[1].X,Y);
                var l2 = new LineSegment( coors[1].X,Y, coors[2].X,Y);
                return (l1, l2);
            }
        }

        public static LineSegment Extend(this LineSegment line,double distance)
        {
            if (line.IsVertical())
            {
                var X = line.P0.X;
                var minY = Math.Min(line.P0.Y, line.P1.Y);
                var maxY = Math.Max(line.P0.Y, line.P1.Y);
                return new LineSegment(X,minY-distance,X,maxY + distance);
            }
            else
            {
                var Y = line.P0.Y;
                var minX = Math.Min(line.P0.X, line.P1.X);
                var maxX = Math.Max(line.P0.X, line.P1.X);
                return new LineSegment( minX - distance,Y, maxX + distance,Y);
            }
        }

        public static List<Coordinate> LineIntersection(this LineSegment line,LineString lstr)
        {
            double distance;
            if (line.IsVertical())
            {
                var ordered = lstr.Coordinates.OrderBy(c => c.X);
                distance = ordered.Last().X - ordered.First().X;
            }
            else
            {
                var ordered = lstr.Coordinates.OrderBy(c => c.Y);
                distance = ordered.Last().Y - ordered.First().Y;
            }
            distance +=  line.GetLineString().Distance(lstr) + 1;
            var extended = line.Extend(distance);
            return extended.GetLineString().Intersection(lstr).Coordinates.ToList();

        }

        public static List<Point> GetCrossPoints(this List<LineSegment> segLines, Polygon area)
        {
            var areas = area.Shell.GetPolygons(segLines.ToLineStrings());//区域分割
            areas = areas.Select(a => a.RemoveHoles()).ToList();//去除中空腔体
            var subAreaSPIdx = new MNTSSpatialIndex(areas);
            var IntSecPts = segLines.GetAllIntSecPs();
            //找到被4个非空区域公用的pt
            var PtToDefine = new List<Point>();
            foreach (var pt in IntSecPts)
            {
                var neighbors = subAreaSPIdx.SelectCrossingGeometry(pt.Buffer(1));
                if (neighbors.Count == 4)
                {
                    PtToDefine.Add(pt);
                }
            }
            return PtToDefine;
        }
    }
}
