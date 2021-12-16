using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
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
        public static void SortLinesByLength(List<Line> lines, bool ascending = true)
        {
            var comparer = new LineLengthComparer();
            lines.Sort(comparer);
            if (!ascending) lines.Reverse();
            return;
        }
        private class LineLengthComparer : IComparer<Line>
        {
            public LineLengthComparer()
            {

            }
            public int Compare(Line a, Line b)
            {
                if (a.Length == b.Length) return 0;
                else if (a.Length < b.Length) return -1;
                else return 1;
            }
        }

        public static void SortLinesByDistanceToPoint(List<Line> lines, Point3d point)
        {
            var comparer = new LineDisToPointComparer(point);
            lines.Sort(comparer);
            return;
        }
        private class LineDisToPointComparer : IComparer<Line>
        {
            public LineDisToPointComparer(Point3d pt)
            {
                Pt = pt;
            }
            private Point3d Pt;
            public int Compare(Line a, Line b)
            {
                var disa = a.GetClosestPointTo(Pt, false).DistanceTo(Pt);
                var disb = b.GetClosestPointTo(Pt, false).DistanceTo(Pt);
                if (disa == disb) return 0;
                else if (disa < disb) return -1;
                else return 1;
            }
        }

        public static void SortAlongCurve(List<Point3d> points, Curve curve)
        {
            var comparer = new PointAlongCurveComparer(curve);
            points.Sort(comparer);
            return;
        }

        public static List<Line> DivideLineByLength(Line line,double length)
        {
            int count = ((int)Math.Floor(line.Length / length));
            List<Line> res = new List<Line>();
            Line a = LineSDL(line.StartPoint, Vector(line), length);
            for (int i = 0; i < count; i++)
            {
                var k = Line(a);
                k.TransformBy(Matrix3d.Displacement(Vector(line).GetNormal() * length * i));
                res.Add(k);
            }
            if (res[res.Count - 1].EndPoint.DistanceTo(line.EndPoint) > 1)
            {
                res.Add(new Line(res[res.Count - 1].EndPoint, line.EndPoint));
            }
            return res;
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
                if (Curve is Polyline)
                {
                    var pl = (Polyline)Curve;
                    param_a = a.GetDisOnPolyLine(pl);
                    param_b = b.GetDisOnPolyLine(pl);
                }
                else if (Curve is Line)
                {
                    var line = (Line)Curve;
                    var pa = line.GetClosestPointTo(a, false);
                    var pb = line.GetClosestPointTo(b, false);
                    param_a = pa.DistanceTo(line.StartPoint);
                    param_b = pb.DistanceTo(line.StartPoint);
                }
                if (param_a == param_b) return 0;
                else if (param_a < param_b) return -1;
                else return 1;
            }
        }

        public static double GetDisOnPolyLine(this Point3d pt, Polyline poly)
        {
            if (poly.GetClosestPointTo(pt, false).DistanceTo(pt) > 0.1)
            {
                return -1;
            }
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
                {
                    distance += lineSeg.Length;
                }
            }
            return distance;
        }

        public static Polyline PolyFromLine(Line a)
        {
            Polyline p = new Polyline();
            p.AddVertexAt(0, a.StartPoint.ToPoint2d(), 0, 0, 0);
            p.AddVertexAt(1, a.EndPoint.ToPoint2d(), 0, 0, 0);
            return p;
        }

        public static Line LineSDL(Point3d start, Vector3d vec, double length)
        {
            var v = vec.GetNormal() * length;
            var pe = start.TransformBy(Matrix3d.Displacement(v));
            return new Line(start, pe);
        }

        public static Vector3d Vector(Line line)
        {
            return Vector(line.StartPoint, line.EndPoint);
        }

        public static Vector3d Vector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }

        public static Line Line(Line a)
        {
            return new Line(a.StartPoint, a.EndPoint);
        }

        public static Polyline PolyFromPoints(List<Point3d> points)
        {
            Polyline p = new Polyline();
            for (int i = 0; i < points.Count; i++)
            {
                p.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
            }
            return p;
        }

        public static Polyline PolyFromPoints(Point3d[] points, bool closed = true)
        {
            Polyline p = new Polyline();
            for (int i = 0; i < points.Length; i++)
            {
                p.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
            }
            p.Closed = closed;
            return p;
        }

        public static List<Polyline> JoinCurves(List<Polyline> plys, List<Line> lines, double tol = 1)
        {
            List<Polyline> pls = new List<Polyline>();
            pls.AddRange(plys);
            lines.ForEach(e => pls.Add(PolyFromLine(e)));
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
            pls.Clear();
            return result;
        }

        public static List<Line> OffsetLine(Line a, double dis)
        {
            var vec_a = Vector(a).GetPerpendicularVector().GetNormal() * dis;
            var la = (Line)a.Clone();
            var lb = (Line)a.Clone();
            la.TransformBy(Matrix3d.Displacement(vec_a));
            lb.TransformBy(Matrix3d.Displacement(-vec_a));
            return new List<Line>() { la, lb };
        }

        public static List<Curve> SplitCurve(Curve curve, DBObjectCollection objs)
        {
            List<Point3d> pts = new List<Point3d>();
            //objs.Cast<Entity>().ToList().ForEach(e => pts.AddRange(curve.Intersect(e, Intersect.OnBothOperands)));
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
                    ;
                }
                return splited.Cast<Curve>().Where(e => e.GetLength() > 1).ToList();
            }
            else return new List<Curve>() { curve };
        }

        public static Curve[] SplitCurve(Curve curve, Curve[] cutters)
        {
            List<Point3d> points = new List<Point3d>();
            cutters.ForEach(e => points.AddRange(curve.Intersect(e, Intersect.OnBothOperands)));
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

        public static Curve[] SplitCurve(Curve curve, Curve cutter)
        {
            List<Point3d> points = new List<Point3d>();
            points.AddRange(curve.Intersect(cutter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0 && curve.GetLength()>1)
            {
                Point3dCollection ps = new Point3dCollection(points.Select(e => curve.GetClosestPointTo(e, false)).ToArray());
                var splited = curve.GetSplitCurves(ps);
                ps.Dispose();

                //DoubleCollection param = new DoubleCollection(points.Select(e => curve.GetParamAtPointX(curve.GetClosestPointTo(e, false))).ToArray());
                //var splited = curve.GetSplitCurves(param);
                return splited.Cast<Curve>().Where(e => e.GetLength()>1).ToArray();
            }
            else
            {
                return new Curve[] { curve };
            }
        }

        public static Curve[] SplitLine(Line curve, Curve cutter)
        {
            List<Point3d> points = new List<Point3d>();
            points.AddRange(curve.Intersect(cutter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0)
            {
                Point3dCollection ps = new Point3dCollection(points.Select(e => curve.GetClosestPointTo(e, false)).ToArray());
                points = points.Select(e => curve.GetClosestPointTo(e, false)).ToList();
                var splited = GetSplitLine(curve, points);
                ps.Dispose();
                return splited.Cast<Curve>().Where(e => e.GetLength()>1).ToArray();
            }
            else
            {
                return new Curve[] { curve };
            }
        }

        public static Curve[] SplitLine(Line curve, List<Polyline> cutters)
        {
            List<Point3d> points = new List<Point3d>();
            foreach (var cutter in cutters)
            {
                points.AddRange(curve.Intersect(cutter, Intersect.OnBothOperands));
            }
            
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0)
            {
                Point3dCollection ps = new Point3dCollection(points.Select(e => curve.GetClosestPointTo(e, false)).ToArray());
                points = points.Select(e => curve.GetClosestPointTo(e, false)).ToList();
                var splited = GetSplitLine(curve, points);
                ps.Dispose();
                return splited.Cast<Curve>().Where(e => e.GetLength()>1).ToArray();
            }
            else
            {
                return new Curve[] { curve };
            }
        }

        public static Curve[] SplitCurve(Curve curve, List<Polyline> cutters)
        {
            List<Point3d> points = new List<Point3d>();          
            foreach (var cutter in cutters)
            {
                points.AddRange(curve.Intersect(cutter, Intersect.OnBothOperands));
            }
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0)
            {
                Point3dCollection ps = new Point3dCollection(points.Select(e => curve.GetClosestPointTo(e, false)).ToArray());
                var splited = curve.GetSplitCurves(ps);
                ps.Dispose();
                return splited.Cast<Curve>().ToArray();
            }
            else
            {
                return new Curve[] { curve };
            }
        }


        public static List<Point3d> RemoveDuplicatePts(List<Point3d> points, double tol = 0)
        {
            List<Point3d> results = new List<Point3d>(points);
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

        public static bool IsParallelLine(Line a, Line b, double degreetol = 1)
        {
            double angle = Vector((Line)a).GetAngleTo(Vector((Line)b));
            return Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < degreetol;
        }

        public static List<Curve> SplitCurve(Curve curve, List<Point3d> points)
        {
            List<Curve> results = new List<Curve>() { curve };
            SortAlongCurve(points, curve);
            if (curve is Line)
            {
                return GetSplitLine((Line)curve, points).Cast<Curve>().ToList();
            }
            else if (curve is Polyline)
            {
                List<Polyline> plys = new List<Polyline>();
                var pl = (Polyline)curve;
                var verts = pl.Vertices().Cast<Point3d>().ToList();
                var param = verts.Select(e => /*pl.GetParamAtPointX(e)*/GetDisOnPolyLine(e, pl)).ToList();
                points.Insert(0, verts.First());
                points.Add(verts.Last());
                points = RemoveDuplicatePts(points);
                SortAlongCurve(points, pl);
                param.RemoveAt(0);
                param.RemoveAt(param.Count - 1);
                verts.RemoveAt(0);
                verts.RemoveAt(verts.Count - 1);
                if(verts.Count==0) return GetSplitLine(new Line(curve.StartPoint,curve.EndPoint), points).Cast<Curve>().ToList();


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

        public static bool IsPerpLine(Line a, Line b, double degreetol = 1)
        {
            double angle = Vector((Line)a).GetAngleTo(Vector((Line)b));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
        }

        public static double DisBetweenTwoParallelLines(Line a, Line b)
        {
            Point3d pt_on_a = a.GetClosestPointTo(b.GetCenter(), false);
            Point3d pt_on_b = b.GetClosestPointTo(pt_on_a, false);
            return pt_on_a.DistanceTo(pt_on_b);
        }

        public static bool IsInAnyPolys(Point3d pt, List<Polyline> pls)
        {
            foreach (var p in pls)
            {
                var pp = p.Clone() as Polyline;
                pp.TransformBy(Matrix3d.Scaling(0.99, pp.Centroid()));
                if (pp.IsPointIn(pt))
                {
                    pp.Dispose();
                    return true;
                }
                pp.Dispose();
            }
            return false;
        }

        public static bool IsInCar(Point3d pt, List<Polyline> pls)
        {
            if (pls.Count == 0) return false;
            var bContains = pls.Any(pl => pl.GeometricExtents.IsPointIn(pt));
            return bContains;
        }

        public static void ClosestPointInCurves(Point3d pt, List<Curve> crvs,
            ref Point3d result, ref double dis, ref int index)
        {
            if (crvs.Count == 0) return;
            result = crvs[0].GetClosestPointTo(pt, false);
            dis = result.DistanceTo(pt);
            index = 0;
            if (crvs.Count == 1) return;
            for (int i = 1; i < crvs.Count; i++)
            {
                var p = crvs[i].GetClosestPointTo(pt, false);
                var d = p.DistanceTo(pt);
                if (d < dis)
                {
                    dis = d;
                    index = i;
                    result = p;
                }
            }
            return;
        }

        public static double ClosestPointInCurves(Point3d pt, List<Curve> crvs)
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

        private static List<Line> GetSplitedLine(Line line, List<Point3d> points)
        {
            List<Line> results = new List<Line>();
            SortAlongCurve(points, line);
            if (points.Count == 0) return new List<Line>() { line };

            if (points[0].DistanceTo(line.StartPoint) > 1) points.Insert(0, line.StartPoint);
            if (points[points.Count - 1].DistanceTo(line.EndPoint) > 1) points.Add(line.EndPoint);
            for (int i = 0; i < points.Count - 1; i++)
            {
                results.Add(new Line(points[i], points[i = 1]));
            }
            return results;

        }


        public static bool IsIntersect(Curve c, List<Curve> crvs)
        {
            foreach (var crv in crvs)
            {
                if (c.Intersect(crv, Intersect.OnBothOperands).Count > 0)
                    return true;
            }
            return false;
        }

        public static Point3dCollection DivideCurveByLength(Curve crv, double length, ref DBObjectCollection segs)
        {
            Point3dCollection pts = new Point3dCollection();

            pts = new Point3dCollection(crv.GetPointsByDist(length).ToArray());

            segs = crv.GetSplitCurves(pts);
            if (segs.Count == 0) segs.Add(crv);

            return pts;
        }

        public static void AddToSpatialIndex(Entity e, ref ThCADCoreNTSSpatialIndex spatialIndex)
        {
            DBObjectCollection add = new DBObjectCollection();
            add.Add(e);
            spatialIndex.Update(add, new DBObjectCollection());

            return;
        }

        public static void AddToSpatialIndex(DBObjectCollection objs, ref ThCADCoreNTSSpatialIndex spatialIndex)
        {
            spatialIndex.Update(objs, new DBObjectCollection());
            return;
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
                s += AnalysisPoly(e) + ";";
            }
            return s;
        }

        public static double GetLengthDifferentFromParallelBofA(Line a, Line b, double buffer = 15700)
        {
            double length = 0;
            var pl = b.Buffer(buffer);
            var splited = SplitCurve(a, new DBObjectCollection() { pl });
            foreach (Line s in splited)
            {
                if (!pl.IsPointIn(s.GetCenter())) length += s.Length;
            }
            return length;
        }

        public static List<Line> GetSplitLine(Line line, List<Point3d> points)
        {
            points.Insert(0, line.StartPoint);
            points.Add(line.EndPoint);
            RemoveDuplicatePts(points);
            try
            {
                SortAlongCurve(points, line);
            }
            catch(Exception ex)
            {
                ;
                SortAlongCurve(points, line);
            }
            List<Line> results = new List<Line>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line r = new Line(points[i], points[i + 1]);
                results.Add(r);
            }
            return results;
        }
    }
}
