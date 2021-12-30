using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThCADCore.NTS;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class StructureDealer
    {
        /// <summary>
        /// Split Polyline
        /// </summary>
        /// <param name="tuples">the polyline will be split</param>
        /// <returns>a list of polylines splited</returns>
        public static void SplitPolyline(List<Tuple<Point3d, Point3d>> tuples, ref List<List<Tuple<Point3d, Point3d>>> tupleLines)
        {
            //Recursion boundary
            int n = tuples.Count;
            if (n == 0 || n > 20)
            {
                return;
            }
            if (n <= 5)
            {
                if (LineDealer.Tuples2Polyline(tuples).Closed == true)
                {
                    //ShowInfo.ShowGeometry(LineDealer.Tuples2Polyline(tuples).ToNTSGeometry(), 3);
                    tupleLines.Add(tuples);
                }
                return;
            }

            //Initialization
            tuples = LineDealer.OrderTuples(tuples);
            n = tuples.Count;
            int halfCnt = n / 2;
            var tuplesA = new List<Tuple<Point3d, Point3d>>();
            var tuplesB = new List<Tuple<Point3d, Point3d>>();
            int splitA;
            int splitB;
            int flag;
            double mindis = double.MaxValue;
            double curdis;

            //Catulate
            //find best split
            int loopCnt = (n & 1) == 1 ? n : (n / 2);
            for (int i = 0; i < loopCnt; ++i)
            {
                splitA = i;
                splitB = (i + halfCnt) % n;
                curdis = tuples[splitA].Item1.DistanceTo(tuples[splitB].Item1);
                flag = 0;
                var tmpTuple = new Tuple<Point3d, Point3d>(tuples[splitA].Item1, tuples[splitB].Item1);
                foreach (var curTuple in tuples)
                {
                    if (LineDealer.IsIntersect(tmpTuple.Item1, tmpTuple.Item2, curTuple.Item1, curTuple.Item2))
                    {
                        flag = 1;
                        break;
                    }
                }
                if (flag == 1)
                {
                    continue;
                }
                var tmpTuplesA = new List<Tuple<Point3d, Point3d>>();
                var tmpTuplesB = new List<Tuple<Point3d, Point3d>>();
                Split2Order(tuples, splitA, splitB, tmpTuplesA, tmpTuplesB);
                if (curdis < mindis)
                {
                    mindis = curdis;
                    tuplesA = tmpTuplesA;
                    tuplesB = tmpTuplesB;
                }
            }
            //Tail Recursion
            SplitPolyline(tuplesA, ref tupleLines);
            SplitPolyline(tuplesB, ref tupleLines);
        }

        /// <summary>
        /// Split a polylin from certain point to two polyline
        /// </summary>
        /// <param name="tuples"></param>
        /// <param name="splitA"></param>
        /// <param name="splitB"></param>
        /// <param name="tuplesA">in & out</param>
        /// <param name="tuplesB">in & out</param>
        public static void Split2Order(List<Tuple<Point3d, Point3d>> tuples, int splitA, int splitB, List<Tuple<Point3d, Point3d>> tuplesA, List<Tuple<Point3d, Point3d>> tuplesB)
        {
            tuplesA.Clear();
            tuplesB.Clear();
            int n = tuples.Count;
            if (splitA >= n || splitA < 0 || splitB >= n || splitB < 0)
            {
                return;
            }
            if (splitA > splitB)
            {
                int tmp;
                tmp = splitA;
                splitA = splitB;
                splitB = tmp;
            }
            for (int i = 0; i < tuples.Count; ++i)
            {
                if (i >= splitA && i < splitB)
                {
                    tuplesA.Add(tuples[i]);
                }
                else
                {
                    tuplesB.Add(tuples[i]);
                }
            }
            tuplesA.Add(new Tuple<Point3d, Point3d>(tuples[splitB].Item1, tuples[splitA].Item1));
            tuplesB.Add(new Tuple<Point3d, Point3d>(tuples[splitA].Item1, tuples[splitB].Item1));
        }

        /// <summary>
        /// Merge Two Polyline to One
        /// </summary>
        /// <param name="polylineA"></param>
        /// <param name="polylineB"></param>
        /// <returns>成功（要求有共线）返回合并后的结果，失败返回第一个多边形</returns>
        public static List<Tuple<Point3d, Point3d>> MergePolyline(List<Tuple<Point3d, Point3d>> polylineA, List<Tuple<Point3d, Point3d>> polylineB, double tolerance = 1)
        {
            HashSet<Tuple<Point3d, Point3d>> nowTuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var line in polylineA)
            {
                nowTuples.Add(line);
            }
            foreach (var line in polylineB)
            {
                var converseLine = new Tuple<Point3d, Point3d>(line.Item2, line.Item1);
                if (nowTuples.Contains(converseLine))
                {
                    nowTuples.Remove(converseLine);
                    continue;
                }
                nowTuples.Add(line);
            }
            return LineDealer.OrderTuples(nowTuples.ToList(), tolerance);
        }
        public static List<Tuple<Point3d, Point3d>> MergePolyline(List<Tuple<Point3d, Point3d>> polylineA, List<Tuple<Point3d, Point3d>> polylineB,
            Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines, Dictionary<Tuple<Point3d, Point3d>, int> lineVisit, double tolerance = 1)
        {
            HashSet<Tuple<Point3d, Point3d>> nowTuples = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var lineA in polylineA)
            {
                nowTuples.Add(lineA);
                if (lineVisit.ContainsKey(lineA))
                {
                    lineVisit[lineA] = 1;
                    findPolylineFromLines.Remove(lineA);
                }
            }
            foreach (var lineB in polylineB)
            {
                var converseLine = new Tuple<Point3d, Point3d>(lineB.Item2, lineB.Item1);
                if (nowTuples.Contains(converseLine))
                {
                    nowTuples.Remove(converseLine);
                    if (lineVisit.ContainsKey(converseLine))
                    {
                        lineVisit[converseLine] = 2;
                        lineVisit[lineB] = 2;
                        findPolylineFromLines.Remove(converseLine);
                    }
                    continue;
                }
                nowTuples.Add(lineB);
            }
            List<Tuple<Point3d, Point3d>> ansTuples = LineDealer.OrderTuples(nowTuples.ToList(), tolerance);
            foreach (var tuple in ansTuples)
            {
                if (!findPolylineFromLines.ContainsKey(tuple))
                {
                    findPolylineFromLines.Add(tuple, ansTuples);
                }
            }
            return ansTuples;
        }

        /// <summary>
        /// For(2*n+1) + (3)edges case，Convert to even edges polyline,  then split to some small polyline
        /// </summary>
        /// <param name="polylineA"></param>
        /// <param name="polylineB"></param>
        /// <returns></returns>
        public static void CaseOddP3(List<Tuple<Point3d, Point3d>> polylineA, List<Tuple<Point3d, Point3d>> polylineB)
        {
            List<Tuple<Point3d, Point3d>> evenLines = MergePolyline(polylineA, polylineB);
            List<List<Tuple<Point3d, Point3d>>> polylines = new List<List<Tuple<Point3d, Point3d>>>();
            SplitPolyline(evenLines, ref polylines);
        }

        /// <summary>
        /// Let near point connect only one line to inner point(double edge)
        /// </summary>
        /// <param name="points"></param>
        /// <param name=""></param>
        public static void DeleteLineConnectToSingle(Dictionary<Polyline, Point3dCollection> outlineNearPts, Point3dCollection clumnPts, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            Dictionary<Point3d, HashSet<Point3d>> tmpDicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var dic in dicTuples)
            {
                tmpDicTuples.Add(dic.Key, new HashSet<Point3d>());
                foreach (var pt in dic.Value)
                {
                    tmpDicTuples[dic.Key].Add(pt);
                }
            }
            Polyline outline;
            Point3d stayPt;
            Point3dCollection innerPts;
            double minDis;
            double curDis;
            foreach (var dicOutl2Pts in outlineNearPts) //for each outline
            {
                outline = dicOutl2Pts.Key;
                innerPts = PointsDealer.RemoveSimmilerPoint(clumnPts, dicOutl2Pts.Value);
                foreach (Point3d nearPt in dicOutl2Pts.Value) //for each near point on a outline
                {
                    if (tmpDicTuples.ContainsKey(nearPt))
                    {
                        minDis = double.MaxValue;
                        stayPt = nearPt;
                        //find all the lines connect with this point, and only leave the line which is the shortest line in lines which do not connect with nearPt
                        foreach (Point3d curCntPt in tmpDicTuples[nearPt])
                        {
                            if (innerPts.Contains(curCntPt))
                            {
                                curDis = curCntPt.DistanceTo(nearPt);
                                if (curDis < minDis)//; && preCntPt != nearPt)
                                {
                                    minDis = curDis;
                                    if (dicTuples.ContainsKey(nearPt) && dicTuples[nearPt].Contains(curCntPt))
                                    {
                                        stayPt = curCntPt;
                                    }
                                }
                                DeleteFromDicTuples(nearPt, curCntPt, ref dicTuples);
                            }
                        }
                        if (stayPt != nearPt)
                        {
                            AddLineTodicTuples(stayPt, nearPt, ref dicTuples);
                            AddLineTodicTuples(nearPt, stayPt, ref dicTuples);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Find Best Connect Point
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="verticalPt"></param>
        /// <param name="fstPts"></param>
        /// <param name="thdPts"></param>
        /// <param name="walls"></param>
        /// <param name="outline"></param>
        /// <param name="toleranceDegree"></param>
        /// <returns></returns>
        public static Point3d BestConnectPt(Point3d basePt, Point3d verticalPt, List<Point3d> fstPts, List<Point3d> thdPts,
            HashSet<Polyline> walls, Line closetLine, double toleranceDegree = Math.PI / 4, double MaxBeamLength = 13000)
        {
            double baseRadius = basePt.DistanceTo(verticalPt) / Math.Cos(toleranceDegree);
            baseRadius = baseRadius > MaxBeamLength ? MaxBeamLength : baseRadius;
            double curDis;
            Point3d tmpPt = verticalPt;
            double minDis = baseRadius;

            //1、Find the nearest Cross Point
            foreach (var fstPt in fstPts)
            {
                if (fstPt.DistanceTo(basePt) > baseRadius || fstPt.DistanceTo(closetLine.GetClosestPointTo(fstPt, false)) > 600)
                {
                    continue;
                }
                curDis = fstPt.DistanceTo(verticalPt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    tmpPt = fstPt;
                }
            }
            if (tmpPt != verticalPt)
            {
                return tmpPt;
            }

            //2、If there is a near wall, get vertical point on wall
            Circle circle = new Circle(verticalPt, new Vector3d(), 300);
            foreach (var wall in walls)
            {
                if (wall.Intersects(circle) || wall.Contains(circle))
                {
                    return verticalPt;
                }
            }

            //3、Find apex point in range(45degree)
            minDis = baseRadius;
            foreach (var thdPt in thdPts)
            {
                if (thdPt.DistanceTo(basePt) > baseRadius || thdPt.DistanceTo(closetLine.GetClosestPointTo(thdPt, false)) > 600)
                {
                    continue;
                }
                curDis = thdPt.DistanceTo(verticalPt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    tmpPt = thdPt;
                }
            }
            if (tmpPt != verticalPt)
            {
                return tmpPt;
            }

            //4、Return the vertical point on outline
            //ShowInfo.ShowPointAsU(verticalPt, 7, 200);
            return verticalPt;
        }

        /// <summary>
        /// reduce degree up to 4 for each point(删除最小夹角中长度最短的那个)
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="outline2BorderNearPts"></param>
        public static void DeleteConnectUpToFourA(Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            foreach (var dic in dicTuples)
            {
                int n = dic.Value.Count;
                while (n > 4)
                {
                    List<Point3d> cntPts = dic.Value.ToList();
                    Vector3d baseVec = cntPts[0] - dic.Key;
                    cntPts = cntPts.OrderBy(pt => (pt - dic.Key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        curDegree = (cntPts[i % n] - dic.Key).GetAngleTo(cntPts[i - 1] - dic.Key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    Point3d rmPt = new Point3d();
                    if (minDegreePairPt.Item1.DistanceTo(dic.Key) <= minDegreePairPt.Item2.DistanceTo(dic.Key))
                    {
                        rmPt = minDegreePairPt.Item1;
                        --n;
                    }
                    else
                    {
                        rmPt = minDegreePairPt.Item2;
                        --n;
                    }

                    dic.Value.Remove(rmPt);
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(dic.Key) && borderPt2NearPts[dic.Key].Contains(rmPt))
                        {
                            borderPt2NearPts[dic.Key].Remove(rmPt);
                            if (borderPt2NearPts[dic.Key].Count == 0)
                            {
                                borderPt2NearPts.Remove(dic.Key);
                            }
                        }
                    }
                    if (dicTuples.ContainsKey(rmPt))
                    {
                        if (dicTuples[rmPt].Contains(dic.Key))
                        {
                            dicTuples[rmPt].Remove(dic.Key);
                        }
                    }
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(rmPt) && borderPt2NearPts[rmPt].Contains(dic.Key))
                        {
                            borderPt2NearPts[rmPt].Remove(dic.Key);
                            if (borderPt2NearPts[rmPt].Count == 0)
                            {
                                borderPt2NearPts.Remove(dic.Key);
                            }
                        }
                    }
                }
            }
        }
        public static void DeleteConnectUpToFourAA(Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            foreach (var dic in dicTuples)
            {
                int n = dic.Value.Count;
                while (n > 4)
                {
                    List<Point3d> cntPts = dic.Value.ToList();
                    Vector3d baseVec = cntPts[0] - dic.Key;
                    cntPts = cntPts.OrderBy(pt => (pt - dic.Key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        curDegree = (cntPts[i % n] - dic.Key).GetAngleTo(cntPts[i - 1] - dic.Key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    Point3d rmPt = new Point3d();
                    if (minDegreePairPt.Item1.DistanceTo(dic.Key) >= minDegreePairPt.Item2.DistanceTo(dic.Key))
                    {
                        rmPt = minDegreePairPt.Item1;
                        --n;
                    }
                    else
                    {
                        rmPt = minDegreePairPt.Item2;
                        --n;
                    }

                    dic.Value.Remove(rmPt);
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(dic.Key) && borderPt2NearPts[dic.Key].Contains(rmPt))
                        {
                            borderPt2NearPts[dic.Key].Remove(rmPt);
                            if (borderPt2NearPts[dic.Key].Count == 0)
                            {
                                borderPt2NearPts.Remove(dic.Key);
                            }
                        }
                    }
                    if (dicTuples.ContainsKey(rmPt))
                    {
                        if (dicTuples[rmPt].Contains(dic.Key))
                        {
                            dicTuples[rmPt].Remove(dic.Key);
                        }
                    }
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(rmPt) && borderPt2NearPts[rmPt].Contains(dic.Key))
                        {
                            borderPt2NearPts[rmPt].Remove(dic.Key);
                            if (borderPt2NearPts[rmPt].Count == 0)
                            {
                                borderPt2NearPts.Remove(dic.Key);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// reduce degree up to 4 for each point(删除最不符合90度的那个)
        /// 删除的时候，如果删除点的对点是外边线上的并且只有这一个连线，则不能删掉，其他都可删
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="outline2BorderNearPts"></param>
        public static void DeleteConnectUpToFourB(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                var key = dic.Key;
                if (!dicTuples.ContainsKey(key))
                {
                    continue;
                }
                var value = dicTuples[key];
                int n = value.Count;
                while (n > 4)
                {
                    List<Point3d> cntPts = value.ToList();
                    Vector3d baseVec = cntPts[0] - key;
                    cntPts = cntPts.OrderBy(pt => (pt - key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        curDegree = (cntPts[i % n] - key).GetAngleTo(cntPts[i - 1] - key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    Point3d rmPt = new Point3d();
                    if (!IsCoreLine(key, minDegreePairPt.Item1, dicTuples))
                    {
                        rmPt = minDegreePairPt.Item1;
                    }
                    else if (!IsCoreLine(key, minDegreePairPt.Item2, dicTuples))
                    {
                        rmPt = minDegreePairPt.Item2;
                    }
                    --n;
                    if (rmPt == new Point3d() || !dicTuples.ContainsKey(rmPt))
                    {
                        continue;
                    }
                    bool flag = false;
                    foreach (var outline2BorderNearPt in outline2BorderNearPts)
                    {
                        if (outline2BorderNearPt.Value.ContainsKey(rmPt) && outline2BorderNearPt.Value[rmPt].Count == 1)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag == true)
                    //if (flag == true || dicTuples[rmPt].Count <= 4)
                    {
                        continue;
                    }
                    value.Remove(rmPt);
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(key) && borderPt2NearPts[key].Contains(rmPt))
                        {
                            borderPt2NearPts[key].Remove(rmPt);
                            if (borderPt2NearPts[key].Count == 0)
                            {
                                borderPt2NearPts.Remove(key);
                            }
                        }
                    }
                    DeleteFromDicTuples(rmPt, key, ref dicTuples);
                    foreach (var borderPt2NearPts in outline2BorderNearPts.Values)
                    {
                        if (borderPt2NearPts.ContainsKey(rmPt) && borderPt2NearPts[rmPt].Contains(key))
                        {
                            borderPt2NearPts[rmPt].Remove(key);
                            if (borderPt2NearPts[rmPt].Count == 0)
                            {
                                borderPt2NearPts.Remove(key);
                            }
                        }
                    }
                }
            }
        }
        public static void DeleteConnectUpToFourC(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, HashSet<Point3d> itcNearPts, double minAngle = Math.PI / 4)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                var key = dic.Key;
                if (!dicTuples.ContainsKey(key))
                {
                    continue;
                }
                var value = dicTuples[key];
                int n = value.Count;
                while (n-- > 4)
                {
                    List<Point3d> cntPts = value.ToList();
                    Vector3d baseVec = cntPts[0] - key;
                    cntPts = cntPts.OrderBy(pt => (pt - key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                    Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                    double minDegree = double.MaxValue;
                    double curDegree;
                    for (int i = 1; i <= n; ++i)
                    {
                        curDegree = (cntPts[i % n] - key).GetAngleTo(cntPts[i - 1] - key);
                        if (curDegree < minDegree)
                        {
                            minDegree = curDegree;
                            minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                        }
                    }
                    Point3d rmPt = new Point3d();
                    if (minDegree > minAngle)
                    {
                        break; //剪枝
                    }
                    var rmPtA = minDegreePairPt.Item1;
                    var rmPtB = minDegreePairPt.Item2;
                    //若要删除的点是边界点且出度为1，则不应删除
                    int priA = PointProirity(rmPtA, dicTuples, itcNearPts);
                    int priB = PointProirity(rmPtB, dicTuples, itcNearPts);
                    if(priA < 2 && priB < 2)
                    {
                        continue;
                    }
                    if(priA < 2 && priB >= 2)
                    {
                        rmPt = rmPtB;
                    }
                    else if (priB < 2 && priA >= 2)
                    {
                        rmPt = rmPtA;
                    }
                    else if (rmPt == new Point3d())
                    {
                        if (!IsCoreLine(key, rmPtA, dicTuples))
                        {
                            rmPt = rmPtA;
                        }
                        else if (!IsCoreLine(key, rmPtB, dicTuples))
                        {
                            rmPt = rmPtB;
                        }
                    }
                    if (rmPt == new Point3d())
                    {
                        continue;
                    }
                    value.Remove(rmPt);
                    DeleteFromDicTuples(rmPt, key, ref dicTuples);
                }
            }
        }
        public static bool IsCoreLine(Point3d baseFromPt , Point3d baseToPt, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (!dicTuples.ContainsKey(baseFromPt) || !dicTuples[baseFromPt].Contains(baseToPt))
            {
                return false;
            }
            var baseVec = baseToPt - baseFromPt;
            int cnt = 0;
            foreach (var pt in dicTuples[baseFromPt])
            {
                var curAngel = (pt - baseFromPt).GetAngleTo(baseVec);
                if (Math.Abs(curAngel - Math.PI / 2) < Math.PI / 15 || (Math.Abs(curAngel - Math.PI) < Math.PI / 15))
                {
                    cnt++;
                }
            }
            //if (!dicTuples.ContainsKey(baseFromPt) || !dicTuples[baseFromPt].Contains(baseToPt))
            //{
            //    return false;
            //}
            //var baseVec = baseFromPt - baseToPt;
            //int cnt = 0;
            //foreach (var pt in dicTuples[baseToPt])
            //{
            //    var curAngel = (pt - baseToPt).GetAngleTo(baseVec);
            //    if (Math.Abs(curAngel - Math.PI / 2) < Math.PI / 15 || (Math.Abs(curAngel - Math.PI) < Math.PI / 15))
            //    {
            //        cnt++;
            //    }
            //}
            if (cnt >= 2) 
                return true;
            return false;
        }
        public static int PointProirity(Point3d pt, Dictionary<Point3d, HashSet<Point3d>> dicTuples, HashSet<Point3d> itcNearPts)
        {
            if (!dicTuples.ContainsKey(pt))
            {
                return 0;//不可操作点
            }
            else
            {
                var cnt = dicTuples[pt].Count;
                if (itcNearPts.Contains(pt))
                {
                    //是边界点且出度为1，则不应删除
                    if (cnt <= 1)
                    {
                        return 1; //1：绝对不可删除
                    }
                    else
                    {
                        return cnt; //2：可能删除
                    }
                }
                else
                {
                    if (dicTuples[pt].Count < 4)
                    {
                        return 1;
                    }
                    else
                    {
                        return cnt;
                    }
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
                            var ansPt = GetObject.GetPointByDirectionB(basePt ,baseVector.RotateBy(Math.PI / 2 * i, Vector3d.ZAxis), basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPt, ref dicTuples);
                        }
                    }
                    else if (cnt == 2)
                    {
                        var vecA = (nowCntPts[0] - basePt).GetNormal();
                        var vecB = (nowCntPts[1] - basePt).GetNormal();
                        double baseAngel = vecA.GetAngleTo(vecB);
                        if (baseAngel > partice * 18 && baseAngel < partice * 22)
                        {
                            var ansPtA = GetObject.GetPointByDirectionB(basePt, -vecA, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtA, ref dicTuples);
                            var ansPtB = GetObject.GetPointByDirectionB(basePt, -vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtB, ref dicTuples);
                        }
                        else if(baseAngel > Math.PI - partice * 4)
                        {
                            var ansPtA = GetObject.GetPointByDirectionB(basePt, vecA + vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtA, ref dicTuples);
                            var ansPtB = GetObject.GetPointByDirectionB(basePt, -vecA - vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineTodicTuples(basePt, ansPtB, ref dicTuples);
                        }
                    }
                    else if(cnt == 3)
                    {
                        var findVec = GetObject.GetDirectionByThreeVecs(basePt, nowCntPts[0], nowCntPts[1], nowCntPts[2]);
                        var ansPt = GetObject.GetPointByDirectionB(basePt, findVec, basePts, partice * 5, MaxBeamLength);
                        AddLineTodicTuples(basePt, ansPt, ref dicTuples);
                    }
                }
            }
        }

        /// <summary>
        /// Close a polyline by its border points
        /// 注意：要考虑最外边框和包含型边框的区别
        /// </summary>
        public static Dictionary<Point3d, Point3d> CloseBorder(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
        {
            Dictionary<Point3d, Point3d> ansDic = new Dictionary<Point3d, Point3d>();
            foreach (var dic in outline2BorderNearPts)
            {
                Polyline polyline = dic.Key;
                List<Point3d> points = new List<Point3d>();
                int n = polyline.NumberOfVertices;
                for (int i = 0; i < n; ++i)
                {
                    Line tmpLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                    List<Point3d> tmpPts = new List<Point3d>();
                    foreach (var borderPt in dic.Value.Keys)
                    {
                        if (borderPt.DistanceTo(tmpLine.GetClosestPointTo(borderPt, false)) < 700)// && !points.Contains(borderPt))
                        {
                            tmpPts.Add(borderPt);
                        }
                    }
                    tmpPts = tmpPts.OrderBy(p => p.DistanceTo(tmpLine.StartPoint)).ToList();
                    points.AddRange(tmpPts);
                }
                for (int i = 1; i <= points.Count; i++)
                {
                    {
                        if (!ansDic.ContainsKey(points[i % points.Count]))
                        {
                            ansDic.Add(points[i % points.Count], points[i - 1]);
                        }
                    }
                }
            }
            return ansDic;
        }

        public static HashSet<Tuple<Point3d, Point3d>> CloseBorder(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts, Dictionary<Polyline, List<Point3d>> outline2ZeroPts = null)
        {
            Dictionary<Point3d, Point3d> ansDic = new Dictionary<Point3d, Point3d>();
            HashSet<Tuple<Point3d, Point3d>> ansTuple = new HashSet<Tuple<Point3d, Point3d>>();
            foreach (var dic in outline2BorderNearPts)
            {
                Polyline polyline = dic.Key;
                List<Point3d> points = new List<Point3d>();
                int n = polyline.NumberOfVertices;
                var borderPts = dic.Value.Keys.ToList();
                if (outline2ZeroPts != null && outline2ZeroPts.ContainsKey(polyline))
                {
                    borderPts.AddRange(outline2ZeroPts[polyline]);
                }
                for (int i = 0; i < n; ++i)
                {
                    Line tmpLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                    List<Point3d> tmpPts = new List<Point3d>();
                    foreach (var borderPt in borderPts)
                    {
                        if (borderPt.DistanceTo(tmpLine.GetClosestPointTo(borderPt, false)) < 700)// && !points.Contains(borderPt))
                        {
                            tmpPts.Add(borderPt);
                        }
                    }
                    tmpPts = tmpPts.OrderBy(p => p.DistanceTo(tmpLine.StartPoint)).ToList();
                    points.AddRange(tmpPts);
                }
                for (int i = 1; i <= points.Count; i++)
                {
                    if (!ansDic.ContainsKey(points[i % points.Count]))
                    {
                        ansDic.Add(points[i % points.Count], points[i - 1]);
                    }
                    ansTuple.Add(new Tuple<Point3d, Point3d>(points[i % points.Count], points[i - 1]));
                }
                if (points.Count > 1)
                {
                    ansTuple.Add(new Tuple<Point3d, Point3d>(points[points.Count - 1], points[0]));
                }
            }
            return ansTuple;
        }

        public static Dictionary<Point3d, Point3d> CloseBorderA(HashSet<Polyline> polylines, List<Point3d> oriPoints)
        {
            var outline2BorderPts = PointsDealer.GetOutline2BorderPts(polylines, oriPoints);
            Dictionary<Point3d, Point3d> ansDic = new Dictionary<Point3d, Point3d>();
            foreach (var dic in outline2BorderPts)
            {
                Polyline polyline = dic.Key;
                List<Point3d> points = new List<Point3d>();
                int n = polyline.NumberOfVertices;
                for (int i = 0; i < n; ++i)
                {
                    Line tmpLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                    List<Point3d> tmpPts = new List<Point3d>();
                    foreach (var borderPt in dic.Value)
                    {
                        if (borderPt.DistanceTo(tmpLine.GetClosestPointTo(borderPt, false)) < 700)
                        {
                            tmpPts.Add(borderPt);
                        }
                    }
                    tmpPts = tmpPts.OrderBy(p => p.DistanceTo(tmpLine.StartPoint)).ToList();
                    points.AddRange(tmpPts);
                }
                for (int i = 1; i <= points.Count; i++)
                {
                    if (!ansDic.ContainsKey(points[i % points.Count]))
                    {
                        ansDic.Add(points[i % points.Count], points[i - 1]);
                    }
                }
            }
            return ansDic;
        }
        public static Dictionary<Point3d, Point3d> CloseBorderB(HashSet<Polyline> polylines, List<Point3d> oriPoints)
        {
            var outline2BorderPts = PointsDealer.GetOutline2BorderPts(polylines, oriPoints);
            Dictionary<Point3d, Point3d> ansDic = new Dictionary<Point3d, Point3d>();
            foreach (var dic in outline2BorderPts)
            {
                Polyline polyline = dic.Key;
                List<Point3d> points = new List<Point3d>();
                int n = polyline.NumberOfVertices;
                for (int i = 0; i < n; ++i)
                {
                    Line tmpLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                    
                    List<Point3d> tmpPts = new List<Point3d>();
                    foreach (var borderPt in dic.Value)
                    {
                        if (borderPt.DistanceTo(tmpLine.GetClosestPointTo(borderPt, false)) < 400)
                        {
                            tmpPts.Add(borderPt);
                        }
                    }
                    tmpPts = tmpPts.OrderBy(p => p.DistanceTo(tmpLine.StartPoint)).ToList();
                    points.AddRange(tmpPts);
                }
                HashSet<Point3d> ptVisted = new HashSet<Point3d>();
                int cnt = points.Count;
                for (int i = 1; i <= cnt; i++)
                {
                    Point3d ptSt = points[i - 1];
                    Point3d ptEd = points[i % points.Count];
                    if (!ansDic.ContainsKey(ptSt) && !ptVisted.Contains(ptEd))
                    {
                        ptVisted.Add(ptSt);
                        ptVisted.Add(ptEd);
                        ansDic.Add(ptSt, ptEd);
                    }
                }
            }
            return ansDic;
        }
        public static Dictionary<Point3d, Point3d> CloseBorderCL(HashSet<Polyline> polylines, List<Point3d> oriPoints)
        {
            var objs = new DBObjectCollection();
            var centerPolylines = CenterLine.RECCenterLines(polylines);

            //做中心线
            Dictionary<Point3d, Point3d> ansDic = new Dictionary<Point3d, Point3d>();
            HashSet<Point3d> ptVisit = new HashSet<Point3d>();
            foreach (var centerPolyline in centerPolylines)
            {
                int n = centerPolyline.NumberOfVertices;
                List<Point3d> points = new List<Point3d>();
                for (int i = 0; i < n; ++i)
                {
                    Line tmpLine = new Line(centerPolyline.GetPoint3dAt(i), centerPolyline.GetPoint3dAt((i + 1) % n));
                    List<Point3d> tmpPts = new List<Point3d>();
                    foreach (var borderPt in oriPoints)
                    {
                        if (!ptVisit.Contains(borderPt) && borderPt.DistanceTo(tmpLine.GetClosestPointTo(borderPt, false)) < 400)
                        {
                            tmpPts.Add(borderPt);
                            ptVisit.Add(borderPt);
                        }
                    }
                    tmpPts = tmpPts.OrderBy(p => p.DistanceTo(tmpLine.StartPoint)).ToList();
                    points.AddRange(tmpPts);
                }
                for (int i = 1; i <= points.Count; i++)
                {
                    if (!ansDic.ContainsKey(points[i % points.Count]))
                    {
                        ansDic.Add(points[i % points.Count], points[i - 1]);
                    }
                }
            }
            return ansDic;
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
        public static void ReduceSimilarLine(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, Dictionary<Point3d, HashSet<Point3d>> priority1stDicTuples = null, double tolerance = Math.PI / 8)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in dicTuples)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                var key = dic.Key;
                if (!dicTuples.ContainsKey(key))
                {
                    continue;
                }
                int cnt = dicTuples[key].Count;
                while (cnt -- > 1)
                {
                    if (!dicTuples.ContainsKey(key))
                    {
                        break;
                    }
                    var value = dicTuples[key];
                    int n = value.Count;
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
                    var ptA = minDegreePairPt.Item1;
                    var ptB = minDegreePairPt.Item2;
                    if (priority1stDicTuples != null && priority1stDicTuples.ContainsKey(key))
                    {
                        if (priority1stDicTuples[key].Contains(ptA) && !priority1stDicTuples[key].Contains(ptB))
                        {
                            rmPt = ptB;
                        }
                        else if (priority1stDicTuples[key].Contains(ptB) && !priority1stDicTuples[key].Contains(ptA))
                        {
                            rmPt = ptA;
                        }
                    }
                    if(rmPt == new Point3d())
                    {
                        if (ptA.DistanceTo(key) >= ptB.DistanceTo(key))
                        {
                            rmPt = ptA;
                        }
                        else
                        {
                            rmPt = ptB;
                        }
                    }
                    DeleteFromDicTuples(rmPt, key, ref dicTuples);
                }
            }
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
            foreach(var dicTuple in dicTuples)
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
                if(cnt == 1)
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
        public static List<Point3d> ReduceSimilarPoints(List<Point3d> pts, List<Point3d> basePoints = null, double tolerance = 900)
        {
            var ptVisted = new HashSet<Point3d>();
            List<Point3d> ansPts = new List<Point3d>();
            foreach (var ptA in pts)
            {
                if (ptVisted.Contains(ptA))
                {
                    continue;
                }
                ptVisted.Add(ptA);
                var closePtlist = new List<Point3d>();
                double xSum = ptA.X;
                double ySum = ptA.Y;
                closePtlist.Add(ptA);
                int cnt = 1;
                foreach (var ptB in pts)
                {
                    if (ptA != ptB && ptA.DistanceTo(ptB) < tolerance)
                    {
                        if (!ptVisted.Contains(ptB))
                        {
                            ptVisted.Add(ptB);
                        }
                        closePtlist.Add(ptB);

                        xSum += ptB.X;
                        ySum += ptB.Y;
                        ++cnt;
                    }
                }
                if (cnt == 1)
                {
                    ansPts.Add(ptA);
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
                ansPts.Add(minDisBasePt);
            }
            return ansPts;
        }
        public static Point3dCollection ReduceSimilarPoints(Point3dCollection pts, List<Point3d> basePoints = null, double tolerance = 10)
        {
            var ptVisted = new HashSet<Point3d>();
            Point3dCollection ansPts = new Point3dCollection();
            foreach (Point3d ptA in pts)
            {
                if (ptVisted.Contains(ptA))
                {
                    continue;
                }
                ptVisted.Add(ptA);
                var closePtlist = new List<Point3d>();
                double xSum = ptA.X;
                double ySum = ptA.Y;
                closePtlist.Add(ptA);
                int cnt = 1;
                foreach (Point3d ptB in pts)
                {
                    if (ptA != ptB && ptA.DistanceTo(ptB) < tolerance)
                    {
                        if (!ptVisted.Contains(ptB))
                        {
                            ptVisted.Add(ptB);
                        }
                        closePtlist.Add(ptB);

                        xSum += ptB.X;
                        ySum += ptB.Y;
                        ++cnt;
                    }
                }
                if (cnt == 1)
                {
                    ansPts.Add(ptA);
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
                ansPts.Add(minDisBasePt);
            }
            return ansPts;
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
                if(dicTuples[ptA].Count == 0)
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
        /// <param name="tuple"></param>
        /// <param name="dicTuples"></param>
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

        /// <summary>
        /// 一堆线集中，两两相交的话删除长度较长的线
        /// </summary>
        public static void RemoveIntersectLines(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, double devision = 1.0)
        {
            var tmpTuples = LineDealer.DicTuplesToTuples(dicTuples).ToList();
            foreach (var tupleA in tmpTuples)
            {
                var newTupA = LineDealer.ReduceTuple(tupleA, 50);
                foreach(var tupleB in tmpTuples)
                {
                    var newTupB = LineDealer.ReduceTuple(tupleB, 50);
                    //若两线相交，删除长线
                    if (tupleA == tupleB || (tupleA.Item1.DistanceTo(tupleB.Item2) < devision && tupleB.Item1.DistanceTo(tupleA.Item2)< devision) || !LineDealer.IsIntersect(newTupA.Item1, newTupA.Item2, newTupB.Item1, newTupB.Item2))
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
        /// 在线集中删除和指定类型线相交的线
        /// </summary>
        public static void RemoveLinesInterSectWithCloseBorderLines(List<Tuple<Point3d, Point3d>> closebdLines, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var tmpTuples = LineDealer.DicTuplesToTuples(dicTuples).ToList();
            foreach (var tup in closebdLines)
            {
                if(tup.Item1.DistanceTo(tup.Item2) > 20000)
                {
                    continue;
                }
                var tupA = LineDealer.ReduceTuple(tup, 200);
                foreach (var tmpTuple in tmpTuples)
                {
                    var tupB = LineDealer.ReduceTuple(tmpTuple, 200);
                    if (LineDealer.IsIntersect(tupA.Item1, tupA.Item2, tupB.Item1, tupB.Item2))
                    {
                        DeleteFromDicTuples(tmpTuple.Item1, tmpTuple.Item2, ref dicTuples);
                    }
                }
            }
        }

        public static Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> UpdateBorder2NearPts(Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts,
            Dictionary<Point3d, HashSet<Point3d>> priority1stDicTuples, double tolerance = Math.PI / 4)
        {
            var dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            outline2BorderNearPts.Values.ForEach(o => o.ForEach(kv => kv.Value.ForEach(pt =>
            {
                AddLineTodicTuples(kv.Key, pt, ref dicTuples);
            })));
            priority1stDicTuples.ForEach(o => o.Value.ForEach(pt =>
            {
                AddLineTodicTuples(o.Key, pt, ref dicTuples);
            }));

            ReduceSimilarLine(ref dicTuples, priority1stDicTuples, tolerance);
            RemoveIntersectLines(ref dicTuples);
            var newOutline2BorderNearPts = new Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>>();
            foreach(var dicKVPair in outline2BorderNearPts)
            {
                var outline = dicKVPair.Key;
                if (!newOutline2BorderNearPts.ContainsKey(outline))
                {
                    newOutline2BorderNearPts.Add(outline, new Dictionary<Point3d, HashSet<Point3d>>());
                }
                foreach(var kvPair in dicKVPair.Value)
                {
                    var borderPt = kvPair.Key;
                    if (!dicTuples.ContainsKey(borderPt) || dicTuples[borderPt].Count == 0)
                    {
                        continue;
                    }
                    if (!newOutline2BorderNearPts[outline].ContainsKey(borderPt))
                    {
                        newOutline2BorderNearPts[outline].Add(borderPt, new HashSet<Point3d>());
                    }
                    foreach(var nearPt in dicTuples[borderPt])
                    {
                        if (!newOutline2BorderNearPts[outline][borderPt].Contains(nearPt))
                        {
                            newOutline2BorderNearPts[outline][borderPt].Add(nearPt);
                        }
                    }
                }
            }
            return newOutline2BorderNearPts;
        }
    }
}
