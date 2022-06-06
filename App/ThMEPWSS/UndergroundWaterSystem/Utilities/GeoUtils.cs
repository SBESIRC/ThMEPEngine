using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPWSS.CADExtensionsNs;

namespace ThMEPWSS.UndergroundWaterSystem.Utilities
{
    public static class GeoUtils
    {
        public static void LogInfos(string str, bool creatNewFile = false)
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var mode = FileMode.Append;
            if (creatNewFile) mode = FileMode.Create;
            FileStream fs = new FileStream(dir + "\\WaterDebug.txt", mode);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(str + DateTime.Now.ToString());
            sw.Close();
            fs.Close();
        }
        public static Line GetProjectLine(this Line line)
        {
            return new Line(new Point3d(line.StartPoint.X, line.StartPoint.Y, 0), new Point3d(line.EndPoint.X, line.EndPoint.Y, 0));
        }
        private static void TraverseConnectedLinesWithHinderpts(
            ref List<Line> emilinatedSelfLines,Point3d point,double tolBrokenLine,Vector3d SelfLine,
            ref List<Line> lines,int i,double toldegree, ref List<Line> connectedLines)
        {
            for (int j = 0; j < emilinatedSelfLines.Count; j++)
            {
                Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                if (point.DistanceTo(ptmp1) < tolBrokenLine)
                {
                    Vector3d vector = new Vector3d(point.X - ptmp1.X, point.Y - ptmp1.Y, 0);
                    double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                    double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                    bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                    bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                    if (bool1 && bool2)
                    {
                        Line line = new Line(point, ptmp1);
                        line.Linetype = lines[i].Linetype;
                        connectedLines.Add(line);
                        emilinatedSelfLines.Insert(i, lines[i]);
                        break;
                    }
                }
                else if (point.DistanceTo(ptmp2) < tolBrokenLine)
                {
                    Vector3d vector = new Vector3d(point.X - ptmp2.X, point.Y - ptmp2.Y, 0);
                    double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                    double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                    bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                    bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                    if (bool1 && bool2)
                    {
                        Line line = new Line(point, ptmp2);
                        line.Linetype = lines[i].Linetype;
                        connectedLines.Add(line);
                        emilinatedSelfLines.Insert(i, lines[i]);
                        break;
                    }
                }
            }
        }
        private static void TraverseConnectedLinesWithJoinedPoints(
         ref List<Line> emilinatedSelfLines, Point3d point, double tolBrokenLine, Vector3d SelfLine,
         ref List<Line> lines, int i, double toldegree, ref List<Line> connectedLines, List<Polyline> crossedplys)
        {
            for (int j = 0; j < emilinatedSelfLines.Count; j++)
            {
                Point3d ptmp1 = emilinatedSelfLines[j].StartPoint;
                Point3d ptmp2 = emilinatedSelfLines[j].EndPoint;
                var p = lines[i].GetClosestPointTo(ptmp1, false).DistanceTo(ptmp1) <
                    lines[i].GetClosestPointTo(ptmp2, false).DistanceTo(ptmp2) ? ptmp1 : ptmp2;
                Polyline crossed= new Polyline();
                foreach (var ply in crossedplys)
                {
                    if (ply.Contains(point)) crossed = ply;
                }
                if (crossed.Area < 1) continue;
                if (!crossed.Contains(p)) continue;
                //if (!IsInAnyPolys(p, crossedplys, true)) continue;
                if (lines[i].GetClosestPointTo(p, false).DistanceTo(p) == 0) continue;
                //Vector3d vector = new Vector3d(point.X - ptmp1.X, point.Y - ptmp1.Y, 0);
                //Vector3d TestLine = new Vector3d(ptmp2.X - ptmp1.X, ptmp2.Y - ptmp1.Y, 0);
                //double degree1 = Math.Abs(SelfLine.GetAngleTo(TestLine).AngleToDegree());
                //double degree2 = Math.Abs(SelfLine.GetAngleTo(vector).AngleToDegree());
                //bool bool1 = degree1 < toldegree || (degree1 > 180 - toldegree && degree1 < 180 + toldegree);
                //bool bool2 = degree2 < toldegree || (degree2 > 180 - toldegree && degree2 < 180 + toldegree);
                //if (bool1 && bool2) continue;
                var vec_j = CreateVector(emilinatedSelfLines[j]);
                double angle = vec_j.GetAngleTo(SelfLine);
                var angle_cond = Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < 1;
                if (angle_cond) continue;
                int count_on_line = 0;
                foreach (var lin in lines)
                {
                    if (lin.GetClosestPointTo(point, false).DistanceTo(point) < 10)
                        count_on_line++;
                }
                if (count_on_line > 1) continue;
                count_on_line = 0;
                foreach (var lin in lines)
                {
                    if (lin.GetClosestPointTo(p, false).DistanceTo(p) < 10)
                        count_on_line++;
                }
                if (count_on_line > 1) continue;
                Line line = new Line(point, p);
                line.Linetype = lines[i].Linetype;
                connectedLines.Add(line);
                emilinatedSelfLines.Insert(i, lines[i]);
                break;
            }
        }
        /// <summary>
        /// 连接认为是一条直线的存在间距的两条直线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="hinderpts"></param>
        /// <returns></returns>
        public static List<Line> ConnectBrokenLine(List<Line> lines, List<Point3d> hinderpts,List<Point3d>joinedpts)
        {
            List<Line> connectedLines = new List<Line>();
            List<Line> emilinatedSelfLines = new List<Line>();
            lines.ForEach(o => emilinatedSelfLines.Add(o));
            double tolHinderpts = 300;
            double tolOriHinder = 300;
            double tolBrokenLine = 2000;
            double toldegree = 3;
            List<Polyline> plylist = new List<Polyline>();
            List<Polyline> plylist_joined = new List<Polyline>();
            hinderpts.ForEach(o => plylist.Add(o.CreateRectangle(tolHinderpts, tolHinderpts)));
            joinedpts.ForEach(o => plylist_joined.Add(o.CreateRectangle(tolHinderpts, tolHinderpts)));
            DBObjectCollection dbObjsOriStart = plylist.ToCollection();
            DBObjectCollection dbObjsOriJoined = plylist_joined.ToCollection();
            //plylist.ForEach(o => dbObjsOriStart.Add(o));
            for (int i = 0; i < lines.Count; i++)
            {
                emilinatedSelfLines.RemoveAt(i);
                Point3d ptStart = lines[i].StartPoint;
                Point3d ptEnd = lines[i].EndPoint;
                Vector3d SelfLine = new Vector3d(ptEnd.X - ptStart.X, ptEnd.Y - ptStart.Y, 0);
                if (GetCrossObjsByPtCollection(ptStart.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0
                    && ClosestPointInVertLines(ptStart, lines[i], lines) > 1)
                {
                    TraverseConnectedLinesWithHinderpts(ref emilinatedSelfLines, ptStart, tolBrokenLine, SelfLine, ref lines,
                        i, toldegree, ref connectedLines);
                }
                if (GetCrossObjsByPtCollection(ptEnd.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriStart).Count == 0
                    && ClosestPointInVertLines(ptEnd, lines[i], lines) > 1)
                {
                    TraverseConnectedLinesWithHinderpts(ref emilinatedSelfLines, ptEnd, tolBrokenLine, SelfLine, ref lines,
                        i, toldegree, ref connectedLines);
                }
                var crossedStart = GetCrossObjsByPtCollection(ptStart.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriJoined).Cast<Polyline>().ToList();
                if (crossedStart.Count > 0
                    && ClosestPointInVertLines(ptStart, lines[i], lines) > 1)
                {
                    TraverseConnectedLinesWithJoinedPoints(ref emilinatedSelfLines, ptStart, tolBrokenLine, SelfLine, ref lines,
                        i, toldegree, ref connectedLines, crossedStart);
                }
                var crossedEnd = GetCrossObjsByPtCollection(ptEnd.CreateRectangle(tolOriHinder, tolOriHinder).Vertices(), dbObjsOriJoined).Cast<Polyline>().ToList();
                if (crossedEnd.Count > 0
                    && ClosestPointInVertLines(ptEnd, lines[i], lines) > 1)
                {
                    TraverseConnectedLinesWithJoinedPoints(ref emilinatedSelfLines, ptEnd, tolBrokenLine, SelfLine, ref lines,
                        i, toldegree, ref connectedLines, crossedEnd);
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
        public static bool IsParallelLine(Line a, Line b, double degreetol = 1)
        {
            double angle = CreateVector((Line)a).GetAngleTo(CreateVector((Line)b));
            return Math.Min(angle, Math.Abs(Math.PI - angle)) / Math.PI * 180 < degreetol;
        }
        public static Vector3d CreateVector(Line line)
        {
            return CreateVector(line.StartPoint, line.EndPoint);
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
        public static Line[] SplitLine(Line line, Curve cutter, double length_filter = 1)
        {
            List<Point3d> points = new List<Point3d>();
            points.AddRange(line.Intersect(cutter, Intersect.OnBothOperands));
            points = RemoveDuplicatePts(points, 1);
            SortAlongCurve(points, line);
            if (points.Count > 0)
                return SplitLine(line, points).Where(e => e.Length > length_filter).ToArray();
            else return new Line[] { new Line(line.StartPoint, line.EndPoint) };
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
        public static Polyline CreatePolyFromPoints(Point3d[] points, bool closed = true)
        {
            Polyline p = new Polyline();
            for (int i = 0; i < points.Length; i++)
            {
                p.AddVertexAt(i, points[i].ToPoint2d(), 0, 0, 0);
            }
            if (closed) p.Closed = true;
            return p;
        }
        public static List<Entity> GetAllEntitiesByExplodingTianZhengElementThoroughly(Entity entity)
        {
            if (!IsTianZhengElement(entity)) return new List<Entity>() { entity };
            List<Entity> results = new List<Entity>();
            List<Entity> containers = new List<Entity>() { entity };
            while (true)
            {
                var elements = new List<Entity>();
                foreach (var ent in containers)
                {
                    if (IsTianZhengElement(ent))
                    {
                        try
                        {
                            var res = ent.ExplodeToDBObjectCollection().OfType<Entity>().ToList();
                            foreach (var r in res)
                            {
                                if (IsTianZhengElement(r)) elements.Add(r);
                                else results.Add(r);
                            }
                        }
                        catch (Exception ex)
                        {
                            //有的天正元素无法炸开？
                        }
                    }
                }
                containers = elements;
                if (containers.Count == 0) break;
            }
            return results;
        }
        public static bool IsConnectedToLines(List<Line> lines, Point3d point, double tol = 1)
        {
            foreach (var line in lines)
                if (IsConnectedToLine(line, point, tol)) return true;
            return false;
        }
        public static bool IsConnectedToLine(Line line, Point3d point, double tol = 1)
        {
            if (line.StartPoint.DistanceTo(point) <= tol || line.EndPoint.DistanceTo(point) <= tol)
                return true;
            return false;
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
        public static string AnalysisLineList(List<Line> a)
        {
            string s = "";
            foreach (var e in a)
            {
                s += AnalysisLine(e);
            }
            return s;
        }
        public static string AnalysisLine(Line a)
        {
            string s = a.StartPoint.X.ToString() + "," + a.StartPoint.Y.ToString() + "," +
                a.EndPoint.X.ToString() + "," + a.EndPoint.Y.ToString() + ",";
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
        public static bool TestContainsChineseCharacter(string text)
        {
            return Regex.IsMatch(text, @"[\u4e00-\u9fa5]");
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
        public static bool IsPerpLine(Line a, Line b, double degreetol = 1)
        {
            double angle = CreateVector((Line)a).GetAngleTo(CreateVector((Line)b));
            return Math.Abs(Math.Min(angle, Math.Abs(Math.PI * 2 - angle)) / Math.PI * 180 - 90) < degreetol;
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
    }
}
