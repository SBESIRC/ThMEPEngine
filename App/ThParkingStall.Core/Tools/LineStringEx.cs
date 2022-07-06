using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Mathematics;
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
        public static List<Coordinate> ToCoordinates(this LineString linestring, double distance)
        {
            var coors = new List<Coordinate>();
            for (int i = 0; i < linestring.Coordinates.Count() - 1; i++)
            {
                var coor_Start = linestring.Coordinates[i];
                var coor_End = linestring.Coordinates[i + 1];
                //var disToEnd = coor_Start.Distance(coor_End);
                var vector = new Vector2D(coor_Start, coor_End).Normalize();
                while (true)
                {
                    coors.Add(coor_Start);
                    coor_Start = vector.Multiply(distance).Translate(coor_Start);
                    var vec_new = new Vector2D(coor_Start, coor_End).Normalize();
                    if (Math.Abs(vec_new.Angle() - vector.Angle()) > 0.1) break;
                }
            }
            return coors;
        }

        //钝化所有锐角 + simplyfy(移除相连的平行线),该函数尚未完全验证
        // inner 钝化内部锐角
        public static LinearRing Obtusify(this LinearRing Linearring,bool inner, double tolerance = 0.1)
        {
            bool findCW = Linearring.IsCCW &&inner || !Linearring.IsCCW && !inner;
            var Coordinates = Linearring.CoordinateSequence.ToCoordinateArray().ToList();
            Coordinates.RemoveAt(Coordinates.Count - 1);//删除最后一个
            Coordinate coor_pre;//前一个点
            Coordinate coor_next;//后一个点
            var NewCoordinates = new List<Coordinate> ();//新的钝化列表
            coor_pre = Coordinates[Coordinates.Count - 1];
            for (int i = 0; i < Coordinates.Count; i++)//遍历所有顶点
            {
                if(NewCoordinates.Count !=0) coor_pre = NewCoordinates.Last();
                if (i == Coordinates.Count - 1)
                {
                    if (NewCoordinates.Count == 0) break;
                    coor_next = NewCoordinates.First();
                }
                else coor_next = Coordinates[i + 1];
                if (coor_pre.Equals(Coordinates[i])) continue;//与前一个点相同 跳过
                var vecA = new Vector2D(coor_pre, Coordinates[i]);
                var vecB = new Vector2D(Coordinates[i], coor_next);
                if (vecA.IsParallel(vecB)) continue;//角的度数为0或pi 当前点可忽略
                bool OnCWSide = vecB.IsOnClockWiseSideOf(vecA);
                bool IsTheVecToFind = (findCW && OnCWSide) || (!findCW && !OnCWSide);
                if (IsTheVecToFind && AngleUtility.IsAcute(coor_pre, Coordinates[i], coor_next) )//锐角 切分锐角至两个钝角
                {
                    var dist1 = Coordinates[i].Distance(coor_pre);//当前点与前一个点的距离
                    var dist2 = Coordinates[i].Distance(coor_next);//当前点与后一个点的距离
                    var mindist = Math.Min(dist1, dist2);//较近的距离
                    mindist = Math.Min(mindist,tolerance);//与容差相比最近的距离
                    if (mindist < dist1)
                    {
                        var vec1 = new Vector2D(Coordinates[i], coor_pre).Normalize();
                        var coor1 = vec1.Multiply(mindist).Translate(Coordinates[i]);//添加节点，当前点向上一个点平移
                        NewCoordinates.Add(coor1);
                    }
                    if(mindist < dist2)
                    {
                        var vec2 = new Vector2D(Coordinates[i], coor_next).Normalize();
                        var coor2 = vec2.Multiply(mindist).Translate(Coordinates[i]);//添加节点，当前点向下一个点平移
                        NewCoordinates.Add(coor2);
                    }
                }
                else NewCoordinates.Add(Coordinates[i]);//添加当前点
            }
            if (NewCoordinates.Count <3) NewCoordinates.Clear();//返回空的linearRing
            else NewCoordinates.Add(NewCoordinates.First());
            return new LinearRing(NewCoordinates.ToArray());
        }

        public static bool IsOnClockWiseSideOf(this Vector2D b, Vector2D a)
        {
            var dot = a.X * -b.Y + a.Y * b.X;
            if (dot > 0) return true;
            else return false;
        }
    }
}
