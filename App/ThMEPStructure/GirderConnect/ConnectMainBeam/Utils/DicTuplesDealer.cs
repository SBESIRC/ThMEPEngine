using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class DicTuplesDealer
    {
        /// <summary>
        /// 简化dicTuples结构
        /// </summary>
        public static void SimplifyDicTuples(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, List<Point3d> basePts, double deviation = 1, double angle = Math.PI / 8)
        {
            basePts = PointsDealer.PointsDistinct(basePts);
            ReduceSimilarPoints(ref dicTuples, basePts);
            ReduceSimilarLine(ref dicTuples, angle);
            DicTuplesStandardize(ref dicTuples, basePts, deviation);
        }

        /// <summary>
        /// 减少相似的点
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="basePoints"></param>
        /// <param name="tolerance"></param>net
        public static void ReduceSimilarPoints(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, List<Point3d> basePoints = null, double tolerance = 900)
        {
            var ptVisted = new HashSet<Point3d>();
            List<Point3d> pts = dicTuples.Keys.ToList();
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var ptA in pts)
            {
                if (ptVisted.Contains(ptA))
                {
                    continue;
                }
                //记录点ptA附近点所相连的点
                var closePtlist = new List<Point3d>();
                double xSum = ptA.X;
                double ySum = ptA.Y;
                ptVisted.Add(ptA);
                foreach (var cntPtA in newDicTuples[ptA])
                {
                    if (dicTuples.ContainsKey(ptA) && dicTuples[ptA].Contains(cntPtA))
                    {
                        closePtlist.Add(cntPtA);
                    }
                }
                int cnt = 1;
                foreach (var ptB in pts)
                {
                    if (ptA != ptB && ptA.DistanceTo(ptB) < tolerance && !ptVisted.Contains(ptB) && !ptVisted.Contains(ptB))
                    {
                        ptVisted.Add(ptB);
                        foreach (var cntPtB in newDicTuples[ptB])
                        {
                            if (dicTuples.ContainsKey(cntPtB) && dicTuples[cntPtB].Contains(ptB))
                            {
                                closePtlist.Add(cntPtB);
                                DeleteFromDicTuples(ptB, cntPtB, ref dicTuples);
                            }
                            if (dicTuples.ContainsKey(ptB) && dicTuples[ptB].Contains(cntPtB))
                            {
                                closePtlist.Add(cntPtB);
                                DeleteFromDicTuples(ptB, cntPtB, ref dicTuples);
                            }
                        }
                        xSum += ptB.X;
                        ySum += ptB.Y;
                        ++cnt;
                    }
                }
                if (cnt == 1)
                {
                    continue;
                }
                Point3d centerPt = new Point3d(xSum / cnt, ySum / cnt, 0);
                double minDis = double.MaxValue;
                Point3d minDisBasePt = centerPt;
                if (basePoints != null)
                {
                    foreach (var basePt in basePoints)
                    {
                        var curDis = basePt.DistanceTo(centerPt);
                        if (curDis < tolerance && curDis < minDis)
                        {
                            minDisBasePt = basePt;
                            minDis = curDis;
                        }
                    }
                }
                //将closePtlist中的所有点都换成minDisBasePt
                foreach (var closePt in closePtlist)
                {
                    AddLineTodicTuples(minDisBasePt, closePt, ref dicTuples);
                }
            }
        }

        /// <summary>
        /// Reduce Similar line to only one
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="tolerance"></param>
        public static void ReduceSimilarLine(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, double tolerance = Math.PI / 8)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                while (true)
                {
                    var key = dic.Key;
                    if (!dicTuples.ContainsKey(key))
                    {
                        break;
                    }
                    var value = dicTuples[key];
                    int n = value.Count;
                    if (n <= 1)
                    {
                        break;
                    }
                    List<Point3d> cntPts = value.ToList();
                    Vector3d baseVec = cntPts[0] - key;
                    cntPts = cntPts.OrderBy(pt => (pt - key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        if (cntPts[i % n].DistanceTo(cntPts[i - 1]) < 1.0 || key.DistanceTo(cntPts[i - 1]) < 1.0 || cntPts[i % n].DistanceTo(key) < 1.0)
                        {
                            continue;
                        }
                        curDegree = (cntPts[i % n] - key).GetAngleTo(cntPts[i - 1] - key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    if (minDegree > tolerance)
                    {
                        break;
                    }
                    Point3d rmPt = new Point3d();
                    //if ((dicTuples.ContainsKey(minDegreePairPt.Item2) && dicTuples[minDegreePairPt.Item2].Count > 1) || minDegreePairPt.Item1.DistanceTo(dic.Key) >= minDegreePairPt.Item2.DistanceTo(dic.Key))
                    if (minDegreePairPt.Item1.DistanceTo(key) >= minDegreePairPt.Item2.DistanceTo(key))
                    {
                        rmPt = minDegreePairPt.Item1;
                    }
                    else
                    {
                        rmPt = minDegreePairPt.Item2;
                    }
                    --n;
                    value.Remove(rmPt);
                    DeleteFromDicTuples(rmPt, key, ref dicTuples);
                }
            }
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
                    AddLineTodicTuples(tmpPtA, tmpPtB, ref dicTuples);
                }
            }
        }

        /// <summary>
        /// 将点的连接增加至4个
        /// </summary>
        /// <param name="dicTuples"></param>
        public static void AddConnectUpToFour(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, Point3dCollection basePts, HashSet<Point3d> itcNearPts = null, double MaxBeamLength = 13000)
        {
            double partice = Math.PI / 36;
            foreach (Point3d basePt in basePts)
            {
                if (itcNearPts == null || itcNearPts.Contains(basePt))
                {
                    continue;
                }

                if (dicTuples.ContainsKey(basePt))
                {
                    var nowCntPts = dicTuples[basePt].ToList();
                    int cnt = nowCntPts.Count;
                    if (cnt == 1)
                    {
                        var baseVector = (nowCntPts[0] - basePt).GetNormal();
                        for (int i = 1; i <= 3; ++i)
                        {
                            var ansPt = GetObject.GetPointByDirectionB(basePt, baseVector.RotateBy(Math.PI / 2 * i, Vector3d.ZAxis), basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPt, ref dicTuples);
                        }
                    }
                    else if (cnt == 2)
                    {
                        var vecA = (nowCntPts[0] - basePt).GetNormal();
                        var vecB = (nowCntPts[1] - basePt).GetNormal();
                        double baseAngel = vecA.GetAngleTo(vecB);
                        //if (baseAngel > Math.PI / 4 * 3)
                        //{
                        //}
                        //else
                        //{
                        //}
                        if (baseAngel > partice * 18 && baseAngel < partice * 22)
                        {
                            var ansPtA = GetObject.GetPointByDirectionB(basePt, -vecA, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtA, ref dicTuples);
                            var ansPtB = GetObject.GetPointByDirectionB(basePt, -vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtB, ref dicTuples);
                        }
                        else if (baseAngel > Math.PI - partice * 4)
                        {
                            var ansPtA = GetObject.GetPointByDirectionB(basePt, vecA + vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtA, ref dicTuples);
                            var ansPtB = GetObject.GetPointByDirectionB(basePt, -vecA - vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtB, ref dicTuples);
                        }
                    }
                    else if (cnt == 3)
                    {
                        var findVec = GetObject.GetDirectionByThreeVecs(basePt, nowCntPts[0], nowCntPts[1], nowCntPts[2]);
                        var ansPt = GetObject.GetPointByDirectionB(basePt, findVec, basePts, partice * 5, MaxBeamLength);
                        AddLineTodicTuples(basePt, ansPt, ref dicTuples);
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
                        AddLineTodicTuples(borderPt, nearPt, ref dicTuples);
                    }
                }
            }
        }

        /// <summary>
        /// 一堆线集中，两两相交的话删除长度较长的线
        /// </summary>
        public static void RemoveIntersectLines(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, double devision = 1.0)
        {
            var tmpTuples = LineDealer.UnifyTuples(dicTuples);
            var tuple2deduce = new Dictionary<Tuple<Point3d, Point3d>, Tuple<Point3d, Point3d>>();
            tmpTuples.ForEach(t => tuple2deduce.Add(t, LineDealer.ReduceTuple(t, 50)));
            foreach (var tupleA in tmpTuples)
            {
                var newTupA = tuple2deduce[tupleA];
                foreach (var tupleB in tmpTuples)
                {
                    //若两线相交，删除长线
                    if (tupleA == tupleB || (tupleA.Item1.DistanceTo(tupleB.Item2) < devision && tupleB.Item1.DistanceTo(tupleA.Item2) < devision)
                        || !LineDealer.IsIntersect(newTupA.Item1, newTupA.Item2, tuple2deduce[tupleB].Item1, tuple2deduce[tupleB].Item2))
                    {
                        continue;
                    }
                    if (tupleA.Item1.DistanceTo(tupleA.Item2) > tupleB.Item1.DistanceTo(tupleB.Item2))
                    {
                        DeleteFromDicTuples(tupleA.Item1, tupleA.Item2, ref dicTuples);
                    }
                    else
                    {
                        DeleteFromDicTuples(tupleB.Item1, tupleB.Item2, ref dicTuples);
                    }
                }
            }
        }

        /// <summary>
        /// 从DicTuple中删除一条双向线
        /// </summary>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <param name="dicTuples"></param>
        public static void DeleteFromDicTuples(Point3d ptA, Point3d ptB, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (dicTuples.ContainsKey(ptA) && dicTuples[ptA].Contains(ptB))
            {
                dicTuples[ptA].Remove(ptB);
                if (dicTuples[ptA].Count == 0)
                {
                    dicTuples.Remove(ptA);
                }
            }
            if (dicTuples.ContainsKey(ptB) && dicTuples[ptB].Contains(ptA))
            {
                dicTuples[ptB].Remove(ptA);
                if (dicTuples[ptB].Count == 0)
                {
                    dicTuples.Remove(ptB);
                }
            }
        }

        /// <summary>
        /// 将一条线加入字典结构
        /// </summary>
        public static void AddLineTodicTuples(Point3d ptA, Point3d ptB, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (!dicTuples.ContainsKey(ptA))
            {
                dicTuples.Add(ptA, new HashSet<Point3d>());
            }
            if (!dicTuples[ptA].Contains(ptB))
            {
                dicTuples[ptA].Add(ptB);
            }
            if (!dicTuples.ContainsKey(ptB))
            {
                dicTuples.Add(ptB, new HashSet<Point3d>());
            }
            if (!dicTuples[ptB].Contains(ptA))
            {
                dicTuples[ptB].Add(ptA);
            }
        }
        public static void AddALineTodicTuples(Point3d ptA, Point3d ptB, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (!dicTuples.ContainsKey(ptA))
            {
                dicTuples.Add(ptA, new HashSet<Point3d>());
            }
            if (!dicTuples[ptA].Contains(ptB))
            {
                dicTuples[ptA].Add(ptB);
            }
        }
    }
}
