using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class GraphDealer
    {
        /// <summary>
        /// 简化graph结构
        /// </summary>
        public static void SimplifyGraph(ref Dictionary<Point3d, HashSet<Point3d>> graph, List<Point3d> basePts, double deviation = 1, double angle = Math.PI / 8)
        {
            basePts = PointsDealer.PointsDistinct(basePts);
            ReduceSimilarPoints(ref graph, basePts);
            ReduceSimilarLine(ref graph, angle);
            GraphStandardize(ref graph, basePts, deviation);
        }

        /// <summary>
        /// 减少相似的点
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="basePoints"></param>
        /// <param name="tolerance"></param>net
        public static void ReduceSimilarPoints(ref Dictionary<Point3d, HashSet<Point3d>> graph, List<Point3d> basePoints = null, double tolerance = 900)
        {
            var ptVisted = new HashSet<Point3d>();
            List<Point3d> pts = graph.Keys.ToList();
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in graph)
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
                    if (graph.ContainsKey(ptA) && graph[ptA].Contains(cntPtA))
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
                            if (graph.ContainsKey(cntPtB) && graph[cntPtB].Contains(ptB))
                            {
                                closePtlist.Add(cntPtB);
                                DeleteFromGraph(ptB, cntPtB, ref graph);
                            }
                            if (graph.ContainsKey(ptB) && graph[ptB].Contains(cntPtB))
                            {
                                closePtlist.Add(cntPtB);
                                DeleteFromGraph(ptB, cntPtB, ref graph);
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
                    AddLineToGraph(minDisBasePt, closePt, ref graph);
                }
            }
        }

        /// <summary>
        /// Reduce Similar line to only one
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="tolerance"></param>
        public static void ReduceSimilarLine(ref Dictionary<Point3d, HashSet<Point3d>> graph, double tolerance = Math.PI / 8)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in graph)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                while (true)
                {
                    var key = dic.Key;
                    if (!graph.ContainsKey(key))
                    {
                        break;
                    }
                    var value = graph[key];
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
                    //if ((graph.ContainsKey(minDegreePairPt.Item2) && graph[minDegreePairPt.Item2].Count > 1) || minDegreePairPt.Item1.DistanceTo(dic.Key) >= minDegreePairPt.Item2.DistanceTo(dic.Key))
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
                    DeleteFromGraph(rmPt, key, ref graph);
                }
            }
        }

        /// <summary>
        /// Merge points whose very close to each other in double line structure
        /// If there is a single line, add a compair one
        /// </summary>
        /// <param name="graph"> 原始数据（可能有误差）</param>
        /// <param name="basePts"> 基于这些点 </param>
        /// <param name="deviation"> 误差</param>
        public static void GraphStandardize(ref Dictionary<Point3d, HashSet<Point3d>> graph, List<Point3d> basePts, double deviation = 1)
        {
            Point3d tmpPtA = new Point3d();
            Point3d tmpPtB = new Point3d();
            var newDicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
            foreach (var dicTuple in graph)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value);
            }
            graph.Clear();
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
                    AddLineToGraph(tmpPtA, tmpPtB, ref graph);
                }
            }
        }

        /// <summary>
        /// 从DicTuple中删除一条双向线
        /// </summary>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <param name="graph"></param>
        public static void DeleteFromGraph(Point3d ptA, Point3d ptB, ref Dictionary<Point3d, HashSet<Point3d>> graph)
        {
            if (graph.ContainsKey(ptA) && graph[ptA].Contains(ptB))
            {
                graph[ptA].Remove(ptB);
                if (graph[ptA].Count == 0)
                {
                    graph.Remove(ptA);
                }
            }
            if (graph.ContainsKey(ptB) && graph[ptB].Contains(ptA))
            {
                graph[ptB].Remove(ptA);
                if (graph[ptB].Count == 0)
                {
                    graph.Remove(ptB);
                }
            }
        }

        /// <summary>
        /// 将一条线加入字典结构
        /// </summary>
        public static void AddLineToGraph(Point3d ptA, Point3d ptB, ref Dictionary<Point3d, HashSet<Point3d>> graph)
        {
            if (!graph.ContainsKey(ptA))
            {
                graph.Add(ptA, new HashSet<Point3d>());
            }
            if (!graph[ptA].Contains(ptB))
            {
                graph[ptA].Add(ptB);
            }
            if (!graph.ContainsKey(ptB))
            {
                graph.Add(ptB, new HashSet<Point3d>());
            }
            if (!graph[ptB].Contains(ptA))
            {
                graph[ptB].Add(ptA);
            }
        }

        /// <summary>
        /// 将两个graph数据集和为一个
        /// </summary>
        /// <param name="graphA"></param>
        /// <param name="graphB"></param>
        public static void MergeGraphAToB(Dictionary<Point3d, HashSet<Point3d>> graphA, ref Dictionary<Point3d, HashSet<Point3d>> graphB)
        {
            foreach(var lineAs in graphA)
            {
                foreach(var lineAed in lineAs.Value)
                {
                    AddLineToGraph(lineAs.Key, lineAed, ref graphB);
                }
            }
        }

        /// <summary>
        /// 一堆线集中，两两相交的话删除长度较长的线
        /// </summary>
        public static void RemoveIntersectLines(ref Dictionary<Point3d, HashSet<Point3d>> graph, double devision = 1.0)
        {
            var tmpTuples = LineDealer.UnifyTuples(graph);
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
                        DeleteFromGraph(tupleA.Item1, tupleA.Item2, ref graph);
                    }
                    else
                    {
                        DeleteFromGraph(tupleB.Item1, tupleB.Item2, ref graph);
                    }
                }
            }
        }


        /// <summary>
        /// 将点的连接增加至4个
        /// </summary>
        /// <param name="dicTuples"></param>
        public static void AddConnectUpToFour(ref Dictionary<Point3d, HashSet<Point3d>> dicTuples, HashSet<Point3d> basePts, HashSet<Point3d> itcNearPts = null, double MaxBeamLength = 13000)
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
                            var ansPt = GetObjects.GetRangePointByDirection(basePt, baseVector.RotateBy(Math.PI / 2 * i, Vector3d.ZAxis), basePts, partice * 5, MaxBeamLength);
                            AddLineToGraph(basePt, ansPt, ref dicTuples);
                        }
                    }
                    else if (cnt == 2)
                    {
                        var vecA = (nowCntPts[0] - basePt).GetNormal();
                        var vecB = (nowCntPts[1] - basePt).GetNormal();
                        double baseAngel = vecA.GetAngleTo(vecB);
                        if (baseAngel > partice * 18 && baseAngel < partice * 22)
                        {
                            var ansPtA = GetObjects.GetRangePointByDirection(basePt, -vecA, basePts, partice * 5, MaxBeamLength);
                            AddLineToGraph(basePt, ansPtA, ref dicTuples);
                            var ansPtB = GetObjects.GetRangePointByDirection(basePt, -vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineToGraph(basePt, ansPtB, ref dicTuples);
                        }
                        else if (baseAngel > Math.PI - partice * 4)
                        {
                            var ansPtA = GetObjects.GetRangePointByDirection(basePt, vecA + vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineToGraph(basePt, ansPtA, ref dicTuples);
                            var ansPtB = GetObjects.GetRangePointByDirection(basePt, -vecA - vecB, basePts, partice * 5, MaxBeamLength);
                            AddLineToGraph(basePt, ansPtB, ref dicTuples);
                        }
                    }
                    else if (cnt == 3)
                    {
                        var findVec = GetDirectionByThreeVecs(basePt, nowCntPts[0], nowCntPts[1], nowCntPts[2]);
                        var ansPt = GetObjects.GetRangePointByDirection(basePt, findVec, basePts, partice * 5, MaxBeamLength);
                        AddLineToGraph(basePt, ansPt, ref dicTuples);
                    }
                }
            }
        }

        /// <summary>
        /// 根据三个方向确定第四个方向
        /// </summary>
        /// <param name="basePt"></param>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <param name="ptC"></param>
        /// <returns></returns>
        public static Vector3d GetDirectionByThreeVecs(Point3d basePt, Point3d ptA, Point3d ptB, Point3d ptC)
        {
            var vecA = ptA - basePt;
            var vecB = ptB - basePt;
            var vecC = ptC - basePt;
            double angelAB = vecA.GetAngleTo(vecB, Vector3d.ZAxis);
            double angelCA = vecC.GetAngleTo(vecA, Vector3d.ZAxis);
            double angelBC = vecB.GetAngleTo(vecC, Vector3d.ZAxis);
            double angelAC = vecA.GetAngleTo(vecC, Vector3d.ZAxis);
            if (angelAB > angelAC)
            {
                vecB = ptC - basePt;
                vecC = ptB - basePt;
                angelAB = vecA.GetAngleTo(vecB, Vector3d.ZAxis);
                angelBC = vecB.GetAngleTo(vecC, Vector3d.ZAxis);
                angelCA = vecC.GetAngleTo(vecA, Vector3d.ZAxis);
            }
            double absAB90 = Math.Abs(angelAB - Math.PI / 2);
            double absCA90 = Math.Abs(angelCA - Math.PI / 2);
            double absBC90 = Math.Abs(angelBC - Math.PI / 2);
            double absAB180 = Math.Abs(angelAB - Math.PI);
            double absCA180 = Math.Abs(angelCA - Math.PI);
            double absBC180 = Math.Abs(angelBC - Math.PI);
            double min90;
            double min180;
            Vector3d vec9X, vec9Y, vec9Z;
            Vector3d vec18X, vec18Y;
            if (absAB90 <= absCA90 && absAB90 <= absBC90)
            {
                min90 = absAB90;
                vec9X = vecA;
                vec9Y = vecB;
                vec9Z = vecC;
            }
            else if (absCA90 <= absBC90)
            {
                min90 = absCA90;
                vec9X = vecC;
                vec9Y = vecA;
                vec9Z = vecB;
            }
            else
            {
                min90 = absBC90;
                vec9X = vecB;
                vec9Y = vecC;
                vec9Z = vecA;
            }
            if (absAB180 <= absCA180 && absAB180 <= absBC180)
            {
                min180 = absAB180;
                vec18X = vecA;
                vec18Y = vecB;
            }
            else if (absCA180 <= absBC180)
            {
                min180 = absCA180;
                vec18X = vecC;
                vec18Y = vecA;
            }
            else
            {
                min180 = absBC180;
                vec18X = vecB;
                vec18Y = vecC;
            }
            if (min90 < min180)
            {
                var angelXY = vec9X.GetAngleTo(vec9Y, Vector3d.ZAxis);
                if (vec9Z.GetAngleTo(vec9X, Vector3d.ZAxis) > Math.PI - angelXY / 2)
                {
                    return vec9X.RotateBy(Math.PI - angelXY, -Vector3d.ZAxis);
                }
                else
                {
                    return vec9Y.RotateBy(Math.PI - angelXY, Vector3d.ZAxis);
                }
            }
            else
            {
                return vec18X.RotateBy((vec18X.GetAngleTo(vec18Y, Vector3d.ZAxis)) / 2, Vector3d.ZAxis);
            }
        }

        /// <summary>
        /// reduce degree up to 4 for each point(删除最不符合90度的那个)
        /// 删除的时候，如果删除点的对点是外边线上的并且只有这一个连线，则不能删掉，其他都可删
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="nearBorderGraph"></param>
        public static void DeleteConnectUpToFour(ref Dictionary<Point3d, HashSet<Point3d>> graph, ref Dictionary<Point3d, HashSet<Point3d>> nearBorderGraph)
        {
            Dictionary<Point3d, List<Point3d>> newDicTuples = new Dictionary<Point3d, List<Point3d>>();
            foreach (var dicTuple in graph)
            {
                newDicTuples.Add(dicTuple.Key, dicTuple.Value.ToList());
            }
            foreach (var dic in newDicTuples)
            {
                var key = dic.Key;
                if (!graph.ContainsKey(key))
                {
                    continue;
                }
                var value = graph[key];
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
                    if (!IsCoreLine(key, minDegreePairPt.Item1, graph))
                    {
                        rmPt = minDegreePairPt.Item1;
                    }
                    else if (!IsCoreLine(key, minDegreePairPt.Item2, graph))
                    {
                        rmPt = minDegreePairPt.Item2;
                    }
                    --n;
                    if (rmPt == new Point3d() || !graph.ContainsKey(rmPt))
                    {
                        continue;
                    }
                    if(nearBorderGraph.ContainsKey(rmPt) && nearBorderGraph[rmPt].Count == 1 || graph[rmPt].Count <= 2)
                    {
                        continue;
                    }
                    value.Remove(rmPt);
                    DeleteFromGraph(key, rmPt, ref nearBorderGraph);
                    DeleteFromGraph(rmPt, key, ref graph);
                }
            }
        }
        public static bool IsCoreLine(Point3d baseFromPt, Point3d baseToPt, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
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
            if (cnt >= 2)
            {
                return true;
            }
            return false;
        }



        /// <summary>
        /// Delete lines who connect with eachother with same class in a line set
        /// </summary>
        public static void RemoveSameClassLine(List<Point3d> points, ref Dictionary<Point3d, HashSet<Point3d>> graph, double deviation = 1)
        {
            HashSet<Point3d> pts = new HashSet<Point3d>();
            HashSet<Point3d> classPts = new HashSet<Point3d>();
            foreach (var pt in points)
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
            foreach (var dic in graph)
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
                    DeleteFromGraph(tuple.Item1, tuple.Item2, ref graph);
                }
            }
        }
    }
}
