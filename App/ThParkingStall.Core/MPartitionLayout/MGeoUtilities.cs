using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThParkingStall.Core.MPartitionLayout
{
    public static class MGeoUtilities
    {
        public static Vector2D Vector(LineSegment line, bool unitization = false)
        {
            Vector2D vec = new Vector2D(line.P0, line.P1);
            if (unitization) vec.Normalize();
            vec = new Vector2D(vec.X, vec.Y);
            return vec;
        }
        public static bool IsHorizontalLine(LineSegment line, double degreetol = 1)
        {
            double angle = Math.Abs(Vector(line).AngleTo(Vector2D.Create(0, 1)));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }
        public static bool IsVerticalLine(LineSegment line, double degreetol = 1)
        {
            double angle = Math.Abs(Vector(line).AngleTo(Vector2D.Create(1, 0)));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }
        public static bool IsPerpVector(Vector2D a, Vector2D b, double degreetol = 1)
        {
            double angle = Math.Abs(a.AngleTo(b));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }
        public static bool IsParallelLine(LineSegment a, LineSegment b, double degreetol = 1)
        {
            double angle = Math.Abs(Vector(a).AngleTo(Vector(b)));
            return Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < degreetol;
        }
        public static bool IsPerpLine(LineSegment a, LineSegment b, double degreetol = 1)
        {
            double angle = Math.Abs(Vector(a).AngleTo(Vector(b)));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }
        public static List<LineString> JoinCurves(List<LineString> plys, List<LineSegment> lines, double tol = 1)
        {
            List<LineString> pls = new List<LineString>();
            pls.AddRange(plys);
            lines.ForEach(e => pls.Add(e.ToLineString()));
            List<LineString> result = new List<LineString>();
            if (pls.Count == 0) return result;
            result.Add(pls[0]);
            pls.RemoveAt(0);
            while (pls.Count > 0)
            {
                bool quit = false;
                for (int i = 0; i < pls.Count; i++)
                {
                    Coordinate pe = result[result.Count - 1].EndPoint.Coordinate;

                    if (pls[i].ClosestPoint(pe).Distance(pe) <= tol)
                    {
                        if (pls[i].EndPoint.Coordinate.Distance(pe) <= tol) pls[i] = new LineString(pls[i].Coordinates.Reverse().ToArray());
                        result[result.Count - 1] = result[result.Count - 1].PolyJoin(pls[i]);
                        pls.RemoveAt(i);
                        quit = true;
                        break;
                    }
                    Coordinate ps = result[result.Count - 1].StartPoint.Coordinate;
                    if (pls[i].ClosestPoint(ps).Distance(ps) <= tol)
                    {
                        if (pls[i].StartPoint.Coordinate.Distance(ps) <= tol) pls[i] = new LineString(pls[i].Coordinates.Reverse().ToArray());
                        result[result.Count - 1] = pls[i].PolyJoin(result[result.Count - 1]);
                        pls.RemoveAt(i);
                        quit = true;
                        break;
                    }
                }
                if (quit) continue;
                result.Add(pls[0]);
                pls.RemoveAt(0);
            }
            pls.Clear();
            return result;
        }
        public static List<LineSegment> SplitLine(LineSegment line, List<Coordinate> points)
        {
            points.Insert(0, line.P0);
            points.Add(line.P1);
            points = RemoveDuplicatePts(points);
            points = points.Where(e => line.ClosestPoint(e).Distance(e) < 0.1).ToList();
            SortAlongCurve(points, line);
            List<LineSegment> results = new List<LineSegment>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                LineSegment r = new LineSegment(points[i], points[i + 1]);
                results.Add(r);
            }
            return results;
        }
        public static LineString[] SplitCurve(LineString curve, LineString splitter)
        {
            List<Coordinate> points = new List<Coordinate>();
            points.AddRange(curve.IntersectPoint(splitter));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0 && curve.Length > 1)
            {
                var ps = points.Select(e => curve.ClosestPoint(e)).ToList();
                var splited = curve.GetSplitCurves(ps);
                return splited.Where(e => e.Length > 1).ToArray();
            }
            else
            {
                return new LineString[] { curve };
            }
        }
        public static LineString[] SplitCurve(Polygon curve, Polygon splitter)
        {
            var a = new LineString(curve.Coordinates);
            var b = new LineString(splitter.Coordinates);
            return SplitCurve(a, b);
        }
        public static List<LineSegment> SplitLine(LineSegment line, Polygon splitter)
        {
            var linestring = SplitCurve(new LineString(new List<Coordinate>() { line.P0, line.P1 }.ToArray()), new LineString(splitter.Coordinates));
            return linestring.Select(e => new LineSegment(e.StartPoint.Coordinate, e.EndPoint.Coordinate)).ToList();
        }
        public static LineSegment[] SplitLine(LineSegment curve, List<Polygon> cutters, double length_filter = 1)
        {
            List<Coordinate> points = new List<Coordinate>();
            foreach (var cutter in cutters)
                points.AddRange(curve.IntersectPoint(cutter));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0)
                return SplitLine(curve, points).Where(e => e.Length > length_filter).ToArray();
            else
                return new LineSegment[] { new LineSegment(curve) };
        }
        private static List<Coordinate> removeDuplicatePts(List<Coordinate> points, double tol = 0, bool preserve_order = true)
        {
            if (points.Count < 2) return points;
            List<Coordinate> results = new List<Coordinate>(points);
            if (preserve_order)
            {
                for (int i = 1; i < results.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (results[i].Distance(results[j]) <= tol)
                        {
                            results.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
                return results;
            }
            else
            {
                results = results.OrderBy(e => e.X).ToList();
                for (int i = 1; i < results.Count; i++)
                {
                    if (results[i].Distance(results[i - 1]) <= tol)
                    {
                        results.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                return results;
            }
        }
        public static List<Coordinate> RemoveDuplicatePts(List<Coordinate> points, double tol = 0.001, bool preserve_order = true)
        {
            while (true)
            {
                var pts = removeDuplicatePts(points, tol, preserve_order);
                if (pts.Count < points.Count) points = pts;
                else break;
            }
            return points;
        }
        public static void RemoveDuplicatedLines(List<LineSegment> lines)
        {
            if (lines.Count < 2) return;
            for (int i = 0; i < lines.Count - 1; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if ((lines[i].P0.Distance(lines[j].P0) < 1 && lines[i].P1.Distance(lines[j].P1) < 1)
                        || (lines[i].P0.Distance(lines[j].P1) < 1 && lines[i].P1.Distance(lines[j].P0) < 1))
                    {
                        lines.RemoveAt(j);
                        j--;
                    }
                }
            }
        }
        public static void SortAlongCurve(List<Coordinate> points, LineSegment curve)
        {
            var comparer = new PointAlongCurveComparer(curve);
            points.Sort(comparer);
            return;
        }
        private class PointAlongCurveComparer : IComparer<Coordinate>
        {
            public PointAlongCurveComparer(LineSegment curve)
            {
                Curve = curve;
            }
            private LineSegment Curve;
            public int Compare(Coordinate a, Coordinate b)
            {
                var param_a = 0.0;
                var param_b = 0.0;
                var pa = Curve.ClosestPoint(a);
                var pb = Curve.ClosestPoint(b);
                param_a = pa.Distance(Curve.P0);
                param_b = pb.Distance(Curve.P0);
                if (param_a == param_b) return 0;
                else if (param_a < param_b) return -1;
                else return 1;
            }
        }
        /// <summary>
        /// allow type: linesegment, linestring, polygon.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="curve"></param>
        public static void SortAlongCurve(List<Coordinate> points, Geometry curve)
        {
            LineString lstring;
            if (curve is Polygon)
                lstring = new LineString(((Polygon)curve).Coordinates);
            else if (curve is LineString) lstring = (LineString)curve;
            else return;
            var comparer = new PointAlongLineStringComparer(lstring);
            points.Sort(comparer);
            return;
        }
        private class PointAlongLineStringComparer : IComparer<Coordinate>
        {
            public PointAlongLineStringComparer(LineString curve)
            {
                Curve = curve;
            }
            private LineString Curve;
            public int Compare(Coordinate a, Coordinate b)
            {
                var pa = Curve.ClosestPoint(a);
                var pb = Curve.ClosestPoint(b);
                int indexa = 0;
                int indexb = 0;
                for (int i = 0; i < Curve.Coordinates.Count() - 1; i++)
                {
                    var seg = new LineSegment(Curve.Coordinates.ToList()[i], Curve.Coordinates.ToList()[i + 1]);
                    if (seg.ClosestPoint(pa).Distance(pa) < 0.1)
                    {
                        indexa = i;
                        break;
                    }
                }
                for (int i = 0; i < Curve.Coordinates.Count() - 1; i++)
                {
                    var seg = new LineSegment(Curve.Coordinates.ToList()[i], Curve.Coordinates.ToList()[i + 1]);
                    if (seg.ClosestPoint(pb).Distance(pb) < 0.1)
                    {
                        indexb = i;
                        break;
                    }
                }
                if (indexa < indexb) return -1;
                else if (indexa > indexb) return 1;
                else
                {
                    var seg = new LineSegment(Curve.Coordinates.ToList()[indexa], Curve.Coordinates.ToList()[indexa + 1]);
                    var para_a = seg.P0.Distance(pa);
                    var para_b = seg.P0.Distance(pb);
                    if (para_a < para_b) return -1;
                    else if (para_a > para_b) return 1;
                    else return 0;
                }
            }
        }
        public static double ClosestPointInCurves(Coordinate pt, List<Polygon> crvs)
        {
            if (crvs.Count == 0) return 0;
            var p = crvs[0].ClosestPoint(pt);
            var res = p.Distance(pt);
            if (crvs.Count == 1) return res;
            for (int i = 1; i < crvs.Count; i++)
            {
                var pc = crvs[i].ClosestPoint(pt);
                var d = pc.Distance(pt);
                if (d < res)
                {
                    res = d;
                }
            }
            return res;
        }
        public static double ClosestPointInCurves(Coordinate pt, List<LineString> crvs)
        {
            if (crvs.Count == 0) return 0;
            var p = crvs[0].ClosestPoint(pt);
            var res = p.Distance(pt);
            if (crvs.Count == 1) return res;
            for (int i = 1; i < crvs.Count; i++)
            {
                var pc = crvs[i].ClosestPoint(pt);
                var d = pc.Distance(pt);
                if (d < res)
                {
                    res = d;
                }
            }
            return res;
        }
        public static double ClosestPointInVertCurves(Coordinate pt, LineSegment line, List<LineSegment> crvss)
        {
            var crvs = crvss.Where(e => IsPerpLine(line, e)).ToList();
            if (crvs.Count == 0) return 0;
            var p = crvs[0].ClosestPoint(pt);
            var res = p.Distance(pt);
            if (crvs.Count == 1) return res;
            for (int i = 1; i < crvs.Count; i++)
            {
                var pc = crvs[i].ClosestPoint(pt, false);
                var d = pc.Distance(pt);
                if (d < res)
                {
                    res = d;
                }
            }
            return res;
        }
        public static LineSegment LineSegmentSDL(Coordinate start, Vector2D direction, double length)
        {
            return new LineSegment(start, start.Translation(direction.Normalize() * length));
        }
        public static Coordinate GetPointAtDisPara(LineString line, double parameter)
        {
            Coordinate coo = line.Coordinates[0];
            for (int i = 0; i < line.Coordinates.Count() - 1; i++)
            {
                var seg = new LineSegment(line.Coordinates[i], line.Coordinates[i + 1]);
                if (parameter > seg.Length) parameter -= seg.Length;
                else
                {
                    coo = seg.P0.Translation(Vector(seg).Normalize() * parameter);
                    break;
                }
            }
            return coo;
        }
        public static Polygon PolyFromLines(LineSegment a, LineSegment b)
        {
            return new Polygon(new LinearRing(new List<Coordinate>() { a.P0, a.P1, b.P1, b.P0, a.P0 }.ToArray()));
        }
        public static Polygon PolyFromLine(LineSegment line)
        {
            return new Polygon(new LinearRing(new List<Coordinate>() { line.P0, line.P1, line.P0 }.ToArray()));
        }
        public static Polygon PolyFromPoints(List<Coordinate> points)
        {
            if (points[0].Distance(points[points.Count - 1]) > 0.001)
                points.Add(points[0]);
            return new Polygon(new LinearRing(points.ToArray()));
        }
        public static bool IsInAnyBoxes(Coordinate pt, List<Polygon> boxes, bool true_on_edge = false)
        {
            if (boxes.Count == 0) return false;
            if (true_on_edge)
            {
                if (ClosestPointInCurves(pt, boxes) < 10) return true;
            }
            for (int i=0;i< boxes.Count;i++)
            {
                if (boxes[i].Area < 1) continue;
                boxes[i] = boxes[i].Scale(0.99999);
                if (boxes[i].Contains(pt))
                {
                    boxes[i]=boxes[i].Scale(1 / 0.99999);
                    return true;
                }
                boxes[i] = boxes[i].Scale(1 / 0.99999);
            }
            return false;
        }
        public static double ClosestPointInVertLines(Coordinate pt, LineSegment line, IEnumerable<LineSegment> lines, bool returninfinity = true)
        {
            var ls = lines.Where(e => IsPerpLine(line, e));
            if (!returninfinity)
                if (ls.Count() == 0) return -1;
            var res = double.PositiveInfinity;
            foreach (var l in ls)
            {
                var dis = l.ClosestPoint(pt).Distance(pt);
                if (res > dis) res = dis;
            }
            return res;
        }
        public static bool IsInAnyPolys(Coordinate pt, List<Polygon> pls, bool allowOnEdge = false)
        {
            if (pls.Count == 0) return false;
            var ps = pls.Where(e => e.Area > 1).OrderBy(e => e.ClosestPoint(pt).Distance(pt));
            if (!allowOnEdge)
            {
                foreach (var p in ps)
                {
                    if (p.Coordinates.Count() == 5)
                        if (p.Envelope.Contains(new Point(pt)) && p.ClosestPoint(pt).Distance(pt) > 10) return true;
                    if (p.Contains(pt) && p.ClosestPoint(pt).Distance(pt) > 10) return true;
                }
            }
            else
            {
                foreach (var p in ps)
                {
                    if (p.Coordinates.Count() == 5)
                        if (p.Envelope.Contains(new Point(pt))) return true;
                    if (p.Contains(pt)) return true;
                }
            }
            return false;
        }
        public static double GetCommonLengthForTwoParallelLinesOnPerpDirection(LineSegment a, LineSegment b)
        {
            var project_a = new LineSegment(b.ClosestPoint(a.P0), b.ClosestPoint(a.P1));
            var buffer = project_a.Buffer(1);
            var splits = SplitLine(b, buffer).Where(e => buffer.Contains(e.MidPoint)).ToList();
            var length = 0.0;
            splits.ForEach(e => length += e.Length);
            return length;
        }
        public static bool IsPointInFast(this Polygon poly, Coordinate p)
        {
            return poly.Contains(new Point(p));
        }
        public static Coordinate AveragePoint(Coordinate a, Coordinate b)
        {
            return new Coordinate((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        }
        public static Polygon RemoveDuplicatedPointOnPolygon(Polygon polygon)
        {
            List<Coordinate> points = polygon.Coordinates.ToList();
            if (points.Count < 2) return polygon;
            for (int i = 1; i < points.Count - 1; i++)
            {
                if (points[i].Distance(points[i - 1]) < 0.001)
                {
                    points.RemoveAt(i);
                    i--;
                }
            }
            if (points[points.Count - 2].Distance(points[points.Count - 1]) < 0.001)
                points.RemoveAt(points.Count - 2);
            if (points.Count == polygon.Coordinates.Count()) return polygon;
            else
            {
                if(points[0].Distance(points[points.Count-1])>0.001)points.Add(points[0]);
                if (points.Count == 1) return new Polygon(new LinearRing(new Coordinate[0]));
                return new Polygon(new LinearRing(points.ToArray()));
            }
        }
        public static double ClosestPointInCurvesFast(Coordinate pt, List<LineString> crvs)
        {
            var pl = crvs.OrderBy(t => t.ClosestPoint(pt).Distance(pt)).First();
            return pl.ClosestPoint(pt).Distance(pt);
        }
        public static LineSegment ChangeLineToBeOrthogonal(LineSegment line)
        {
            double distx = Math.Abs(line.P0.X - line.P1.X);
            double disty = Math.Abs(line.P0.Y - line.P1.Y);
            double averx = (line.P0.X + line.P1.X) / 2;
            double avery = (line.P0.Y + line.P1.Y) / 2;
            if (distx >= disty)
                return new LineSegment(new Coordinate(line.P0.X, avery), new Coordinate(line.P1.X, avery));
            else
                return new LineSegment(new Coordinate(averx, line.P0.Y), new Coordinate(averx, line.P1.Y));
        }
        public static List<Coordinate> DivideCurveByLength(LineSegment crv, double length, ref List<LineSegment> segs)
        {
            var pts = crv.GetPointsByDist(length).ToList();
            pts.Add(crv.P1);
            pts = RemoveDuplicatePts(pts);
            segs = SplitLine(crv, pts);
            if (segs.Count == 0) segs.Add(crv);
            return pts;
        }
        public static List<Coordinate> DivideCurveByKindsOfLength(LineSegment crv, ref List<LineSegment> segs,
        double length_a, int count_a, double length_b, int count_b,
        double length_c, int count_c, double length_d, int count_d)
        {
            List<Coordinate> pts = new List<Coordinate>();
            pts.Add(crv.P0);
            double t = 0;
            bool quit = false;
            while (true)
            {
                for (int i = 0; i < count_a + count_b + count_c + count_d; i++)
                {
                    if (i < count_a) t += length_a;
                    else if (i < count_a + count_b) t += length_b;
                    else if (i < count_a + count_b + count_c) t += length_c;
                    else t += length_d;
                    if (t < crv.Length) pts.Add(GetPointAtDisPara(crv.ToLineString(),t));
                    else
                    {
                        quit = true;
                        break;
                    }
                }
                if (quit) break;
            }
            pts.Add(crv.P1);
            segs = SplitLine(crv, pts);
            if (segs.Count == 0) segs.Add(crv);
            return pts;
        }
        public static void AddToSpatialIndex(Geometry e, ref MNTSSpatialIndex spatialIndex)
        {
            spatialIndex.Update(new List<Geometry>() { e}, new List<Geometry>());
            return;
        }
        public static bool ClosestPointInCurveInAllowDistance(Coordinate pt, List<Polygon> crvs, double distance)
        {
            foreach (var t in crvs)
            {
                if (t.ClosestPoint(pt).Distance(pt) < distance) return true;
            }
            return false;
        }
        public static bool IsConnectedLines(LineSegment a, LineSegment b)
        {
            if (a.P0.Distance(b.P0) < 1 || a.P0.Distance(b.P1) < 1
                || a.P1.Distance(b.P0) < 1 || a.P1.Distance(b.P1) < 1) return true;
            else return false;
        }

    }
}
