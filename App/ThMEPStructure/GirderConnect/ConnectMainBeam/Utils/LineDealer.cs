using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
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
        /// Judge whether two lines are cross
        /// </summary>
        /// <param name="aSt">line A start</param>
        /// <param name="aEd">line A end</param>
        /// <param name="bSt">line B start</param>
        /// <param name="bEd">line B end</param>
        /// <returns></returns>
        public static bool IsIntersect(Point3d aSt, Point3d aEd, Point3d bSt, Point3d bEd)
        {
            if (Math.Max(aSt.X, aEd.X) < Math.Min(bSt.X, bEd.X)) return false;
            if (Math.Max(aSt.Y, aEd.Y) < Math.Min(bSt.Y, bEd.Y)) return false;
            if (Math.Max(bSt.X, bEd.X) < Math.Min(aSt.X, aEd.X)) return false;
            if (Math.Max(bSt.Y, bEd.Y) < Math.Min(aSt.Y, aEd.Y)) return false;
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
                    DicTuplesDealer.DeleteFromDicTuples(tuple.Item1, tuple.Item2, ref dicTuples);
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
                    DicTuplesDealer.DeleteFromDicTuples(tuple.Item1, tuple.Item2, ref dicTuples);
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
                    DicTuplesDealer.DeleteFromDicTuples(tuple.Item1, tuple.Item2, ref dicTuples);
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

        /// <summary>
        /// 在线集中删除掉穿过Outline过长的线
        /// </summary>
        public static Dictionary<Point3d, HashSet<Point3d>> RemoveLineIntersectWithOutline(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts,
           ref List<Tuple<Point3d, Point3d>> priority1stBorderNearTuples, double intersectLength)
        {
            var dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            var borderToNearLines = new List<Tuple<Point3d, Point3d>>();
            outline2BorderNearPts.Values.ForEach(o => o.ForEach(kv => kv.Value.ForEach(pt => borderToNearLines.Add(new Tuple<Point3d, Point3d>(kv.Key, pt)))));
            HashSet<Polyline> outlines = outline2BorderNearPts.Keys.ToHashSet();
            foreach (var borderToNearLine in borderToNearLines)
            {
                bool flag = false;
                var line = ReduceTupleB(borderToNearLine, intersectLength);
                foreach (var outline in outlines)
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
                DicTuplesDealer.AddLineTodicTuples(borderToNearLine.Item1, borderToNearLine.Item2, ref dicTuples);
            }
            foreach (var borderToNearLine in priority1stBorderNearTuples)
            {
                bool flag = false;
                var line = ReduceTupleB(borderToNearLine, intersectLength);
                foreach (var outline in outlines)
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
                DicTuplesDealer.AddLineTodicTuples(borderToNearLine.Item1, borderToNearLine.Item2, ref dicTuples);
            }
            return dicTuples;
        }

        /// <summary>
        /// 剪短一条线
        /// </summary>
        public static Tuple<Point3d, Point3d> ReduceTuple(Tuple<Point3d, Point3d> tuple, double length)
        {
            var direction = (tuple.Item2 - tuple.Item1).GetNormal();
            return new Tuple<Point3d, Point3d>(tuple.Item1 + direction * length, tuple.Item2 - direction * length);
        }
        public static Line ReduceLine(Line line, double length)
        {
            var direction = (line.EndPoint - line.StartPoint).GetNormal();
            return new Line(line.StartPoint + direction * length, line.EndPoint - direction * length);
        }
        public static Line ReduceLine(Point3d ptA, Point3d ptB, double length)
        {
            var direction = (ptB - ptA).GetNormal();
            return new Line(ptA + direction * length, ptB - direction * length);
        }
        public static Line ReduceTupleB(Tuple<Point3d, Point3d> tuple, double length)
        {
            var direction = (tuple.Item2 - tuple.Item1).GetNormal();
            return new Line(tuple.Item1 + direction * length, tuple.Item2);
        }
    }
}
