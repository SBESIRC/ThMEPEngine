using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NFox.Cad;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Algorithm;

namespace ThMEPStructure.Reinforcement.Service
{
    public static class ThReinforcementUtils
    {
        public readonly static double WallColumnBufferLength = 1.0;
        public static DBObjectCollection GetEntitiesFromMS(this Database db, List<string> layers)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                return acadDb.ModelSpace
                    .OfType<Entity>()
                    .Where(o => layers.Contains(o.Layer))
                    .ToCollection();
            }
        }
        public static DBObjectCollection Clean(this DBObjectCollection lines)
        {
            if (lines.Count == 0)
            {
                return new DBObjectCollection();
            }
            else
            {
                var cleanInstance = new ThLaneLineCleanService();
                return cleanInstance.CleanNoding(lines);
            }
        }

        public static DBObjectCollection PostProcess(this DBObjectCollection polygons, double areaTolerance = 1.0)
        {
            var results = polygons.FilterSmallArea(areaTolerance);
            var roomSimplifier = new ThPolygonalElementSimplifier();
            results = roomSimplifier.Normalize(results);
            results = results.FilterSmallArea(areaTolerance);
            results = roomSimplifier.MakeValid(results);
            results = results.FilterSmallArea(areaTolerance);
            results = roomSimplifier.Simplify(results);
            results = results.FilterSmallArea(areaTolerance);
            results = results.RepeatedRemoved();
            return results;
        }

        public static DBObjectCollection RepeatedRemoved(this DBObjectCollection objs)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.SelectAll();
        }

        public static DBObjectCollection Extend(this DBObjectCollection lines, double length)
        {
            return lines.OfType<Line>().Select(o => o.ExtendLine(length)).ToCollection();
        }

        public static DBObjectCollection SelectCrossPolygon(this DBObjectCollection objs, Point3dCollection pts)
        {
            var spatialIndex = new ThCADCore.NTS.ThCADCoreNTSSpatialIndex(objs);
            return spatialIndex.SelectCrossingPolygon(pts);
        }
        /// <summary>
        /// 扩展的Dispose
        /// </summary>
        /// <param name="objs"></param>
        public static void DisposeEx(this DBObjectCollection objs)
        {
            objs.OfType<Entity>().ForEach(e =>
            {
                if (!e.IsDisposed)
                {
                    e.Dispose();
                }
            });
        }
        /// <summary>
        /// 扩展的Clone
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        public static DBObjectCollection CloneEx(this DBObjectCollection objs)
        {
            return objs.OfType<Entity>().Select(e => e.Clone() as Entity).ToCollection();
        }
        public static IEnumerable<Point3d> SortPointsCCW(this IEnumerable<Point3d> points)
        {
            var center =
                Point3d.Origin +
                points.Aggregate((p1, p2) => p1 + p2.GetAsVector()).GetAsVector() / points.Count();
            return points
                .OrderBy(pt => center.GetVectorTo(pt).AngleOnPlane(new Plane()));
        }
        public static IEnumerable<Point3d> SortPointsCCW(this IEnumerable<Point3d> points, Point3d startPoint)
        {
            var center =
                Point3d.Origin +
                points.Aggregate((p1, p2) => p1 + p2.GetAsVector()).GetAsVector() / points.Count();
            var vector = center.GetVectorTo(startPoint);
            return points.OrderBy(pt => vector.GetAngleTo(center.GetVectorTo(pt), Vector3d.ZAxis));
        }
        public static double GetArea(Point2d pt1, Point2d pt2, Point2d pt3)
        {
            return (((pt2.X - pt1.X) * (pt3.Y - pt1.Y)) -
                        ((pt3.X - pt1.X) * (pt2.Y - pt1.Y))) / 2.0;
        }

        public static double GetArea(this CircularArc2d arc)
        {
            double rad = arc.Radius;
            double ang = arc.IsClockWise ?
                arc.StartAngle - arc.EndAngle :
                arc.EndAngle - arc.StartAngle;
            return rad * rad * (ang - Math.Sin(ang)) / 2.0;
        }

        public static double GetArea(this Polyline pline)
        {
            // area<0 ? "CW":"CCW"
            CircularArc2d arc = new CircularArc2d();
            double area = 0.0;
            int last = pline.NumberOfVertices - 1;
            Point2d p0 = pline.GetPoint2dAt(0);

            if (pline.GetBulgeAt(0) != 0.0)
            {
                area += pline.GetArcSegment2dAt(0).GetArea();
            }
            for (int i = 1; i < last; i++)
            {
                area += GetArea(p0, pline.GetPoint2dAt(i), pline.GetPoint2dAt(i + 1));
                if (pline.GetBulgeAt(i) != 0.0)
                {
                    area += pline.GetArcSegment2dAt(i).GetArea(); ;
                }
            }
            if ((pline.GetBulgeAt(last) != 0.0) && pline.Closed)
            {
                area += pline.GetArcSegment2dAt(last).GetArea();
            }
            return area;
        }

        /// <summary>
        /// Classify points on polyline by in or out
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="PointClass"></param>
        /// <returns></returns>
        public static Dictionary<Point3d, int> PointClassify(this Polyline polyline)
        {
            var result = new Dictionary<Point3d, int>();
            var vertexes = polyline.Vertices();
            var sorts = vertexes.OfType<Point3d>().SortPointsCCW(polyline.StartPoint).ToList();
            int n = sorts.Count;
            for (int i = 0; i < n; ++i)
            {
                var prePoint = sorts[(i + n - 1) % n];
                var curPoint = sorts[i];
                var nxtPoint = sorts[(i + 1) % n];
                if (!result.ContainsKey(curPoint))
                {
                    if (DirectionCompair(prePoint, curPoint, nxtPoint) < 0)
                    {
                        result.Add(curPoint, 1);
                    }
                    else
                    {
                        result.Add(curPoint, 2);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Compair the relation about two lines(right judge)
        /// </summary>
        /// <param name="prePoint">line A start</param>
        /// <param name="curPoint">line A end，line B start mean time</param>
        /// <param name="nxtPoint">line B end</param>
        /// <returns></returns>
        public static double DirectionCompair(Point3d prePoint, Point3d curPoint, Point3d nxtPoint)
        {
            return (curPoint.X - prePoint.X) * (nxtPoint.Y - curPoint.Y) - (nxtPoint.X - curPoint.X) * (curPoint.Y - prePoint.Y);
        }
        public static bool IsEqual(this double first, double second, double tolerance = 1e-6)
        {
            return Math.Abs(first - second) <= tolerance;
        }
        public static int Round(this double length)
        {
            return (int)Math.Floor(length + 0.5);
        }
        public static List<Tuple<Point3d, Point3d>> ToLines(this Polyline poly)
        {
            var results = new List<Tuple<Point3d, Point3d>>();
            for (int i = 0; i < poly.NumberOfVertices - 1; i++)
            {
                var st = poly.GetSegmentType(i);
                if (st == SegmentType.Line)
                {
                    var lineSeg = poly.GetLineSegmentAt(i);
                    results.Add(Tuple.Create(lineSeg.StartPoint, lineSeg.EndPoint));
                }
            }
            return results;
        }
        public static List<Line> GetLines(this Polyline poly)
        {
            return ThDrawTool.ToLines(poly);
        }
        public static Vector3d GetLineDirection(this Tuple<Point3d, Point3d> linePtPair)
        {
            return linePtPair.Item1.GetVectorTo(linePtPair.Item2).GetNormal();
        }
        public static double GetLineDistance(this Tuple<Point3d, Point3d> linePtPair)
        {
            return linePtPair.Item1.DistanceTo(linePtPair.Item2);
        }
        public static Polyline CreateRectangle(this Tuple<Point3d, Point3d> linePtPair,double width)
        {
            return ThDrawTool.ToRectangle(linePtPair.Item1, linePtPair.Item2, width);
        }
        public static Point3d? FindLinkPt(this Line first,Line second,double tolerance=1.0)
        {
            var linkPts = new List<Point3d>();
            if(first.StartPoint.DistanceTo(second.StartPoint)<= tolerance)
            {
                linkPts.Add(first.StartPoint);
            }
            if (first.StartPoint.DistanceTo(second.EndPoint) <= tolerance)
            {
                linkPts.Add(first.StartPoint);
            }
            if (first.EndPoint.DistanceTo(second.StartPoint) <= tolerance)
            {
                linkPts.Add(first.EndPoint);
            }
            if (first.EndPoint.DistanceTo(second.EndPoint) <= tolerance)
            {
                linkPts.Add(first.EndPoint);
            }
            if(linkPts.Count == 1)
            {
                return linkPts[0];
            }
            else
            {
                return null;
            }
        }
        public static DBObjectCollection GetObbFrames(this DBObjectCollection polys)
        {
            var transformer = new ThMEPOriginTransformer(polys);
            transformer.Transform(polys);
            var results =  polys
                .OfType<Polyline>()
                .Select(p => p.GetMinimumRectangle())
                .ToCollection();
            transformer.Reset(polys);
            transformer.Reset(results);
            return results;
        }
        public static bool IsValidStirrup(string stirrup)
        {
            //Z8@120,C8@120
            var newStirrup = stirrup.Trim().ToUpper();
            string pattern = @"^[ZC]{1}[\s]*\d+[\s]*[@]{1}[\s]*\d+$";
            return Regex.IsMatch(newStirrup,pattern);
        }
        public static List<int> GetStirrupDatas(string stirrup)
        {
            return IsValidStirrup(stirrup) ? GetIntegers(stirrup) : new List<int>();
        }
        public static bool IsValidLink(string link)
        {
            //1 Z 8@120,1 C 8@120
            var newLink = link.Trim().ToUpper();
            string pattern = @"^\d+[\s]*[ZC]{1}[\s]*\d+[\s]*[@]{1}[\s]*\d+$";
            return Regex.IsMatch(newLink,pattern);
        }
        public static List<int> GetLinkDatas(string link)
        {
            return IsValidLink(link) ? GetIntegers(link) : new List<int>();
        }
        public static List<int> GetIntegers(string content)
        {
            var datas = new List<int>();
            string pattern = @"\d+";
            foreach (Match item in Regex.Matches(content, pattern))
            {
                datas.Add(int.Parse(item.Value));
            }
            return datas;
        }
        public static List<double> GetDoubles(string content)
        {
            var datas = new List<double>();
            string pattern = @"\d+([.]\d+)?";
            foreach (Match item in Regex.Matches(content, pattern))
            {
                datas.Add(double.Parse(item.Value));
            }
            return datas;
        }
        /// <summary>
        /// 判断value1是否大于value2,
        /// 或value1四舍五入后是否大于value2
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <returns></returns>
        public static bool IsBiggerThan(double value1, double value2,int value1Digits)
        {
            return value1 >= value2 || Math.Round(value1, value1Digits) >= value2;
        }
        public static DBObjectCollection Clip(this Entity polygon,
            DBObjectCollection curves, bool inverted = false)
        {            
            var results = new DBObjectCollection();
            if (polygon is Polyline polyline)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(polyline, curves, inverted);
            }
            else if (polygon is MPolygon mPolygon)
            {
                results = ThCADCoreNTSGeometryClipper.Clip(mPolygon, curves, inverted);
            }
            return results.OfType<Curve>().ToCollection();
        }
        public static DBObjectCollection ExplodeLines(this DBObjectCollection curves)
        {
            return ThLaneLineEngine.Explode(curves)
                .OfType<Line>()
                .Where(o => o.Length > 0.0)
                .ToCollection();
        }
        public static void Print(this DBObjectCollection objs,Database database,string layer="0")
        {
            using (var acadDb= AcadDatabase.Use(database))
            {
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.Layer = layer;
                    e.ColorIndex = (short)ColorIndex.BYLAYER;
                    e.Linetype = "Bylayer";
                });
            }
        }
    }
}
