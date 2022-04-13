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

namespace ThMEPWSS.UndergroundWaterSystem.Utilities
{
    public static class GeoUtils
    {
        /// <summary>
        /// 连接认为是一条直线的存在间距的两条直线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="hinderpts"></param>
        /// <returns></returns>
        public static List<Line> ConnectBrokenLine(List<Line> lines, List<Point3d> hinderpts)
        {
            List<Line> connectedLines = new List<Line>();
            List<Line> emilinatedSelfLines = new List<Line>();
            lines.ForEach(o => emilinatedSelfLines.Add(o));
            double tolHinderpts = 300;
            double tolOriHinder = 300;
            double tolBrokenLine = 2000;
            double toldegree = 3;
            List<Polyline> plylist = new List<Polyline>();
            hinderpts.ForEach(o => plylist.Add(o.CreateRectangle(tolHinderpts, tolHinderpts)));
            DBObjectCollection dbObjsOriStart = new DBObjectCollection();
            plylist.ForEach(o => dbObjsOriStart.Add(o));
            for (int i = 0; i < lines.Count; i++)
            {
                emilinatedSelfLines.RemoveAt(i);
                Point3d ptStart = lines[i].StartPoint;
                Point3d ptEnd = lines[i].EndPoint;
                Vector3d SelfLine = new Vector3d(ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y, 0);
                if (GetCrossObjsByPtCollection(ptStart.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0)
                {
                    for (int j = 0; j < emilinatedSelfLines.Count; j++)
                    {
                        Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                        Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                        Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                        if (ptStart.DistanceTo(ptmp1) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptStart.X - ptmp1.X, ptStart.Y - ptmp1.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptStart, ptmp1);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                        else if (ptStart.DistanceTo(ptmp2) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptStart.X - ptmp2.X, ptStart.Y - ptmp2.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptStart, ptmp2);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                    }
                }
                if (GetCrossObjsByPtCollection(ptEnd.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0)
                {
                    for (int j = 0; j < emilinatedSelfLines.Count; j++)
                    {
                        Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                        Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                        Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                        if (ptEnd.DistanceTo(ptmp1) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptEnd.X - ptmp1.X, ptEnd.Y - ptmp1.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptEnd, ptmp1);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                        else if (ptEnd.DistanceTo(ptmp2) < tolBrokenLine)
                        {
                            Vector3d vector = new Vector3d(ptEnd.X - ptmp2.X, ptEnd.Y - ptmp2.Y, 0);
                            double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                            double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                            bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                            bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                            if (bool1 && bool2)
                            {
                                Line line = new Line(ptEnd, ptmp2);
                                connectedLines.Add(line);
                                emilinatedSelfLines.Insert(i, lines[i]);
                                break;
                            }
                        }
                    }
                }
                emilinatedSelfLines.Insert(i, lines[i]);
            }
            return connectedLines;
        }
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
        public static DBObjectCollection GetCrossObjsByPtCollection(Point3dCollection ptcoll, DBObjectCollection dbObjs)
        {
            ThCADCoreNTSSpatialIndex spatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            var crossObjs = spatialIndex.SelectCrossingPolygon(ptcoll);
            return crossObjs;
        }
        public static bool IsTianZhengElement(Entity ent)
        {
            return IsTianZhengElement(ent.GetType());
        }
        private static bool IsTianZhengElement(Type type)
        {
            return type.IsNotPublic && type.Name.StartsWith("Imp") && type.Namespace == "Autodesk.AutoCAD.DatabaseServices";
        }
        public static Vector3d CreateVector(Point3d ps, Point3d pe)
        {
            return new Vector3d(pe.X - ps.X, pe.Y - ps.Y, pe.Z - ps.Z);
        }
        public static void InterrptLineByPoints(List<Line> lines, List<Point3d> points)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var pts = points.Where(p => line.GetClosestPointTo(p, false).DistanceTo(p) < 10)
                    .Select(p => line.GetClosestPointTo(p, false))
                    .Where(p => p.DistanceTo(line.StartPoint) > 10 && p.DistanceTo(line.EndPoint) > 10).ToList();
                if (pts.Count == 0) continue;
                else
                {
                    var res = SplitLine(line, pts);
                    lines.AddRange(res);
                    lines.RemoveAt(i);
                    i--;
                }
            }
        }
        public static List<Line> SplitLine(Line line, List<Point3d> points)
        {
            points.Insert(0, line.StartPoint);
            points.Add(line.EndPoint);
            points = RemoveDuplicatePts(points);
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
                lineSeg.Dispose();
            }
            return distance;
        }
    }
}
