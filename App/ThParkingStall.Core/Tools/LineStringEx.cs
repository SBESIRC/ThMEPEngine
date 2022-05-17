using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
namespace ThParkingStall.Core.Tools
{
    public static class LineStringEx
    {
        public static List<LineSegment> ToLineSegments(this LineString lstr)
        {
            var LineSegments = new List<LineSegment>();
            for(int i = 0;i<lstr.Coordinates.Count() -1;i++)
            {
                var coor1 = lstr.Coordinates[i].Copy();
                var coor2 = lstr.Coordinates[i+1].Copy();
                LineSegments.Add(new LineSegment(coor1, coor2));
            }
            return LineSegments;
        }
        public static List<LineString> ToLineStrings(this LineString lstr)
        {
            var LineStrings = new List<LineString>();
            for (int i = 0; i < lstr.Coordinates.Count() - 1; i++)
            {
                var coor1 = lstr.Coordinates[i].Copy();
                var coor2 = lstr.Coordinates[i + 1].Copy();
                var coors = new Coordinate[] { coor1, coor2 };
                LineStrings.Add(new LineString(coors));
            }
            return LineStrings;
        }
        public static List<LineSegment> ToLineSegments(this IEnumerable<LineString> lstr)
        {
            var LineSegments = new List<LineSegment>();
            foreach (LineString l in lstr) LineSegments.AddRange(l.ToLineSegments());
            return LineSegments;
        }
        public static List<Point> GetIntersectPts(this LineString lstr,LineSegment line)
        {
            var pts = new HashSet<Coordinate>();
            var lines = lstr.ToLineSegments();
            foreach(var l in lines)
            {
                var IntSecPt = line.Intersection(l);
                if (IntSecPt != null) pts.Add(IntSecPt);
            }
            var res = new List<Point>();
            foreach(var coor in pts)
            {
                res.Add(new Point(coor));
            }
            return res;
        }

        public static List<Point> GetIntersectPts(this LineString lstr1, LineString lstr2)
        {
            var pts = new HashSet<Coordinate>();
            var lines1 = lstr1.ToLineSegments();
            var lines2 = lstr2.ToLineSegments();
            for(int i= 0; i < lines1.Count; i++)
            {
                for(int j = i;j<lines2.Count;j++)
                {
                    if (i == j) continue;
                    var IntSecPt = lines1[i].Intersection(lines2[j]);
                    if (IntSecPt != null) pts.Add(IntSecPt);
                }
            }
            var res = new List<Point>();
            foreach (var coor in pts)
            {
                res.Add(new Point(coor));
            }
            return res;
        }
        //public static bool PartInCommon(this LineString lstr1, LineString lstr2)
        //{
        //    if(lstr1 == null || lstr2 == null) return false;
        //    var intSection = lstr1.Intersection(lstr2);
        //    if (intSection.Length >0 ) return true;
        //    else return false;
        //}
        public static bool PartInCommon(this LineString lstr1, Geometry geo)
        {
            if (lstr1 == null || geo == null) return false;
            var intSection = lstr1.Intersection(geo);
            if (intSection.Length > 0) return true;
            else return false;
        }

        public static bool IsVertical(this LineString lstr)
        {
            if (lstr.Coordinates.Count() != 2) throw new Exception("Not supported");
            if (Math.Abs(lstr[0].X - lstr[1].X) < 1e-4) return true;
            else return false;
        }
        public static List<LineSegment> GetVaildParts(this IEnumerable<LineString> lstrs,Polygon area)
        {
            var VaildParts = new List<LineSegment>();
            foreach(var lstr in lstrs)
            {
                var intSection = lstr.Intersection(area.Shell);
                if (intSection.Length > 0)
                {
                    var pts = intSection.Coordinates.OrderBy(coor => coor.X + coor.Y);
                    VaildParts.Add(new LineSegment(pts.First(), pts.Last()));
                }
            }
            return VaildParts;
        }

        public static List<LineString> GetVaildLstrs(this IEnumerable<LineString> lstrs, Polygon area)
        {
            var VaildParts = new List<LineString>();
            foreach (var lstr in lstrs)
            {
                var intSection = lstr.Intersection(area.Shell);
                if (intSection.Length > 0)
                {
                    var pts = intSection.Coordinates.OrderBy(coor => coor.X + coor.Y);
                    var coors = new Coordinate[] { pts.First(), pts.Last() };
                    VaildParts.Add(new LineString(coors));
                }
            }
            return VaildParts;
        }
        // 合并一堆linestring
        public static Geometry Union(this List<LineString> linestrings)
        {
            // UnaryUnionOp.Union()有Robust issue
            // 会抛出"non-noded intersection" TopologyException
            // OverlayNGRobust.Union()在某些情况下仍然会抛出TopologyException (NTS 2.2.0)
            var lineStrSet = linestrings.ToHashSet();//去重
            var firstOne = lineStrSet.First();
            lineStrSet.Remove(firstOne);
            var multiLineStrings = new MultiLineString(lineStrSet.ToArray());
            return OverlayNGRobust.Overlay(firstOne, multiLineStrings, SpatialFunction.Union);
        }
        public static List<Geometry> Difference(this LineString linestring, IEnumerable<LineString> linestrings)
        {
            LineString result = linestring.Copy() as LineString;
            List<Geometry> res = new List<Geometry>();
            foreach (var linestring2 in linestrings)
            {
                res.Add( OverlayNGRobust.Overlay(result, linestring2, SpatialFunction.Difference));
            }
            return res;
        }
        public static List<Polygon> GetPolygons(this List<LineString> linestrings)
        {
            var LSTR_Union = linestrings.Union();
            var geos = LSTR_Union.Polygonize();
            return geos.ToList();
        }
        public static List<Polygon> GetPolygons(this LineString linestring, IEnumerable<LineString> others)
        {
            var list = new List<LineString> { linestring };
            list.AddRange(others);
            return list.GetPolygons();
        }
    }
}
