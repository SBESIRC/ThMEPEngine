using System;
using System.Collections.Generic;
using System.Linq;
using NFox.Cad;
using Linq2Acad;
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
        public static void SplitPolyline(List<Tuple<Point3d, Point3d>> tuples, List<List<Tuple<Point3d, Point3d>>> tupleLines)
        {
            //Recursion boundary
            int n = tuples.Count;
            if (n == 0 || n > 20)
            {
                return;
            }
            if (n <= 5)
            {
                tupleLines.Add(tuples);
                return;
            }

            //Initialization
            //Polyline polyline = LineDealer.Tuples2Polyline(tuples);
            tuples = LineDealer.OrderTuples(tuples);
            n = tuples.Count;
            //double area = polyline.Area;
            //double halfArea = area / 2.0;
            //double minCmp = double.MaxValue;
            //double curCmp;
            int halfCnt = n / 2;
            List<Tuple<Point3d, Point3d>> tuplesA = new List<Tuple<Point3d, Point3d>>();
            List<Tuple<Point3d, Point3d>> tuplesB = new List<Tuple<Point3d, Point3d>>();
            //double areaA;
            //double areaB;
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
                //if (curdis > 9000)
                //{
                //    continue;
                //}

                flag = 0;
                //var x = tuples[splitA].Item1;
                //var xx = tuples[splitB].Item1;
                Tuple<Point3d, Point3d> tmpTuple = new Tuple<Point3d, Point3d>(tuples[splitA].Item1, tuples[splitB].Item1);
                foreach (var curTuple in tuples)
                {
                    if (LineDealer.IsIntersect(tmpTuple.Item1, tmpTuple.Item2, curTuple.Item1, curTuple.Item2))
                    //if ((new Line(tmpTuple.Item1, tmpTuple.Item2).Intersect(new Line(curTuple.Item1, curTuple.Item2), 0)) != null)
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
                //areaA = LineDealer.Tuples2Polyline(tmpTuplesA).Area;
                //areaB = LineDealer.Tuples2Polyline(tmpTuplesB).Area;
                //curCmp = (Math.Pow(areaA - halfArea, 2.0) + Math.Pow(areaB - halfArea, 2.0)) * Math.Pow(tuples[i].Item1.DistanceTo(tuples[(i + halfCnt) % n].Item1), 4);
                //curCmp = (Math.Pow(areaA - halfArea, 2.0) + Math.Pow(areaB - halfArea, 2.0)) * Math.Pow(tuples[splitA].Item1.DistanceTo(tuples[splitB].Item1), 4);

                
                //if (curCmp < minCmp)
                if (curdis < mindis)
                {
                    mindis = curdis;
                    //minCmp = curCmp;
                    tuplesA = tmpTuplesA;
                    tuplesB = tmpTuplesB;
                }
            }

            //Tail Recursion
            SplitPolyline(tuplesA, tupleLines);
            SplitPolyline(tuplesB, tupleLines);
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
            SplitPolyline(evenLines, polylines);
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
            //Point3d preCntPt;
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
                    //ShowInfo.ShowPointAsX(nearPt, 210, 520);
                    if (tmpDicTuples.ContainsKey(nearPt))
                    {
                        minDis = double.MaxValue;
                        stayPt = nearPt;
                        //find all the lines connect with this point, and only leave the line which is the shortest line in lines which do not connect with nearPt
                        foreach (Point3d curCntPt in tmpDicTuples[nearPt]) //no more than 4
                        {
                            if (innerPts.Contains(curCntPt))
                            {
                                //ShowInfo.ShowPointAsO(curCntPt, 1, 300);
                                curDis = curCntPt.DistanceTo(nearPt);
                                if (curDis < minDis)//; && preCntPt != nearPt)
                                {
                                    minDis = curDis;
                                    if (dicTuples.ContainsKey(nearPt) && dicTuples[nearPt].Contains(curCntPt))
                                    {
                                        stayPt = curCntPt;
                                    }
                                }
                                if (dicTuples.ContainsKey(nearPt) && dicTuples[nearPt].Contains(curCntPt))
                                {
                                    dicTuples[nearPt].Remove(curCntPt);
                                }
                                if (dicTuples.ContainsKey(curCntPt) && dicTuples[curCntPt].Contains(nearPt))
                                {
                                    dicTuples[curCntPt].Remove(nearPt);
                                }
                            }
                        }
                        if (stayPt != nearPt)
                        {
                            if (!dicTuples.ContainsKey(stayPt))
                            {
                                dicTuples.Add(stayPt, new HashSet<Point3d>());
                            }
                            if (!dicTuples[stayPt].Contains(nearPt))
                            {
                                dicTuples[stayPt].Add(nearPt);
                                //ShowInfo.DrawLine(stayPt, nearPt);
                            }
                            if (!dicTuples.ContainsKey(nearPt))
                            {
                                dicTuples.Add(nearPt, new HashSet<Point3d>());
                            }
                            if (!dicTuples[nearPt].Contains(stayPt))
                            {
                                dicTuples[nearPt].Add(stayPt);
                                //ShowInfo.DrawLine(nearPt, stayPt);
                            }
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
            HashSet<Polyline> walls, Line closetLine, double toleranceDegree = Math.PI / 4)//, HashSet<Point3d> zeroPts)
        {
            double baseRadius = basePt.DistanceTo(verticalPt) / Math.Cos(toleranceDegree);
            baseRadius = baseRadius > 9000 ? 9000 : baseRadius;
            double curDis;
            Point3d tmpPt = verticalPt;
            double minDis = baseRadius;

            ////0、Find the nearest Column Point
            //foreach (var zeroPt in zeroPts)
            //{
            //    if (zeroPt.DistanceTo(basePt) > baseRadius || zeroPt.DistanceTo(closetLine.GetClosestPointTo(zeroPt, false)) > 600)
            //    {
            //        continue;
            //    }
            //    curDis = zeroPt.DistanceTo(verticalPt);
            //    if (curDis < minDis)
            //    {
            //        minDis = curDis;
            //        tmpPt = zeroPt;
            //    }
            //}
            //if (tmpPt != verticalPt)
            //{
            //    return tmpPt;
            //}

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
                //ShowInfo.ShowPointAsO(tmpPt);
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
            //ShowInfo.ShowPointAsU(verticalPt, 7, 200); //common or do not delete
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

        /// <summary>
        /// reduce degree up to 4 for each point(删除最不符合90度的那个)
        /// 删除的时候，如果删除点的对点是外边线上的并且只有这一个连线，则不能删掉，其他都可删
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="outline2BorderNearPts"></param>
        public static void DeleteConnectUpToFourB(Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Polyline, Dictionary<Point3d, HashSet<Point3d>>> outline2BorderNearPts)
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
                    if(!IsCoreLine(dic.Key, minDegreePairPt.Item1, dicTuples))
                    //if (minDegreePairPt.Item1.DistanceTo(dic.Key) >= minDegreePairPt.Item2.DistanceTo(dic.Key))
                    {
                        rmPt = minDegreePairPt.Item1;
                    }
                    else if(!IsCoreLine(dic.Key, minDegreePairPt.Item2, dicTuples))
                    {
                        rmPt = minDegreePairPt.Item2;
                    }
                    --n;
                    bool flag = false;
                    foreach(var outline2BorderNearPt in outline2BorderNearPts)
                    {
                        if (outline2BorderNearPt.Value.ContainsKey(rmPt) && outline2BorderNearPt.Value.Values.Count == 1)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if(flag == true)
                    {
                        continue;
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
        public static bool IsCoreLine(Point3d baseFromPt , Point3d baseToPt, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            if (!dicTuples.ContainsKey(baseFromPt) || !dicTuples[baseFromPt].Contains(baseToPt))
            {
                return false;
            }
            var baseVec = baseToPt - baseFromPt;
            int cnt = 0;
            foreach(var pt in dicTuples[baseFromPt])
            {
                var curAngel = (pt - baseFromPt).GetAngleTo(baseVec);
                if(Math.Abs(curAngel - Math.PI / 2) < Math.PI / 15 || (Math.Abs(curAngel - Math.PI) < Math.PI / 15))
                {
                    cnt++;
                }
            }
            if (cnt >= 2) return true;
            return false;
        }

        /// <summary>
        /// 将点的连接增加至4个
        /// </summary>
        /// <param name="dicTuples"></param>
        public static void AddConnectUpToFour(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, Point3dCollection basePts, HashSet<Point3d> itcNearPts = null)
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
                        var baseVector = nowCntPts[0] - basePt;
                        for (int i = 1; i <= 3; ++i)
                        {
                            var ansPt = GetObject.GetPointByDirection(basePt ,baseVector.RotateBy(Math.PI / 2 * i, Vector3d.ZAxis), basePts, partice * 3, 13000);
                            AddLineTodicTuples(basePt, ansPt, ref dicTuples);
                            AddLineTodicTuples(ansPt, basePt, ref dicTuples);
                        }
                    }
                    else if (cnt == 2)
                    {
                        var vecA = nowCntPts[0] - basePt;
                        var vecB = nowCntPts[1] - basePt;
                        double baseAngel = vecA.GetAngleTo(vecB);
                        if (baseAngel > partice * 18 && baseAngel < partice * 22)
                        {
                            var ansPtA = GetObject.GetPointByDirection(basePt, -vecA, basePts, partice * 3, 13000);
                            AddLineTodicTuples(basePt, ansPtA, ref dicTuples);
                            AddLineTodicTuples(ansPtA, basePt, ref dicTuples);
                            var ansPtB = GetObject.GetPointByDirection(basePt, -vecB, basePts, partice * 3, 13000);
                            AddLineTodicTuples(basePt, ansPtB, ref dicTuples);
                            AddLineTodicTuples(ansPtB, basePt, ref dicTuples);
                        }
                        else if(baseAngel > Math.PI - partice * 4)
                        {
                            var ansPtA = GetObject.GetPointByDirection(basePt, vecA + vecB, basePts, partice * 3, 13000);
                            AddLineTodicTuples(basePt, ansPtA, ref dicTuples);
                            AddLineTodicTuples(ansPtA, basePt, ref dicTuples);
                            var ansPtB = GetObject.GetPointByDirection(basePt, -vecA - vecB, basePts, partice * 3, 13000);
                            AddLineTodicTuples(basePt, ansPtB, ref dicTuples);
                            AddLineTodicTuples(ansPtB, basePt, ref dicTuples);
                        }
                    }
                    else if(cnt == 3)
                    {
                        var findVec = GetObject.GetDirectionByThreeVecs(basePt, nowCntPts[0], nowCntPts[1], nowCntPts[2]);
                        var ansPt = GetObject.GetPointByDirection(basePt, findVec, basePts, partice * 3, 13000);
                        AddLineTodicTuples(basePt, ansPt, ref dicTuples);
                        AddLineTodicTuples(ansPt, basePt, ref dicTuples);
                    }
                }
                else
                {
                    //ShowInfo.ShowPointAsO(basePt, 2, 1000); //孤零零点
                    //cnt = 0;
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
                        //ShowInfo.ShowPointAsU(borderPt, 1);
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
                    //if (points[i % points.Count].DistanceTo(points[i - 1]) < 9000 * 2)
                    {
                        if (!ansDic.ContainsKey(points[i % points.Count]))
                        {
                            //ShowInfo.DrawLine(points[i % points.Count], points[i - 1], 2);
                            ansDic.Add(points[i % points.Count], points[i - 1]);

                            //ansDic.Add(points[i - 1], points[i % points.Count]);
                        }
                        ansTuple.Add(new Tuple<Point3d, Point3d>(points[i % points.Count], points[i - 1]));
                    }
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
            //var outline2BorderPts = PointsDealer.GetOutline2BorderPts(polylines, oriPoints); //使用这个结构可以显著减少复杂度

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
            foreach (var dic in dicTuples)
            {
                int n = dic.Value.Count;
                if (n <= 1)
                {
                    continue;
                }
                List<Point3d> cntPts = dic.Value.ToList();
                Vector3d baseVec = cntPts[0] - dic.Key;
                cntPts = cntPts.OrderBy(pt => (pt - dic.Key).GetAngleTo(baseVec, Vector3d.ZAxis)).ToList();
                Tuple<Point3d, Point3d> minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[0], cntPts[1]);
                double minDegree = double.MaxValue;
                double curDegree;
                for (int i = 1; i <= n; ++i)
                {
                    if(cntPts[i % n].DistanceTo(cntPts[i - 1]) < 1.0 || dic.Key.DistanceTo(cntPts[i - 1]) < 1.0 || cntPts[i % n].DistanceTo(dic.Key) < 1.0)
                    {
                        continue;
                    }
                    curDegree = (cntPts[i % n] - dic.Key).GetAngleTo(cntPts[i - 1] - dic.Key);
                    if (curDegree < minDegree)
                    {
                        minDegree = curDegree;
                        minDegreePairPt = new Tuple<Point3d, Point3d>(cntPts[i % n], cntPts[i - 1]);
                    }
                }
                if (minDegree > tolerance)
                {
                    continue;
                }
                Point3d rmPt = new Point3d();
                //if ((dicTuples.ContainsKey(minDegreePairPt.Item2) && dicTuples[minDegreePairPt.Item2].Count > 1) || minDegreePairPt.Item1.DistanceTo(dic.Key) >= minDegreePairPt.Item2.DistanceTo(dic.Key))
                if (minDegreePairPt.Item1.DistanceTo(dic.Key) >= minDegreePairPt.Item2.DistanceTo(dic.Key))
                {
                    rmPt = minDegreePairPt.Item1;
                }
                else
                {
                    rmPt = minDegreePairPt.Item2;
                }
                //ShowInfo.DrawLine(dic.Key, rmPt, 210);
                --n;
                dic.Value.Remove(rmPt);
                if (dicTuples.ContainsKey(rmPt) && dicTuples[rmPt].Contains(dic.Key))
                {
                    dicTuples[rmPt].Remove(dic.Key);
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
                    AddLineTodicTuples(closePt, minDisBasePt, ref dicTuples);
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
        }

        public static void RemoveIntersectLines(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, double devision = 1.0)
        {
            var tmpTuples = LineDealer.DicTuplesToTuples(dicTuples).ToList();
            foreach (var tupleA in tmpTuples)
            {
                foreach(var tupleB in tmpTuples)
                {
                    //若两线相交，删除长线
                    if(tupleA == tupleB || (tupleA.Item1.DistanceTo(tupleB.Item2) < devision && tupleB.Item1.DistanceTo(tupleA.Item2)< devision) || !LineDealer.IsIntersect(tupleA.Item1, tupleA.Item2, tupleB.Item1, tupleB.Item2))
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

        public static void RemoveLinesInterSectWithCloseBorderLines(List<Tuple<Point3d, Point3d>> closebdLines, ref Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            var tmpTuples = LineDealer.DicTuplesToTuples(dicTuples).ToList();
            foreach (var tup in closebdLines)
            {
                if(tup.Item1.DistanceTo(tup.Item2) > 20000)
                {
                    continue;
                }
                foreach (var tmpTuple in tmpTuples)
                {
                    if (LineDealer.IsIntersect(tup.Item1, tup.Item2, tmpTuple.Item1, tmpTuple.Item2))
                    {
                        DeleteFromDicTuples(tmpTuple.Item1, tmpTuple.Item2, ref dicTuples);
                    }
                }
            }
        }
    }
}
