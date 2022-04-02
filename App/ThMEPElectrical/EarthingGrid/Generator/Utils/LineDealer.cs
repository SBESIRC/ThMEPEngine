using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class LineDealer
    {
        /// <summary>
        /// DCEL的双向线转换为单线
        /// </summary>
        public static HashSet<Tuple<Point3d, Point3d>> UnifyTuples(Dictionary<Point3d, HashSet<Point3d>> graph)
        {
            var ansTuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var kv in graph)
            {
                var ptSet = kv.Value;
                foreach (var pt in ptSet)
                {
                    if (kv.Key.DistanceTo(pt) <= 10) continue;
                    var positiveTuple = new Tuple<Point3d, Point3d>(kv.Key, pt);
                    var negativeTuple = new Tuple<Point3d, Point3d>(pt, kv.Key);
                    if (!ansTuples.Contains(positiveTuple) && !ansTuples.Contains(negativeTuple))
                    {
                        ansTuples.Add(positiveTuple);
                    }
                }
            }
            return ansTuples;
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
                var edges = OrderTuples(tuples);
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

        /// <summary>
        /// Judge whether two lines are cross
        /// </summary>
        /// <param name="aSt">line A start</param>
        /// <param name="aEd">line A end</param>
        /// <param name="bSt">line B start</param>
        /// <param name="bEd">line B end</param>
        /// <returns></returns>
        public static bool IsIntersect(Point3d aSt, Point3d aEd, Point3d bSt, Point3d bEd)
        {
            //快速排斥
            if (Math.Max(aSt.X, aEd.X) < Math.Min(bSt.X, bEd.X)) return false;
            if (Math.Max(aSt.Y, aEd.Y) < Math.Min(bSt.Y, bEd.Y)) return false;
            if (Math.Max(bSt.X, bEd.X) < Math.Min(aSt.X, aEd.X)) return false;
            if (Math.Max(bSt.Y, bEd.Y) < Math.Min(aSt.Y, aEd.Y)) return false;
            //相交测试
            double EPS3 = 1.0e-3f;
            var dir = bEd - bSt;
            var dValue = (aSt - bSt).CrossProduct(dir).Z * (aEd - bSt).CrossProduct(dir).Z;
            if (Math.Abs(dValue) > EPS3 && dValue < 0)
            {
                dir = aEd - aSt;
                dValue = (bSt - aSt).CrossProduct(dir).Z * (bEd - aSt).CrossProduct(dir).Z;
                return Math.Abs(dValue) > EPS3 && dValue < 0;
            }
            return false;
        }


        /// <summary>
        /// 在线集中删除掉穿过Outline过长的线
        /// </summary>
        public static Dictionary<Point3d, HashSet<Point3d>> RemoveLineIntersectWithOutline(List<Polyline> allOutlines,
           ref Dictionary<Point3d, HashSet<Point3d>> nearBorderGraph, double intersectLength)
        {
            var dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var pt2pts in nearBorderGraph)
            {
                foreach (var ptB in pt2pts.Value)
                {
                    bool flag = false;
                    var line = ReduceLine(pt2pts.Key, ptB, intersectLength);
                    foreach (var outline in allOutlines)
                    {
                        if (line.Intersect(outline, 0).Count > 0)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag == true)
                    {
                        continue;
                    }
                    GraphDealer.AddLineToGraph(pt2pts.Key, ptB, ref dicTuples);
                }
            }
            return dicTuples;
        }
        public static void RemoveLinesInterSectWithOutlines(List<Polyline> outlines, ref Dictionary<Point3d, HashSet<Point3d>> graph, double intersectLength)
        {
            var tmpTuples = UnifyTuples(graph);
            var tuple2reduce = new Dictionary<Tuple<Point3d, Point3d>, Tuple<Point3d, Point3d>>();
            tmpTuples.ForEach(t => { if (t.Item1.DistanceTo(t.Item2) > 2300) tuple2reduce.Add(t, ReduceTuple(t, intersectLength)); });
            foreach (var tmpTuple in tmpTuples)
            {
                if (tuple2reduce.ContainsKey(tmpTuple))
                {
                    Line reducedLine = new Line(tuple2reduce[tmpTuple].Item1, tuple2reduce[tmpTuple].Item2);
                    //Point3d middlePt = new Point3d((tuple2reduce[tmpTuple].Item1.X + tuple2reduce[tmpTuple].Item2.X) / 2, (tuple2reduce[tmpTuple].Item1.Y + tuple2reduce[tmpTuple].Item2.Y) / 2, 0);
                    //Circle circle = new Circle(middlePt, Vector3d.ZAxis, 1000);
                    foreach (var outline in outlines)
                    {
                        //if (outline.Intersects(reducedLine) || outline.Intersects(circle))
                        if (outline.Intersects(reducedLine))
                        {
                            GraphDealer.DeleteFromGraph(tmpTuple.Item1, tmpTuple.Item2, ref graph);
                        }
                    }
                }
            }
        }

        public static Tuple<Point3d, Point3d> ReduceTuple(Tuple<Point3d, Point3d> tuple, double length)
        {
            var direction = (tuple.Item2 - tuple.Item1).GetNormal();
            return new Tuple<Point3d, Point3d>(tuple.Item1 + direction * length, tuple.Item2 - direction * length);
        }
        public static Line ReduceLine(Point3d pta, Point3d ptb, double length)
        {
            var direction = (ptb - pta).GetNormal();
            return new Line(pta + direction * length, ptb - direction * length);
        }

        /// <summary>
        /// Connect a random order line list to ordered: connect this line tail and another line start
        /// </summary>
        public static List<Tuple<Point3d, Point3d>> OrderTuples(List<Tuple<Point3d, Point3d>> tuples, double tolerance = 1.0)
        {
            if (tuples.Count == 0)
            {
                return new List<Tuple<Point3d, Point3d>>();
            }
            List<Tuple<Point3d, Point3d>> ansTuples = new List<Tuple<Point3d, Point3d>>();
            var tmpPt = tuples[0].Item1;
            for (int i = 0; i < tuples.Count; ++i)
            {
                foreach (var tup in tuples)
                {
                    if ((tup.Item1 == tmpPt || tmpPt.DistanceTo(tup.Item1) <= tolerance) && !ansTuples.Contains(new Tuple<Point3d, Point3d>(tup.Item1, tup.Item2)))
                    {
                        ansTuples.Add(new Tuple<Point3d, Point3d>(tup.Item1, tup.Item2));
                        tmpPt = tup.Item2;
                        break;
                    }
                }
            }
            return ansTuples;
        }

        public static HashSet<Tuple<Point3d, Point3d>> Graph2Lines(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
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
                    if(!tuples.Contains(new Tuple<Point3d, Point3d>(point, dicTuple.Key)))
                    {
                        tuples.Add(new Tuple<Point3d, Point3d>(dicTuple.Key, point));
                    }
                }
            }
            return tuples;
        }
    }
}
