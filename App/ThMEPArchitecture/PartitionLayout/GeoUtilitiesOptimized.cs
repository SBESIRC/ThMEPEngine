using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
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
    public static class GeoUtilitiesOptimized
    {
        public static Point3d GetRecCentroid(this Polyline rec)
        {
            var ext = rec.GeometricExtents;
            var min = ext.MinPoint;
            var max = ext.MaxPoint;
            return new Point3d((min.X + max.X) / 2, (min.Y + max.Y) / 2, 0);
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
            }
            return distance;
        }

        public static List<Line> SplitLine(Line line, List<Point3d> points)
        {
            points.Insert(0, line.StartPoint);
            points.Add(line.EndPoint);
            RemoveDuplicatePts(points);
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
            else return new Line[] { line };
        }

        public static Line[] SplitLine(Line curve, List<Polyline> cutters, double length_filter = 1)
        {
            List<Point3d> points = new List<Point3d>();
            foreach (var cutter in cutters)
                points.AddRange(curve.Intersect(cutter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, curve);
            if (points.Count > 0)
                return SplitLine(curve, points).Where(e => e.Length > length_filter).ToArray();
            else
                return new Line[] { curve };
        }



    }
}
