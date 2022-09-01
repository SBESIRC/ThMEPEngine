using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.Common;
using ThMEPWSS.DrainageSystemAG.Models;
using ThMEPWSS.Model;
namespace ThMEPWSS.DrainageSystemAG.Bussiness
{
    public class FloorFramedSpliter
    {
        public static List<Polyline> ConvertToCorrectSpliteLines(List<Polyline> splitLines, Polyline bound)
        {
            var modified_spliter = new List<Polyline>();
            bound = bound.GeometricExtents.ToRectangle();
            //分割线合理性判断
            modified_spliter = splitLines.Where(e => bound.Contains(e.GetMidpoint()) || e.IntersectWithEx(bound).Count>0).ToList();
            //如果分割线超出楼层框，取框内部分
            modified_spliter = modified_spliter.Select(e =>
              {
                  if (e.IntersectWithEx(bound).Count>0)
                  {
                      var split = SplitCurve(e, bound).Where(crv => crv.GetLength() > 0).OrderByDescending(crv => crv.GetLength()).First();
                      var pl = new Polyline();
                      if (split is Polyline)
                          pl = (Polyline)split;
                      else if (split is Line)
                          pl = PolyFromLine((Line)split);
                      return pl;
                  }
                  else
                      return e;
              }).ToList();
            modified_spliter = modified_spliter.Where(e => e.Length > 0).ToList();
            //起始点终点按x坐标升序排序
            modified_spliter = modified_spliter.Select(e =>
              {
                  if (e.StartPoint.X <= e.EndPoint.X)
                      return e;
                  else
                  {
                      var points=e.Vertices().Cast<Point3d>().ToList();
                      points.Reverse();
                      var pl = PolyFromPoints(points.ToArray(), false);
                      return pl;
                  }
              }).ToList();
            var edge_up = bound.GetEdges().OrderByDescending(e => e.GetMidpoint().Y).First();
            var edge_down = bound.GetEdges().OrderByDescending(e => e.GetMidpoint().Y).Last();
            var edge_left= bound.GetEdges().OrderBy(e => e.GetMidpoint().X).First();
            var edge_right = bound.GetEdges().OrderBy(e => e.GetMidpoint().X).Last();
            //首尾连接框线
            modified_spliter = modified_spliter.Select(e =>
              {
                  var points = e.Vertices().Cast<Point3d>().ToList();
                  int count = points.Count;
                  var firstPoint = points.First();
                  var lastPoint = points.Last();
                  var p_up = edge_up.GetClosestPointTo(firstPoint, false);
                  var p_down = edge_down.GetClosestPointTo(lastPoint, false);
                  if (p_up.DistanceTo(firstPoint) > 1)
                      points.Insert(0, p_up);
                  if (p_down.DistanceTo(lastPoint) > 1)
                      points.Add(p_down);
                  if (points.Count == count)
                      return e;
                  else
                      return PolyFromPoints(points.ToArray(),false);
              }).ToList();
            modified_spliter.Insert(0, PolyFromLine(edge_left));
            modified_spliter.Add(PolyFromLine(edge_right));
            var res = new List<Polyline>();
            //相邻分隔线组成面域
            modified_spliter = modified_spliter.OrderBy(e => e.StartPoint.X).ToList();
            for (int i = 0; i < modified_spliter.Count - 1; i++)
            {
                var a = modified_spliter[i];
                var b=modified_spliter[i + 1];
                var points_a = a.Vertices().Cast<Point3d>().ToList();
                var points_b = a.Vertices().Cast<Point3d>().ToList();
                points_a.AddRange(b.Vertices().Cast<Point3d>());
                points_b.AddRange(b.Vertices().Cast<Point3d>().Reverse());
                var pl_a = PolyFromPoints(points_a.ToArray());
                var pl_b=PolyFromPoints(points_b.ToArray());
                if (pl_a.Length <= pl_b.Length)
                    res.Add(pl_a);
                else
                    res.Add(pl_b);
            }
            return res;
        }
        static Polyline PolyFromPoints(Point3d[] points, bool closed = true)
        {
            Polyline p = new Polyline();
            for (int i = 0; i < points.Length; i++)
            {
                p.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
            }
            p.Closed = closed;
            return p;
        }
        static Polyline PolyFromLine(Line a)
        {
            Polyline p = new Polyline();
            p.AddVertexAt(0, a.StartPoint.ToPoint2d(), 0, 0, 0);
            p.AddVertexAt(1, a.EndPoint.ToPoint2d(), 0, 0, 0);
            return p;
        }
        static Curve[] SplitCurve(Curve curve, Curve splitter)
        {
            List<Point3d> points = new List<Point3d>();
            points.AddRange(curve.Intersect(splitter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0 && curve.GetLength() > 1)
            {
                Point3dCollection ps = new Point3dCollection(points.Select(e => curve.GetClosestPointTo(e, false)).ToArray());
                var splited = curve.GetSplitCurves(ps);
                ps.Dispose();
                return splited.Cast<Curve>().Where(e => e.GetLength() > 1).ToArray();
            }
            else
                return new Curve[] { curve };
        }
        static void SortAlongCurve(List<Point3d> points, Curve curve)
        {
            var comparer = new PointAlongCurveComparer(curve);
            points.Sort(comparer);
            return;
        }
        class PointAlongCurveComparer : IComparer<Point3d>
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
        static double GetDisOnPolyLine(Point3d pt, Polyline poly)
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
        static List<Point3d> RemoveDuplicatePts(List<Point3d> points, double tol = 0)
        {
            if (points.Count < 2) return points;
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
    }
}
