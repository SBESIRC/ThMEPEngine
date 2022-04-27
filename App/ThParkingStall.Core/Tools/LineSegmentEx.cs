using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace ThParkingStall.Core.Tools
{
    public static class LineSegmentEx
    {
        public static LineString ToLineString(this LineSegment line)
        {
            var coors = new Coordinate[] { line.P0.Copy(), line.P1.Copy()};
            return new LineString(coors);
        }
        public static List<LineString> ToLineStrings(this List<LineSegment> lines)
        {
            var LineStrings = new List<LineString>();
            lines.ForEach(line => LineStrings.Add(line.ToLineString()));
            return LineStrings;
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
            lineSegments.ForEach(line => linestrings.Add(line.ToLineString()));
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

        public static LineSegment GetVaildPart(this LineSegment lineSegment,Polygon subArea)
        {
            double tol = 0.1;
            var pts = subArea.Coordinates.Where(x => lineSegment.Distance(x) < tol).OrderBy(x => x.X +x.Y);
            if (pts.Count() ==0) return null;
            return new LineSegment(pts.First(), pts.Last());
        }
    }
}
