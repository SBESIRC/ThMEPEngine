using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Linq2Acad;
using System.Collections;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADExtension;
using ThCADCore.NTS;
using AcHelper;
using DotNetARX;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.EditorInput;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.Overlay.Snap;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Triangulate;
using NetTopologySuite.LinearReferencing;
using AcHelper.Commands;
using ThMEPStructure.GirderConnect.ConnectMainBeam.ConnectProcess;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class LineDealer
    {
        /// <summary>
        /// Connect a random order line list to ordered: connect this line tail and another line start
        /// </summary>
        /// <param name="tuples">lines</param>
        public static List<Tuple<Point3d, Point3d>> OrderTuples(List<Tuple<Point3d, Point3d>> tuples, double tolerance = 1.0)
        {
            List<Tuple<Point3d, Point3d>> ansTuples = new List<Tuple<Point3d, Point3d>>();
            var tmpPt = tuples[0].Item1;
            for (int i = 0; i < tuples.Count; ++i)
            {
                foreach(var tup in tuples)
                {
                    if((tup.Item1 == tmpPt || tmpPt.DistanceTo(tup.Item1) <= tolerance) && !ansTuples.Contains(new Tuple<Point3d, Point3d>(tup.Item1, tup.Item2)))
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
        /// <param name="polyline"></param>
        /// <returns></returns>
        public static List<Tuple<Point3d, Point3d>> Polyline2Tuples(Polyline polyline, double tolerance = 1.0)
        {
            List<Tuple<Point3d, Point3d>> tuples = new List<Tuple<Point3d, Point3d>>();
            Point3d prePoint = polyline.GetPoint3dAt(0);
            for (int i = 1; i < polyline.NumberOfVertices; ++i)
            {
                Point3d curPoint = polyline.GetPoint3dAt(i);
                if (prePoint.DistanceTo(curPoint) <= tolerance)
                {
                    continue;
                }
                tuples.Add(new Tuple<Point3d, Point3d>(prePoint, curPoint));
                prePoint = curPoint;
            }
            return tuples;
        }

        /// <summary>
        /// Convert style: tuple list -> polyline
        /// </summary>
        /// <param name="tuples"></param>
        /// <returns></returns>
        public static Polyline Tuples2Polyline(List<Tuple<Point3d, Point3d>> tuples, double tolerance = 1.0)
        {
            Polyline polyline = new Polyline();
            int n = tuples.Count;
            if(n == 0)
            {
                return polyline;
            }
            else if(n == 1)
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
                foreach(var edge in edges)
                {
                    if(edge.Item1.DistanceTo(edge.Item2) <= tolerance)
                    {
                        continue;
                    }
                    polyline.AddVertexAt(cnt, new Point2d(edge.Item2.X, edge.Item2.Y), 0, 0, 0);
                    ++cnt;
                }
                polyline.Closed = true;
                return polyline;
            }
        }

        //public static int direction(Point3d a, Point3d b, Point3d c)
        //{
        //    return (int)((a.X - c.X) * (a.Y - b.Y) - (a.X -b.X) * (a.Y - c.Y));
        //}
        //public static bool onSegment(Point3d pi, Point3d pj, Point3d pk)
        //{
        //    if (Math.Min(pi.X, pj.Y) <= pk.X && pk.X <= Math.Max(pi.X, pj.X) && Math.Min(pi.Y, pj.Y) <= pk.Y && pk.Y <= Math.Max(pi.Y, pj.Y))
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        ///// <summary>
        ///// 计算两条直线是否相交
        ///// </summary>
        ///// <param name="aSt">第一条线起点</param>
        ///// <param name="aEd">第一条线终点</param>
        ///// <param name="bSt">第二条线起点</param>
        ///// <param name="bEd">第二条线终点</param>
        ///// <returns></returns>
        //public static bool IsIntersect(Point3d aSt, Point3d aEd, Point3d bSt, Point3d bEd)
        //{
        //    int d1 = direction(bSt, bEd, aSt);
        //    int d2 = direction(bSt, bEd, aEd);
        //    int d3 = direction(aSt, aEd, bSt);
        //    int d4 = direction(aSt, aEd, bEd);
        //    if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        /// <summary>
        /// Judge whether two lines are cross
        /// </summary>
        /// <param name="aSt">line A start</param>
        /// <param name="aEd">line A end</param>
        /// <param name="bSt">line B start</param>
        /// <param name="bEd">line B end</param>
        /// <returns></returns>
        public static bool IsIntersect(Point3d aSt, Point3d aEd, Point3d bSt, Point3d bEd) // copy
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

        /// <summary>
        /// Delete lines who connect with eachother with same class in a line set
        /// </summary>
        /// <param name="points">same class points</param>
        /// <param name="tuples"></param>
        public static void DeleteSameClassLine(HashSet<Point3d> points, HashSet<Tuple<Point3d, Point3d>> tuples, double deviation = 0.001)
        {
            List<Tuple<Point3d, Point3d>> tmpTuples = tuples.ToList();
            int cnt;
            foreach (var tuple in tmpTuples)
            {
                cnt = 0;
                foreach(var pt in points)
                {
                    if(pt.DistanceTo(tuple.Item1) < deviation)
                    {
                        ++cnt;
                    }
                    else if(pt.DistanceTo(tuple.Item2) < deviation)
                    {
                        ++cnt;
                    }
                }
                if(cnt >= 2)
                {
                    tuples.Remove(tuple);
                }
            }
        }
        public static void DeleteSameClassLine(HashSet<Point3d> points, Dictionary<Point3d, HashSet<Point3d>> dicTuples, double deviation = 0.001)
        {
            List<Tuple<Point3d, Point3d>> tmpTuples = new List<Tuple<Point3d, Point3d>>();
            foreach(var dic in dicTuples)
            {
                foreach(Point3d pt in dic.Value)
                {
                    tmpTuples.Add(new Tuple<Point3d, Point3d>(dic.Key, pt));
                }
            }

            int cnt;
            foreach (var tuple in tmpTuples)
            {
                cnt = 0;
                foreach (var pt in points)
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
                    dicTuples[tuple.Item1].Remove(tuple.Item2);
                    if(dicTuples[tuple.Item1].Count == 0)
                    {
                        dicTuples.Remove(tuple.Item1);
                    }
                }
            }
        }

        /// <summary>
        /// Delete lines who connect with eachother with different classes in a line set
        /// (based by tuples)
        /// </summary>
        /// <param name="ptClassA">points with class A</param>
        /// <param name="ptClassB">points with class B</param>
        /// <param name="dicTuples"></param>
        /// <param name="deviation"></param>
        public static void DeleteDiffClassLine(HashSet<Point3d> ptClassA, Point3dCollection ptClassB, Dictionary<Point3d, HashSet<Point3d>> dicTuples, double deviation = 0.001)
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
                    dicTuples[tuple.Item1].Remove(tuple.Item2);
                    if (dicTuples[tuple.Item1].Count == 0)
                    {
                        dicTuples.Remove(tuple.Item1);
                    }
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
                    dicTuples[tuple.Item1].Remove(tuple.Item2);
                    if (dicTuples[tuple.Item1].Count == 0)
                    {
                        dicTuples.Remove(tuple.Item1);
                    }
                }
            }
        }

        /// <summary>
        /// Merge very close points to one whithout change structure
        /// </summary>
        /// <param name="outline2BorderNearPts"></param>
        /// <param name="outPts">constrains & assists</param>
        public static void SimplifyLineConnect(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, List<Point3d> outPts)
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
                                minDis = curDis < 600 ? curDis : 600; //changable parameter
                            }
                            curDis = borderPtB.DistanceTo(outPt);
                            if (curDis < minDis)
                            {
                                stayPt = borderPtB;
                                deletePt = borderPtA;
                                minDis = curDis < 600 ? curDis : 600; //changable parameter
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
                                //borderPt2NearPts[stayPt].AddRange(borderPt2NearPts[deletePt]);
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
        /// <param name="outline2BorderNearPts">from</param>
        /// <param name="tuples">to</param>
        public static void AddSpecialLine(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, HashSet<Tuple<Point3d, Point3d>> tuples)
        {
            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt2NearPt in borderPt2NearPts)
                {
                    var borderPt = borderPt2NearPt.Key;
                    foreach (var nearPt in borderPt2NearPt.Value) //loops no more than three times
                    {
                        tuples.Add(new Tuple<Point3d, Point3d>(borderPt, nearPt));
                        tuples.Add(new Tuple<Point3d, Point3d>(nearPt, borderPt));
                        ShowInfo.DrawLine(borderPt, nearPt, 210);
                        ShowInfo.DrawLine(nearPt, borderPt, 210);
                    }
                }
            }
        }
        public static void AddSpecialLine(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            List<Tuple<Point3d, Point3d>> tmpTuples = new List<Tuple<Point3d, Point3d>>();
            foreach (var dic in dicTuples)
            {
                foreach (Point3d pt in dic.Value)
                {
                    tmpTuples.Add(new Tuple<Point3d, Point3d>(dic.Key, pt));
                }
            }
            foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
            {
                foreach (var borderPt2NearPt in borderPt2NearPts)
                {
                    var borderPt = borderPt2NearPt.Key;
                    foreach (var nearPt in borderPt2NearPt.Value) //loops no more than three times
                    {
                        if (!dicTuples.ContainsKey(borderPt))
                        {
                            dicTuples.Add(borderPt, new HashSet<Point3d>());
                        }
                        dicTuples[borderPt].Add(nearPt);

                        if (!dicTuples.ContainsKey(nearPt))
                        {
                            dicTuples.Add(nearPt, new HashSet<Point3d>());
                        }
                        dicTuples[nearPt].Add(borderPt);
                    }
                }
            }
        }

        /// <summary>
        /// Merge points whose very close to each other in double line structure
        /// </summary>
        /// <param name="tuples"> 原始数据（可能有误差）</param>
        /// <param name="basePts"> 基于这些点 </param>
        /// <param name="deviation"> 误差</param>
        public static Dictionary<Point3d, HashSet<Point3d>> TuplesStandardize(HashSet<Tuple<Point3d, Point3d>> tuples, Point3dCollection basePts, double deviation = 0.001)
        {
            Dictionary<Point3d, HashSet<Point3d>> dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            Point3d tmpPtA = new Point3d();
            Point3d tmpPtB = new Point3d();
            foreach (var tuple in tuples)
            {
                int flag = 0;
                foreach(Point3d ptA in basePts)
                {
                    if(ptA.DistanceTo(tuple.Item1) < deviation)
                    {
                        flag = 1;
                        tmpPtA = ptA;
                        break;
                    }
                }
                if(flag == 0)//profs there is no nearest point
                {
                    tmpPtA = tuple.Item1;
                }
                if (!dicTuples.ContainsKey(tmpPtA))
                {
                    dicTuples.Add(tmpPtA, new HashSet<Point3d>());
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
                //foreach(var ptinA in dicTuples[tmpPtA])
                //{
                //    if(ptinA.DistanceTo(tuple.Item2) < deviation)
                //    {
                //        flag = 3;
                //        break;
                //    }
                //}
                //if(flag == 3)
                //{
                //    continue;
                //}
                if (flag == 0)//no nearest point
                {
                    tmpPtB = tuple.Item2;
                }
                if (!dicTuples[tmpPtA].Contains(tmpPtB))
                {
                    dicTuples[tmpPtA].Add(tmpPtB);
                }
            }
            return dicTuples;
        }

        /// <summary>
        /// Points of Entity intersect with Entity
        /// </summary>
        /// <param name="firstEntity"></param>
        /// <param name="secondEntity"></param>
        /// <returns></returns>
        public static Point3dCollection IntersectWith(Entity firstEntity, Entity secondEntity) // copy
        {
            Point3dCollection pts = new Point3dCollection();
            Plane plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            firstEntity.IntersectWith(secondEntity, Intersect.OnBothOperands, plane, pts, IntPtr.Zero, IntPtr.Zero);
            plane.Dispose();
            return pts;
        }
    }
}
