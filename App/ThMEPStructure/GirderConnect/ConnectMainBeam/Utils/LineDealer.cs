using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class LineDealer
    {
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
        /// Judge whether two lines are cross
        /// </summary>
        /// <param name="aSt">line A start</param>
        /// <param name="aEd">line A end</param>
        /// <param name="bSt">line B start</param>
        /// <param name="bEd">line B end</param>
        /// <returns></returns>
        public static bool IsIntersect(Point3d aSt, Point3d aEd, Point3d bSt, Point3d bEd)
        {
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

        public static bool IsIntersectB(Point3d aSt, Point3d aEd, Point3d bSt, Point3d bEd)
        {
            if (Math.Max(aSt.X, aEd.X) < Math.Min(bSt.X, bEd.X)) return false;
            if (Math.Max(aSt.Y, aEd.Y) < Math.Min(bSt.Y, bEd.Y)) return false;
            if (Math.Max(bSt.X, bEd.X) < Math.Min(aSt.X, aEd.X)) return false;
            if (Math.Max(bSt.Y, bEd.Y) < Math.Min(aSt.Y, aEd.Y)) return false;
            if (Mult(bSt.X, bSt.Y, aEd.X, aEd.Y, aSt.X, aSt.Y) * Mult(aEd.X, aEd.Y, bEd.X, bEd.Y, aSt.X, aSt.Y) < 0) return false;
            if (Mult(aSt.X, aSt.Y, bEd.X, bEd.Y, bSt.X, bSt.Y) * Mult(bEd.X, bEd.Y, aEd.X, aEd.Y, bSt.X, bSt.Y) < 0) return false;
            return true;
        }

        public static double Mult(double x1, double y1, double x2, double y2, double x3, double y3)   //计算叉乘 
        {
            return (x1 - x3) * (y2 - y3) - (x2 - x3) * (y1 - y3);
        }


        /// <summary>
        /// Delete lines who connect with eachother with same class in a line set
        /// </summary>
        public static void DeleteSameClassLine(HashSet<Point3d> points, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, double deviation = 1)
        {
            HashSet<Point3d> pts = new HashSet<Point3d>();
            HashSet<Point3d> classPts = new HashSet<Point3d>();
            foreach (var pt in points.ToList())
            {
                if (!pts.Contains(pt))
                {
                    pts.Add(pt);
                    classPts.Add(pt);
                }
                foreach (var ptB in classPts)
                {
                    if (pts.Contains(ptB))
                    {
                        continue;
                    }
                    if (ptB.DistanceTo(pt) < deviation)
                    {
                        pts.Add(ptB);
                    }
                }
            }

            List<Tuple<Point3d, Point3d>> tmpTuples = new List<Tuple<Point3d, Point3d>>();
            foreach (var dic in dicTuples)
            {
                foreach (Point3d pt in dic.Value)
                {
                    tmpTuples.Add(new Tuple<Point3d, Point3d>(dic.Key, pt));
                }
            }

            int cnt;
            foreach (var tuple in tmpTuples)
            {
                cnt = 0;
                foreach (var pt in classPts)
                {
                    if (pt.DistanceTo(tuple.Item1) < deviation)
                    {
                        ++cnt;
                    }
                    else if (pt.DistanceTo(tuple.Item2) < deviation)
                    {
                        ++cnt;
                    }
                }
                if (cnt >= 2)
                {
                    StructureDealer.DeleteFromDicTuples(tuple.Item1, tuple.Item2, ref dicTuples);
                }
            }
        }

        /// <summary>
        /// Delete lines who connect with eachother with different classes in a line set
        /// (based by tuples)
        /// </summary>
        public static void DeleteDiffClassLine(HashSet<Point3d> ptClassA, Point3dCollection ptClassB, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, double deviation = 0.001)
        {
            List<Tuple<Point3d, Point3d>> tmpTuples = new List<Tuple<Point3d, Point3d>>();
            foreach (var dic in dicTuples)
            {
                foreach (Point3d pt in dic.Value)
                {
                    tmpTuples.Add(new Tuple<Point3d, Point3d>(dic.Key, pt));
                }
            }
            int cnt;
            //delete line from A to B
            foreach (var tuple in tmpTuples)
            {
                cnt = 0;
                foreach (Point3d pt in ptClassA)
                {
                    if (pt.DistanceTo(tuple.Item1) < deviation)
                    {
                        ++cnt;
                    }
                }
                foreach (Point3d pt in ptClassB)
                {
                    if (pt.DistanceTo(tuple.Item2) < deviation)
                    {
                        ++cnt;
                    }
                }
                if (cnt >= 2)
                {
                    StructureDealer.DeleteFromDicTuples(tuple.Item1, tuple.Item2, ref dicTuples);
                }
            }
            //delete line from B to A
            foreach (var tuple in tmpTuples)
            {
                cnt = 0;
                foreach (Point3d pt in ptClassB)
                {
                    if (pt.DistanceTo(tuple.Item1) < deviation)
                    {
                        ++cnt;
                    }
                }
                foreach (Point3d pt in ptClassA)
                {
                    if (pt.DistanceTo(tuple.Item2) < deviation)
                    {
                        ++cnt;
                    }
                }
                if (cnt >= 2)
                {
                    StructureDealer.DeleteFromDicTuples(tuple.Item1, tuple.Item2, ref dicTuples);
                }
            }
        }

        /// <summary>
        /// Merge very close points to one whithout change structure
        /// </summary>
        public static void SimplifyLineConnect(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, List<Point3d> outPts, double tolerance = 600)
        {
            List<Point3d> borderPts = new List<Point3d>();
            foreach (var dic in outline2BorderNearPts.Values)
            {
                borderPts.AddRange(dic.Keys.ToList());
            }
            double minDis = double.MaxValue;
            double curDis;
            Point3d stayPt = new Point3d();
            Point3d deletePt = new Point3d();
            foreach (var borderPtA in borderPts)
            {
                foreach (var borderPtB in borderPts)
                {
                    if (borderPtA.X != borderPtB.X && borderPtA.Y != borderPtB.Y && borderPtA.DistanceTo(borderPtB) < 400) //changable parameter
                    {
                        foreach (var outPt in outPts)
                        {
                            curDis = borderPtA.DistanceTo(outPt);
                            if (curDis < minDis)
                            {
                                stayPt = borderPtA;
                                deletePt = borderPtB;
                                minDis = curDis < tolerance ? curDis : tolerance; //changable parameter
                            }
                            curDis = borderPtB.DistanceTo(outPt);
                            if (curDis < minDis)
                            {
                                stayPt = borderPtB;
                                deletePt = borderPtA;
                                minDis = curDis < tolerance ? curDis : tolerance; //changable parameter
                            }
                        }
                        foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                        {
                            if (borderPt2NearPts.ContainsKey(stayPt) && borderPt2NearPts.ContainsKey(deletePt))
                            {
                                foreach (var pt in borderPt2NearPts[deletePt])
                                {
                                    borderPt2NearPts[stayPt].Add(pt);
                                }
                                borderPt2NearPts.Remove(deletePt);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add lines to a line set who does not have them brfore
        /// </summary>
        public static void AddSpecialLine(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt2NearPt in borderPt2NearPts)
                {
                    var borderPt = borderPt2NearPt.Key;
                    foreach (var nearPt in borderPt2NearPt.Value)
                    {
                        StructureDealer.AddLineTodicTuples(borderPt, nearPt, ref dicTuples);
                    }
                }
            }
        }

        /// <summary>
        /// Points of Entity intersect with Entity
        /// <param name="firstEntity"></param>
        /// <param name="secondEntity"></param>
        /// <returns></returns>
        public static Point3dCollection IntersectWith(Entity firstEntity, Entity secondEntity)
        {
            Point3dCollection pts = new Point3dCollection();
            Plane plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            firstEntity.IntersectWith(secondEntity, Intersect.OnBothOperands, plane, pts, IntPtr.Zero, IntPtr.Zero);
            plane.Dispose();
            return pts;
        }

        /// <summary>
        /// Merge points whose very close to each other in double line structure
        /// </summary>
        /// <param name="tuples"> 原始数据（可能有误差）</param>
        /// <param name="basePts"> 基于这些点 </param>
        /// <param name="deviation"> 误差</param>
        public static Dictionary<Point3d, HashSet<Point3d>> TuplesStandardize(HashSet<Tuple<Point3d, Point3d>> tuples, List<Point3d> basePts, double deviation = 1)
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
                StructureDealer.AddLineTodicTuples(tmpPtA, tmpPtB, ref dicTuples);
            }
            return dicTuples;
        }

        /// <summary>
        /// Merge points whose very close to each other in double line structure
        /// If there is a single line, add a compair one
        /// </summary>
        /// <param name="dicTuples"> 原始数据（可能有误差）</param>
        /// <param name="basePts"> 基于这些点 </param>
        /// <param name="deviation"> 误差</param>
        public static void DicTuplesStandardize(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, List<Point3d> basePts, double deviation = 1)
        {
            Point3d tmpPtA = new Point3d();
            Point3d tmpPtB = new Point3d();
            var newDicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value);
            }
            dicTuples.Clear();
            foreach (var dicTuple in newDicTuples)
            {
                var hashpts = dicTuple.Value.ToList();
                foreach (var edPt in hashpts)
                {
                    int flag = 0;
                    foreach (Point3d ptA in basePts)
                    {
                        if (ptA.DistanceTo(dicTuple.Key) < deviation)
                        {
                            flag = 1;
                            tmpPtA = ptA;
                            break;
                        }
                    }
                    if (flag == 0)//profs there is no nearest point
                    {
                        tmpPtA = dicTuple.Key;
                    }
                    flag = 0;
                    foreach (Point3d ptB in basePts)
                    {
                        if (ptB.DistanceTo(edPt) < deviation)
                        {
                            flag = 1;
                            tmpPtB = ptB;
                            break;
                        }
                    }
                    if (flag == 0)//no nearest point
                    {
                        tmpPtB = edPt;
                    }
                    StructureDealer.AddLineTodicTuples(tmpPtA, tmpPtB, ref dicTuples);
                }
            }
        }

        /// <summary>
        /// Get AngleCount which is bigger than 180
        /// </summary>
        public static int ObtuseAngleCount(List<Tuple<Point3d, Point3d>> tuples, double tolerance = Math.PI / 18 * 19)
        {
            int cnt = 0;
            int n = tuples.Count;
            if (n <= 2)
            {
                return -1;
            }
            var preVertex = tuples[n - 1].Item1 - tuples[n - 1].Item2;
            for (int i = 0; i < n; ++i)
            {
                double curAngel = preVertex.GetAngleTo((tuples[i].Item2 - tuples[i].Item1), -Vector3d.ZAxis);
                if (curAngel > tolerance)
                {
                    ++cnt;
                }
                preVertex = tuples[i].Item1 - tuples[i].Item2;
            }
            return cnt;
        }

        /// <summary>
        /// 获得一个多变形中最大的角的度数（均小于180）
        /// 判断最大角，也就是与150°比较的角时，不包括与辅助线有关的夹角
        /// </summary>
        /// <param name="tuples"></param>
        /// <returns></returns>
        public static double GetBiggestAngel(List<Tuple<Point3d, Point3d>> tuples, HashSet<Point3d> borderPts)
        {
            double biggestDegree = -1;
            int n = tuples.Count;
            if (n <= 2)
            {
                return -1;
            }
            var preVertex = tuples[n - 1].Item1 - tuples[n - 1].Item2;
            for (int i = 0; i < n; ++i)
            {
                double curDegree = preVertex.GetAngleTo(tuples[i].Item2 - tuples[i].Item1, -Vector3d.ZAxis);
                preVertex = tuples[i].Item1 - tuples[i].Item2;
                if (curDegree > Math.PI / 18 * 17)
                {
                    return -1;
                }
                if ((borderPts.Contains(tuples[i].Item1) && borderPts.Contains(tuples[i].Item2)) || (borderPts.Contains(tuples[(i + n - 1) % n].Item1) && borderPts.Contains(tuples[(i + n - 1) % n].Item2)))
                {
                    continue;
                }
                if (curDegree > Math.PI / 6 * 5)
                {
                    return -1;
                }
                if (curDegree > biggestDegree)
                {
                    biggestDegree = curDegree;
                }
            }
            return biggestDegree;
        }

        public static HashSet<Tuple<Point3d, Point3d>> DicTuplesToTuples(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
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

        /// <summary>
        /// DCEL的双向线转换为单线
        /// </summary>
        public static HashSet<Tuple<Point3d, Point3d>> UnifyTuples(Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var ansTuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var kv in dicTuples)
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

        public static Tuple<Point3d, Point3d> ReduceTuple(Tuple<Point3d, Point3d> tuple, double length)
        {
            var direction = (tuple.Item2 - tuple.Item1).GetNormal();
            return new Tuple<Point3d, Point3d>(tuple.Item1 + direction * length, tuple.Item2 - direction * length);
        }
        public static Line ReduceTupleB(Tuple<Point3d, Point3d> tuple, double length)
        {
            var direction = (tuple.Item2 - tuple.Item1).GetNormal();
            return new Line(tuple.Item1 + direction * length, tuple.Item2);
        }
        public static Line ReduceLine(Line line, double length)
        {
            var direction = (line.EndPoint - line.StartPoint).GetNormal();
            return new Line(line.StartPoint + direction * length, line.EndPoint - direction * length);
        }
    }
}
