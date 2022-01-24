using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class TypeConvertor
    {
        /// <summary>
        /// Convert style: polyline -> tuple list
        /// </summary>
        public static List<Tuple<Point3d, Point3d>> Polyline2Tuples(Polyline polyline, double tolerance = 1.0)
        {
            List<Tuple<Point3d, Point3d>> tuples = new List<Tuple<Point3d, Point3d>>();
            Point3d prePoint = polyline.GetPoint3dAt(0);
            int n = polyline.NumberOfVertices;
            for (int i = 1; i < n; ++i)
            {
                Point3d curPoint = polyline.GetPoint3dAt(i);
                if (prePoint.DistanceTo(curPoint) <= tolerance)
                {
                    continue;
                }
                tuples.Add(new Tuple<Point3d, Point3d>(prePoint, curPoint));
                prePoint = curPoint;
            }
            if (polyline.GetPoint3dAt(0) != polyline.GetPoint3dAt(n - 1))
            {
                tuples.Add(new Tuple<Point3d, Point3d>(polyline.GetPoint3dAt(n - 1), polyline.GetPoint3dAt(0)));
            }
            return tuples;
        }
        public static List<Line> Polyline2Lines(Polyline polyline, double tolerance = 1.0)
        {
            List<Line> lines = new List<Line>();
            Point3d prePoint = polyline.GetPoint3dAt(0);
            int n = polyline.NumberOfVertices;
            for (int i = 1; i < n; ++i)
            {
                Point3d curPoint = polyline.GetPoint3dAt(i);
                if (prePoint.DistanceTo(curPoint) <= tolerance)
                {
                    continue;
                }
                lines.Add(new Line(prePoint, curPoint));
                prePoint = curPoint;
            }
            if (polyline.GetPoint3dAt(0) != polyline.GetPoint3dAt(n - 1))
            {
                lines.Add(new Line(polyline.GetPoint3dAt(n - 1), polyline.GetPoint3dAt(0)));
            }
            return lines;
        }

        /// <summary>
        /// Convert style: tuple list -> polyline
        /// </summary>
        public static Polyline Tuples2Polyline(List<Tuple<Point3d, Point3d>> tuples, double tolerance = 1.0)
        {
            Polyline polyline = new Polyline();
            int n = tuples.Count;
            if (n == 0)
            {
                return polyline;
            }
            else if (n == 1)
            {
                polyline.AddVertexAt(0, new Point2d(tuples[0].Item1.X, tuples[0].Item1.Y), 0, 0, 0);
                polyline.AddVertexAt(1, new Point2d(tuples[0].Item2.X, tuples[0].Item2.Y), 0, 0, 0);
                return polyline;
            }
            else
            {
                var edges = LineDealer.OrderTuples(tuples);
                polyline.AddVertexAt(0, new Point2d(tuples[0].Item1.X, tuples[0].Item1.Y), 0, 0, 0);
                int cnt = 1;
                foreach (var edge in edges)
                {
                    if (edge.Item1.DistanceTo(edge.Item2) <= tolerance)
                    {
                        continue;
                    }
                    polyline.AddVertexAt(cnt, new Point2d(edge.Item2.X, edge.Item2.Y), 0, 0, 0);
                    ++cnt;
                }
                if (polyline.GetPoint3dAt(0).DistanceTo(polyline.GetPoint3dAt(cnt - 1)) < 10)
                {
                    polyline.Closed = true;
                }
                return polyline;
            }
        }

        /// <summary>
        /// Merge points whose very close to each other in double line structure
        /// </summary>
        /// <param name="tuples"> 原始数据（可能有误差）</param>
        /// <param name="basePts"> 基于这些点 </param>
        /// <param name="deviation"> 误差</param>
        public static Dictionary<Point3d, HashSet<Point3d>> Tuples2DicTuples(HashSet<Tuple<Point3d, Point3d>> tuples, List<Point3d> basePts, double deviation = 1)
        {
            Dictionary<Point3d, HashSet<Point3d>> dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            Point3d tmpPtA = new Point3d();
            Point3d tmpPtB = new Point3d();
            foreach (var tuple in tuples)
            {
                int flag = 0;
                foreach (Point3d ptA in basePts)
                {
                    if (ptA.DistanceTo(tuple.Item1) < deviation)
                    {
                        flag = 1;
                        tmpPtA = ptA;
                        break;
                    }
                }
                if (flag == 0)
                {
                    tmpPtA = tuple.Item1;
                }

                flag = 0;
                foreach (Point3d ptB in basePts)
                {
                    if (ptB.DistanceTo(tuple.Item2) < deviation)
                    {
                        flag = 1;
                        tmpPtB = ptB;
                        break;
                    }
                }
                if (flag == 0)
                {
                    tmpPtB = tuple.Item2;
                }
                DicTuplesDealer.AddLineTodicTuples(tmpPtA, tmpPtB, ref dicTuples);
            }
            return dicTuples;
        }

        public static HashSet<Tuple<Point3d, Point3d>> DicTuples2Tuples(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            HashSet<Tuple<Point3d, Point3d>> tuples = new HashSet<Tuple<Point3d, Point3d>>();
            if (dicTuples.IsNull())
            {
                return tuples;
            }
            foreach (var dicTuple in dicTuples)
            {
                foreach (var point in dicTuple.Value)
                {
                    tuples.Add(new Tuple<Point3d, Point3d>(dicTuple.Key, point));
                }
            }
            return tuples;
        }

        public static Dictionary<Point3d, HashSet<Point3d>> Lines2Tuples(List<Line> lines)
        {
            Dictionary<Point3d, HashSet<Point3d>> pt2Pts = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var line in lines)
            {
                var stPt = line.StartPoint;
                var edPt = line.EndPoint;
                if (!pt2Pts.ContainsKey(stPt))
                {
                    pt2Pts.Add(stPt, new HashSet<Point3d>());
                }
                if (!pt2Pts[stPt].Contains(edPt))
                {
                    pt2Pts[stPt].Add(edPt);
                }
                if (!pt2Pts.ContainsKey(edPt))
                {
                    pt2Pts.Add(edPt, new HashSet<Point3d>());
                }
                if (!pt2Pts[edPt].Contains(stPt))
                {
                    pt2Pts[edPt].Add(stPt);
                }
            }
            return pt2Pts;
        }
    }
}
