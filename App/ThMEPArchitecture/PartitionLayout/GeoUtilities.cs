using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;

namespace ThMEPArchitecture.PartitionLayout
{
    public static class GeoUtilities
    {
        public static bool IsConnectedLines(Line a, Line b)
        {
            if (a.StartPoint.DistanceTo(b.StartPoint) < 1 || a.StartPoint.DistanceTo(b.EndPoint) < 1
                || a.EndPoint.DistanceTo(b.StartPoint) < 1 || a.EndPoint.DistanceTo(b.EndPoint) < 1) return true;
            else return false;
        }

        public static double GetCommonLengthForTwoParallelLinesOnPerpDirection(Line a, Line b)
        {
            var project_a = new Line(b.GetClosestPointTo(a.StartPoint, true), b.GetClosestPointTo(a.EndPoint, true));
            var buffer = project_a.Buffer(1);
            var splits = SplitLine(b, buffer).Where(e => buffer.Contains(e.GetCenter()));
            var length = 0.0;
            splits.ForEach(e => length += e.Length);
            return length;
        }

        public static void RemoveDuplicatedLines(List<Line> lines)
        {
            if (lines.Count < 2) return;
            for (int i = 0; i < lines.Count - 1; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if ((lines[i].StartPoint.DistanceTo(lines[j].StartPoint) < 1 && lines[i].EndPoint.DistanceTo(lines[j].EndPoint) < 1)
                        || (lines[i].StartPoint.DistanceTo(lines[j].EndPoint) < 1 && lines[i].EndPoint.DistanceTo(lines[j].StartPoint) < 1))
                    {
                        lines.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

        public static Point3d AveragePoint(Point3d a, Point3d b)
        {
            return new Point3d((a.X + b.X) / 2, ((a.Y + b.Y) / 2), (a.Z + b.Z) / 2);
        }

        public static Point3d GetRecCentroid(this Polyline rec)
        {
            var ext = rec.GeometricExtents;
            var min = ext.MinPoint;
            var max = ext.MaxPoint;
            return new Point3d((min.X + max.X) / 2, (min.Y + max.Y) / 2, 0);
        }

        public static Line ChangeLineToBeOrthogonal(Line line)
        {
            double distx = Math.Abs(line.StartPoint.X - line.EndPoint.X);
            double disty = Math.Abs(line.StartPoint.Y - line.EndPoint.Y);
            double averx = (line.StartPoint.X + line.EndPoint.X) / 2;
            double avery = (line.StartPoint.Y + line.EndPoint.Y) / 2;
            if (distx >= disty)
                return new Line(new Point3d(line.StartPoint.X, avery, 0), new Point3d(line.EndPoint.X, avery, 0));
            else
                return new Line(new Point3d(averx, line.StartPoint.Y, 0), new Point3d(averx, line.EndPoint.Y, 0));
        }

        public static List<Point3d> RemoveDuplicatePts(List<Point3d> points, double tol = 0, bool preserve_order = true)
        {
            if (points.Count < 2) return points;
            List<Point3d> results = new List<Point3d>(points);
            if (preserve_order)
            {
                for (int i = 1; i < results.Count; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (results[i].DistanceTo(results[j]) <= tol)
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
                    if (results[i].DistanceTo(results[i - 1]) <= tol)
                    {
                        results.RemoveAt(i);
                        i--;
                        continue;
                    }
                }
                return results;
            }
        }

        public static void SortAlongCurve(List<Point3d> points, Curve curve)
        {
            var comparer = new PointAlongCurveComparer(curve);
            points.Sort(comparer);
            return;
        }

        private class PointAlongCurveComparer : IComparer<Point3d>
        {
            public PointAlongCurveComparer(Curve curve)
            {
                Curve = curve;
            }
            private Curve Curve;
            public int Compare(Point3d a, Point3d b)
            {
                var param_a = 0.0;
                var param_b = 0.0;
                if (Curve is Line)
                {
                    var line = (Line)Curve;
                    var pa = line.GetClosestPointTo(a, false);
                    var pb = line.GetClosestPointTo(b, false);
                    param_a = pa.DistanceTo(line.StartPoint);
                    param_b = pb.DistanceTo(line.StartPoint);
                }
                else if (Curve is Polyline)
                {
                    var pl = (Polyline)Curve;
                    param_a = GetDisOnPolyLine(a, pl);
                    param_b = GetDisOnPolyLine(b, pl);
                }
                else
                {
                    try
                    {
                        param_a = Curve.GetDistAtPointX(a);
                        param_b = Curve.GetDistAtPointX(b);
                    }
                    catch
                    {
                        //The func of GetDistAtPointX is unstable.
                    }
                }
                if (param_a == param_b) return 0;
                else if (param_a < param_b) return -1;
                else return 1;
            }
        }

        public static Polyline CreatePolyFromLine(Line a)
        {
            Polyline p = new Polyline();
            p.AddVertexAt(0, a.StartPoint.ToPoint2d(), 0, 0, 0);
            p.AddVertexAt(1, a.EndPoint.ToPoint2d(), 0, 0, 0);
            return p;
        }

        public static Line CreateLineFromStartPtAndVector(Point3d start, Vector3d vec, double length)
        {
            var v = vec.GetNormal() * length;
            var pe = start.TransformBy(Matrix3d.Displacement(v));
            return new Line(start, pe);
        }

        public static Vector3d CreateVector(Line line)
        {
            return CreateVector(line.StartPoint, line.EndPoint);
        }

        public static Vector3d CreateVector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }

        public static Line CreateLine(Line a)
        {
            return new Line(a.StartPoint, a.EndPoint);
        }

        public static Polyline CreatePolyFromPoint(Point3d point)
        {
            Polyline p = new Polyline();
            p.AddVertexAt(0, point.ToPoint2d(), 0, 0, 0);
            return p;
        }

        public static Polyline CreatePolyFromPoints(Point3d[] points, bool closed = true)
        {
            Polyline p = new Polyline();
            for (int i = 0; i < points.Length; i++)
            {
                p.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
            }
            p.Closed = closed;
            return p;
        }

        public static Polyline CreatPolyFromLines(Line a, Line b, bool closed = true)
        {
            Polyline p = new Polyline();
            p.AddVertexAt(0, a.StartPoint.ToPoint2d(), 0, 0, 0);
            p.AddVertexAt(1, a.EndPoint.ToPoint2d(), 0, 0, 0);
            p.AddVertexAt(2, b.EndPoint.ToPoint2d(), 0, 0, 0);
            p.AddVertexAt(3, b.StartPoint.ToPoint2d(), 0, 0, 0);
            p.Closed = closed;
            return p;
        }

        public static List<Polyline> JoinCurves(List<Polyline> plys, List<Line> lines, double tol = 1)
        {
            List<Polyline> pls = new List<Polyline>();
            pls.AddRange(plys);
            lines.ForEach(e => pls.Add(CreatePolyFromLine(e)));
            List<Polyline> result = new List<Polyline>();
            if (pls.Count == 0) return result;
            result.Add(pls[0]);
            pls.RemoveAt(0);
            while (pls.Count > 0)
            {
                bool quit = false;
                for (int i = 0; i < pls.Count; i++)
                {
                    Point3d pe = result[result.Count - 1].EndPoint;
                    if (pls[i].GetClosestPointTo(pe, false).DistanceTo(pe) <= tol)
                    {
                        if (pls[i].EndPoint.DistanceTo(pe) <= tol) pls[i].ReverseCurve();
                        result[result.Count - 1] = result[result.Count - 1].PolyJoin(pls[i]);
                        pls.RemoveAt(i);
                        quit = true;
                        break;
                    }
                    Point3d ps = result[result.Count - 1].StartPoint;
                    if (pls[i].GetClosestPointTo(ps, false).DistanceTo(ps) <= tol)
                    {
                        if (pls[i].StartPoint.DistanceTo(ps) <= tol) pls[i].ReverseCurve();
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
            pls.ForEach(pl => pl.Dispose());
            pls.Clear();
            return result;
        }

        public static List<Curve> SplitCurve(Curve curve, DBObjectCollection objs)
        {
            List<Point3d> pts = new List<Point3d>();
            foreach (var e in objs.Cast<Entity>().ToList())
            {
                if (e != null)
                    pts.AddRange(curve.Intersect(e, Intersect.OnBothOperands));
            }
            pts = RemoveDuplicatePts(pts, 1);
            for (int i = 0; i < pts.Count; i++)
            {
                if (curve.GetClosestPointTo(pts[i], false).DistanceTo(pts[i]) > 1)
                {
                    pts.RemoveAt(i);
                    i--;
                }
            }
            if (pts.Count > 0 && curve.GetLength()>1)
            {
                SortAlongCurve(pts, curve);
                Point3dCollection ps = new Point3dCollection(pts.Select(e => curve.GetClosestPointTo(e, false)).ToArray());
                var splited = new DBObjectCollection();
                try
                {
                    splited = curve.GetSplitCurves(ps);
                }
                catch
                {
                }
                return splited.Cast<Curve>().Where(e => e.GetLength() > 1).ToList();
            }
            else return new List<Curve>() { curve.Clone() as Curve };
        }

        public static Curve[] SplitCurve(Curve curve, Curve splitter)
        {
            List<Point3d> points = new List<Point3d>();
            points.AddRange(curve.Intersect(splitter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0 && curve.GetLength()>1)
            {
                Point3dCollection ps = new Point3dCollection(points.Select(e => curve.GetClosestPointTo(e, false)).ToArray());
                var splited = curve.GetSplitCurves(ps);
                ps.Dispose();
                return splited.Cast<Curve>().Where(e => e.GetLength()>1).ToArray();
            }
            else
            {
                return new Curve[] { curve };
            }
        }     

        public static bool IsParallelLine(Line a, Line b, double degreetol = 1)
        {
            double angle = CreateVector((Line)a).GetAngleTo(CreateVector((Line)b));
            return Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < degreetol;
        }

        public static List<Curve> SplitCurve(Curve curve, List<Point3d> points)
        {
            List<Curve> results = new List<Curve>() { curve };
            SortAlongCurve(points, curve);
            if (curve is Line)
            {
                return SplitLine((Line)curve, points).Cast<Curve>().ToList();
            }
            else if (curve is Polyline)
            {
                List<Polyline> plys = new List<Polyline>();
                var pl = (Polyline)curve;
                var verts = pl.Vertices().Cast<Point3d>().ToList();
                var param = verts.Select(e => GetDisOnPolyLine(e, pl)).ToList();
                points.Insert(0, verts.First());
                points.Add(verts.Last());
                points = RemoveDuplicatePts(points,1);
                points=points.Distinct().ToList();
                SortAlongCurve(points, pl);
                param.RemoveAt(0);
                param.RemoveAt(param.Count - 1);
                verts.RemoveAt(0);
                verts.RemoveAt(verts.Count - 1);
                if(verts.Count==0) return SplitLine(new Line(curve.StartPoint,curve.EndPoint), points).Cast<Curve>().ToList();
                var curparam = points.Select(e => /*pl.GetParamAtPointX(e)*/GetDisOnPolyLine(e,pl)).ToList();
                for (int i = 0; i < curparam.Count - 1; i++)
                {
                    Polyline p = new Polyline();
                    p.AddVertexAt(0, points[i].ToPoint2D(), 0, 0, 0);
                    int index = 1;
                    bool quit = false;
                    for (int j = 0; j < param.Count; j++)
                    {
                        if (param[j] > curparam[i] && param[j] < curparam[i + 1])
                        {
                            p.AddVertexAt(index, verts[j].ToPoint2D(), 0, 0, 0);
                            index++;
                        }
                        else
                        {
                            p.AddVertexAt(index, points[i + 1].ToPoint2D(), 0, 0, 0);
                            plys.Add(p);
                            quit = true;
                            break;
                        }
                    }
                    if (quit)
                    {
                        if (points.Count > 0)
                        {
                            points.RemoveAt(0);
                            curparam.RemoveAt(0);
                        }
                        continue;
                    }
                    p.AddVertexAt(index, points[i + 1].ToPoint2D(), 0, 0, 0);
                    plys.Add(p);
                }
                return plys.Cast<Curve>().ToList();
            }
            else
            {
                return results;
            }
        }

        public static List<Line> SplitLine(Line line, List<Point3d> points)
        {
            points.Insert(0, line.StartPoint);
            points.Add(line.EndPoint);
            points=RemoveDuplicatePts(points);
            points = points.Where(e => line.GetClosestPointTo(e, false).DistanceTo(e) < 0.1).ToList();
            SortAlongCurve(points, line);
            List<Line> results = new List<Line>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line r = new Line(points[i], points[i + 1]);
                results.Add(r);
            }
            return results;
        }

        public static Line[] SplitLine(Line line, Curve cutter, double length_filter = 1)
        {
            List<Point3d> points = new List<Point3d>();
            points.AddRange(line.Intersect(cutter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, line);
            if (points.Count > 0)
                return SplitLine(line, points).Where(e => e.Length > length_filter).ToArray();
            else return new Line[] { CreateLine(line) };
        }

        public static Line[] SplitLine(Line curve, List<Polyline> cutters, double length_filter = 1,
            bool allow_split_similar_car = false)
        {
            List<Point3d> points = new List<Point3d>();
            foreach (var cutter in cutters)
                points.AddRange(curve.Intersect(cutter, Intersect.OnBothOperands));
            if (allow_split_similar_car)
            {
                //在处理车道末端时候，出现一个特殊case，车位与车道之间有10容差距离，以此加强判断。
                foreach (var cutter in cutters)
                {
                    points.AddRange(cutter.Vertices().Cast<Point3d>()
                        .Where(p => curve.GetClosestPointTo(p, false).DistanceTo(p) < 20)
                        .Select(p => curve.GetClosestPointTo(p, false)));
                }
            }
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0)
                return SplitLine(curve, points).Where(e => e.Length > length_filter).ToArray();
            else
                return new Line[] { new Line(curve.StartPoint, curve.EndPoint) };
        }

        public static Line[] SplitLine(Line curve, List<Line> cutters, double length_filter = 1)
        {
            List<Point3d> points = new List<Point3d>();
            foreach (var cutter in cutters)
                points.AddRange(curve.Intersect(cutter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0)
                return SplitLine(curve, points).Where(e => e.Length > length_filter).ToArray();
            else
                return new Line[] { new Line(curve.StartPoint, curve.EndPoint) };
        }

        public static bool IsPerpLine(Line a, Line b, double degreetol = 1)
        {
            double angle = CreateVector((Line)a).GetAngleTo(CreateVector((Line)b));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }

        public static bool IsPerpVector(Vector3d a, Vector3d b, double degreetol = 1)
        {
            double angle = a.GetAngleTo(b);
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }

        public static bool IsPointInFast(this Polyline poly, Point3d p)
        {
            return poly.IsPointIn(p);
            double temp = 0;
            var points = poly.Vertices();
            for (int i = 0; i < points.Count; i++)
            {
                int j = (i < points.Count - 1) ? (i + 1) : 0;
                var v1 = points[i].ToPoint2d() - p.ToPoint2d();
                var v2 = points[j].ToPoint2d() - p.ToPoint2d();
                temp += v1.MinusPiToPiAngleTo(v2);
            }
            if (Math.Abs(Math.Abs(temp) - 2 * Math.PI) < 0.1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsInAnyPolys(Point3d pt, List<Polyline> pls, bool allowOnEdge = false)
        {
            if (pls.Count == 0) return false;
            var ps = pls.Where(e => e.Area > 1).OrderBy(e => e.GetClosestPointTo(pt, false).DistanceTo(pt));
            if (!allowOnEdge)
            {
                foreach (var p in ps)
                {
                    if (p.Vertices().Count == 5)
                        if (p.GeometricExtents.IsPointIn(pt) && p.GetClosePoint(pt).DistanceTo(pt) > 10) return true;
                    if (p.Contains(pt) && p.GetClosestPointTo(pt, false).DistanceTo(pt) > 10) return true;
                }
            }
            else
            {
                foreach (var p in ps)
                {
                    if (p.Vertices().Count == 5)
                        if (p.GeometricExtents.IsPointIn(pt)) return true;
                    if (p.Contains(pt)) return true;
                }
            }
            return false;
        }

        public static bool IsInExtent(Point3d pt, Extents3d ext)
        {
            return ext.IsPointIn(pt);
        }

        public static bool IsAnyInExtent(IEnumerable<Point3d> points, Extents3d ext)
        {
            foreach (var pt in points)
                if (ext.IsPointIn(pt)) return true;
            return false;
        }

        public static bool IsInAnyBoxes(Point3d pt, List<Polyline> boxes, bool true_on_edge = false)
        {
            if (boxes.Count == 0) return false;
            if (true_on_edge)
            {
                if (ClosestPointInCurves(pt, boxes) < 10) return true;
            }
            foreach (var p in boxes)
            {
                if (p.Area < 1) continue;
                p.TransformBy(Matrix3d.Scaling(0.99999, p.GetRecCentroid()));
                if (p.Contains(pt))
                {
                    p.TransformBy(Matrix3d.Scaling(1 / 0.99999, p.GetRecCentroid()));
                    return true;
                }
                p.TransformBy(Matrix3d.Scaling(1 / 0.99999, p.GetRecCentroid()));
            }
            return false;
        }

        public static double ClosestPointInCurves(Point3d pt, List<Line> crvs)
        {
            if (crvs.Count == 0) return 0;
            var p = crvs[0].GetClosestPointTo(pt, false);
            var res = p.DistanceTo(pt);
            if (crvs.Count == 1) return res;
            for (int i = 1; i < crvs.Count; i++)
            {
                var pc = crvs[i].GetClosestPointTo(pt, false);
                var d = pc.DistanceTo(pt);
                if (d < res)
                {
                    res = d;
                }
            }
            return res;
        }
        public static double ClosestPointInVertCurves(Point3d pt, Line line, List<Line> crvss)
        {
            var crvs = crvss.Where(e => IsPerpLine(line, e)).ToList();
            if (crvs.Count == 0) return 0;
            var p = crvs[0].GetClosestPointTo(pt, false);
            var res = p.DistanceTo(pt);
            if (crvs.Count == 1) return res;
            for (int i = 1; i < crvs.Count; i++)
            {
                var pc = crvs[i].GetClosestPointTo(pt, false);
                var d = pc.DistanceTo(pt);
                if (d < res)
                {
                    res = d;
                }
            }
            return res;
        }

        public static double ClosestPointInVertLines(Point3d pt, Line line, IEnumerable<Line> lines, bool returninfinity = true)
        {
            var ls = lines.Where(e => IsPerpLine(line, e));
            if (!returninfinity)
                if (ls.Count() == 0) return -1;
            var res = double.PositiveInfinity;
            foreach (var l in ls)
            {
                var dis = l.GetClosestPointTo(pt, false).DistanceTo(pt);
                if (res > dis) res = dis;
            }
            return res;
        }

        public static bool ClosestPointInCurveInAllowDistance(Point3d pt, List<Polyline> crvs, double distance)
        {
            foreach (var t in crvs)
            {
                if (t.GetClosestPointTo(pt, false).DistanceTo(pt) < distance) return true;
            }
            return false;
        }

        public static double ClosestPointInCurvesFast(Point3d pt, List<Polyline> crvs)
        {
            var pl = crvs.OrderBy(t => t.GetClosePoint(pt).DistanceTo(pt)).First();
            return pl.GetClosePoint(pt).DistanceTo(pt);
        }

        public static double ClosestPointInCurves(Point3d pt, List<Polyline> crvs)
        {
            if (crvs.Count == 0) return 0;
            var p = crvs[0].GetClosestPointTo(pt, false);
            var res = p.DistanceTo(pt);
            if (crvs.Count == 1) return res;
            for (int i = 1; i < crvs.Count; i++)
            {
                var pc = crvs[i].GetClosestPointTo(pt, false);
                var d = pc.DistanceTo(pt);
                if (d < res)
                {
                    res = d;
                }
            }
            return res;
        }

        public static Point3dCollection DivideCurveByLength(Curve crv, double length, ref DBObjectCollection segs)
        {
            Point3dCollection pts = new Point3dCollection();
            pts = new Point3dCollection(crv.GetPointsByDist(length).ToArray());
            segs = crv.GetSplitCurves(pts);
            if (segs.Count == 0) segs.Add(crv);
            return pts;
        }

        public static Point3dCollection DivideCurveByKindsOfLength(Curve crv, ref DBObjectCollection segs,
            double length_a, int count_a, double length_b, int count_b,
            double length_c, int count_c, double length_d, int count_d)
        {
            Point3dCollection pts = new Point3dCollection();
            pts.Add(crv.StartPoint);
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
                    if (t < crv.GetLength()) pts.Add(crv.GetPointAtDist(t));
                    else
                    {
                        quit = true;
                        break;
                    }
                }
                if (quit) break;
            }
            pts.Add(crv.EndPoint);
            segs = crv.GetSplitCurves(pts);
            if (segs.Count == 0) segs.Add(crv);
            return pts;
        }

        public static Point3dCollection DivideCurveByDifferentLength(Curve crv, ref DBObjectCollection segs, double length_a, int count_a, double length_b, int count_b)
        {
            Point3dCollection pts = new Point3dCollection();
            pts.Add(crv.StartPoint);
            double t = 0;
            bool quit = false;
            while (true)
            {
                for (int i = 0; i < count_a + count_b; i++)
                {
                    if (i < count_a)
                    {
                        t += length_a;
                    }
                    else
                    {
                        t += length_b;
                    }
                    if (t < crv.GetLength())
                    {
                        pts.Add(crv.GetPointAtDist(t));
                    }
                    else
                    {
                        quit = true;
                        break;
                    }
                }
                if (quit) break;
            }
            pts.Add(crv.EndPoint);
            segs = crv.GetSplitCurves(pts);
            if (segs.Count == 0) segs.Add(crv);
            return pts;
        }

        public static double GetClosestDistanceOnOffsetDirection(Line line, Vector3d vec, List<Line> lines)
        {
            lines = lines.Where(e => IsParallelLine(line, e)).ToList();
            var pt = line.GetCenter();
            Line sdl = CreateLineFromStartPtAndVector(pt, vec, 100000);
            var points = new List<Point3d>();
            lines.Select(e => sdl.Intersect(e, Intersect.OnBothOperands)).ForEach(f => points.AddRange(f));
            points = points.OrderBy(e => e.DistanceTo(pt)).ToList();
            sdl.Dispose();
            return points.Count > 0 ? pt.DistanceTo(points.First()) : 0;
        }

        public static void AddToSpatialIndex(Entity e, ref ThCADCoreNTSSpatialIndex spatialIndex)
        {
            DBObjectCollection add = new DBObjectCollection();
            add.Add(e);
            spatialIndex.Update(add, new DBObjectCollection());
            return;
        }

        public static bool IsHorizontalLine(Line line, double degreetol = 1)
        {
            double angle = CreateVector(line).GetAngleTo(Vector3d.YAxis);
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }

        public static bool IsVerticalLine(Line line, double degreetol = 1)
        {
            double angle = CreateVector(line).GetAngleTo(Vector3d.XAxis);
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }

        public static double GetDisOnPolyLine(Point3d pt, Polyline poly)
        {
            if (poly.GetClosestPointTo(pt, false).DistanceTo(pt) > 0.1)
                return -1;
            double distance = 0.0;
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var lineSeg = poly.GetLineSegmentAt(i);
                if (lineSeg.IsOn(pt, new Tolerance(1.0, 1.0)))
                {
                    var newPt = pt.GetProjectPtOnLine(lineSeg.StartPoint, lineSeg.EndPoint);
                    distance += lineSeg.StartPoint.DistanceTo(newPt);
                    break;
                }
                else
                    distance += lineSeg.Length;
                lineSeg.Dispose();
            }
            return distance;
        }
        public static bool IsSubLine(Line a, Line b)
        {
            if (b.GetClosestPointTo(a.StartPoint, false).DistanceTo(a.StartPoint) < 0.001
                && b.GetClosestPointTo(a.EndPoint, false).DistanceTo(a.EndPoint) < 0.001)
                return true;
            return false;
        }
        public static void JoinLines(List<Line> lines)
        {
            double tol = 0.001;
            if (lines.Count < 2) return;
            for (int i = 0; i < lines.Count - 1; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (IsParallelLine(lines[i], lines[j]) && !IsSubLine(lines[i], lines[j]))
                    {
                        if (lines[i].StartPoint.DistanceTo(lines[j].StartPoint) < tol)
                        {
                            lines[j] = new Line(lines[i].EndPoint, lines[j].EndPoint);
                            lines.RemoveAt(i);
                            i--;
                            break;
                        }
                        else if (lines[i].StartPoint.DistanceTo(lines[j].EndPoint) < tol)
                        {
                            lines[j] = new Line(lines[i].EndPoint, lines[j].StartPoint);
                            lines.RemoveAt(i);
                            i--;
                            break;
                        }
                        else if (lines[i].EndPoint.DistanceTo(lines[j].StartPoint) < tol)
                        {
                            lines[j] = new Line(lines[i].StartPoint, lines[j].EndPoint);
                            lines.RemoveAt(i);
                            i--;
                            break;
                        }
                        else if (lines[i].EndPoint.DistanceTo(lines[j].EndPoint) < tol)
                        {
                            lines[j] = new Line(lines[i].StartPoint, lines[j].StartPoint);
                            lines.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
        }
        public static List<Line> GetLinesByInterruptingIntersections(List<Line> lines)
        {
            if (lines.Count < 2) return lines;
            var points = new List<Point3d>();
            var res = new List<Line>();
            for (int i = 0; i < lines.Count - 1; i++)
                for (int j = i + 1; j < lines.Count; j++)
                    points.AddRange(lines[i].Intersect(lines[j], Intersect.OnBothOperands));
            points = points.Distinct().ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                var pts = points.Where(p => lines[i].GetClosestPointTo(p, false).DistanceTo(p) < 0.001).ToList();
                res.AddRange(SplitLine(lines[i], pts));
            }
            return res;
        }
        public static string AnalysisLine(Line a)
        {
            string s = a.StartPoint.X.ToString() + "," + a.StartPoint.Y.ToString() + "," +
                a.EndPoint.X.ToString() + "," + a.EndPoint.Y.ToString() + ",";
            return s;
        }
        public static string AnalysisLineList(List<Line> a)
        {
            string s = "";
            foreach (var e in a)
            {
                s += AnalysisLine(e);
            }
            return s;
        }
        public static string AnalysisPoly(Polyline a)
        {
            string s = "";
            var e = a.Vertices().Cast<Point3d>().ToList();
            for (int i = 0; i < e.Count; i++)
            {
                s += e[i].X.ToString() + "," + e[i].Y.ToString() + ",";
            }
            return s;
        }

        public static string AnalysisPolyList(List<Polyline> pls)
        {
            string s = "";
            foreach (var e in pls)
            {
                s += AnalysisPoly(e);
                s.Remove(s.Length - 1);
                s += ";";
            }
            return s;
        }
        public static string AnalysisPointList(List<Point3d> points)
        {
            string s = "";
            foreach (var pt in points)
            {
                s += pt.X.ToString() + "," + pt.Y.ToString() + ",";
            }
            return s;
        }

    }
}
