using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using static ThMEPArchitecture.PartitionLayout.GeoUtilitiesOptimized;

namespace ThMEPArchitecture.PartitionLayout
{
    public static class GeoUtilities
    {
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
            else return new List<Curve>() { curve };
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

        public static bool IsPerpLine(Line a, Line b, double degreetol = 1)
        {
            double angle = CreateVector((Line)a).GetAngleTo(CreateVector((Line)b));
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
            if (!allowOnEdge)
            {
                if (ClosestPointInCurves(pt, pls) < 1) return false;
            }
            pls.OrderBy(e => e.GetClosestPointTo(pt, false).DistanceTo(pt));
            foreach (var p in pls)
            {
                if (p.Area < 1) continue;
                if (p.Vertices().Count == 5)
                {
                    if (p.GeometricExtents.IsPointIn(pt))
                        return true;
                }
                else
                {
                    if (/*p.IsPointInFast(pt)*/p.Contains(pt))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsInAnyBoxes(Point3d pt, List<Polyline> boxes)
        {
            foreach (var p in boxes)
            {
                if (p.Area < 1) continue;
                p.TransformBy(Matrix3d.Scaling(0.99, p.GetRecCentroid()));
                if (p.GeometricExtents.IsPointIn(pt))
                {
                    p.TransformBy(Matrix3d.Scaling(1 / 0.99, p.GetRecCentroid()));
                    return true;
                }
                p.TransformBy(Matrix3d.Scaling(1 / 0.99, p.GetRecCentroid()));
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
