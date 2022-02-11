using System;
using System.Linq;
using System.Collections.Generic;
using AcHelper;
using NFox.Cad;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPStructure.GirderConnect.ConnectMainBeam.Utils
{
    class AreaDealer
    {
        public double SimilarAngle = Math.PI / 8;
        public double SimilarPointsDis = 500;
        public double SplitArea = 0.0;
        public Dictionary<Point3d, HashSet<Point3d>> dicTuples = new Dictionary<Point3d, HashSet<Point3d>>();
        public List<Polyline> Outlines = new List<Polyline>();
        public List<Point3d> zeroPts = new List<Point3d>();

        public AreaDealer(double _SimilarAngle, double _SimilarPointsDis, double _SplitArea, Dictionary<Point3d, HashSet<Point3d>> _dicTuples
            , List<Polyline> _Outlines, List<Point3d> _zeroPts)
        {
            SimilarAngle = _SimilarAngle;
            SimilarPointsDis = _SimilarPointsDis;
            SplitArea = _SplitArea;
            dicTuples = _dicTuples;
            Outlines = _Outlines;
            zeroPts = _zeroPts;
        }

        /// <summary>
        /// 对现有的dicTuples结构进行多边形分割与合并
        /// </summary>
        public Dictionary<Point3d, HashSet<Point3d>> SplitAndMerge()
        {
            //0、预处理
            HashSet<Tuple<Point3d, Point3d>> closeBorderLine = StructureDealer.CloseBorderA(Outlines, dicTuples.Keys.ToList());
            closeBorderLine.ForEach(o => DicTuplesDealer.AddLineTodicTuples(o.Item1, o.Item2, ref dicTuples));
            DicTuplesDealer.RemoveIntersectLines(ref dicTuples);

            //1、分割
            var findPolylineFromLines = new Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>>();
            BuildPolygonsC(dicTuples, ref findPolylineFromLines);
            SplitBlock(ref findPolylineFromLines, SplitArea);

            //1.5、处理数据
            dicTuples.Clear();
            dicTuples = TypeConvertor.Tuples2DicTuples(findPolylineFromLines.Keys.ToHashSet(), zeroPts, 100);
            DicTuplesDealer.SimplifyDicTuples(ref dicTuples, zeroPts, SimilarPointsDis, SimilarAngle);
            findPolylineFromLines.Clear();
            BuildPolygons(dicTuples, ref findPolylineFromLines);
            var ptList = new HashSet<Point3d>();
            findPolylineFromLines.Keys.ForEach(t => {
                if (!ptList.Contains(t.Item1))
                {
                    ptList.Add(t.Item1);
                }
                if (!ptList.Contains(t.Item2))
                {
                    ptList.Add(t.Item2);
                }
            });
            HashSet<Point3d> borderPts = PointsDealer.FindIntersectBorderPt(Outlines, ptList);

            //2、合并
            MergeFragments(ref findPolylineFromLines, borderPts, SplitArea);

            //3、生成结果
            Dictionary<Point3d, HashSet<Point3d>> newDicTuples = TypeConvertor.Tuples2DicTuples(findPolylineFromLines.Keys.ToHashSet(), zeroPts, 100);
            HashSet<Point3d> itcBorderPts = PointsDealer.FindIntersectBorderPt(Outlines, newDicTuples.Keys.ToHashSet());
            LineDealer.DeleteSameClassLine(itcBorderPts, ref newDicTuples);
            DicTuplesDealer.RemoveIntersectLines(ref newDicTuples);
            return newDicTuples;
        }

        /// <summary>
        /// Build a structure
        /// can find a polyline by any line in this polyline
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="findPolylineFromLines"></param>
        public static void BuildPolygons(Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
        {
            HashSet<Tuple<Point3d, Point3d>> lineVisit = new HashSet<Tuple<Point3d, Point3d>>();
            HashSet<Tuple<Point3d, Point3d>> tuppleList = TypeConvertor.DicTuples2Tuples(dicTuples);
            foreach (var tuple in tuppleList)
            {
                if (!lineVisit.Contains(tuple))
                {
                    var tmpLines = new List<Tuple<Point3d, Point3d>>();
                    lineVisit.Add(tuple);
                    tmpLines.Add(tuple);
                    var curTuple = new Tuple<Point3d, Point3d>(tuple.Item1, tuple.Item2);
                    int flag = 0;
                    while (true)
                    {
                        Point3d nextPt = GetNextConnectPoint(curTuple.Item1, curTuple.Item2, dicTuples);

                        curTuple = new Tuple<Point3d, Point3d>(curTuple.Item2, nextPt);
                        if (lineVisit.Contains(curTuple))
                        {
                            if (curTuple.Item2 != nextPt)
                            {
                                flag = 1;
                            }
                            break;
                        }
                        if (!lineVisit.Contains(curTuple))
                        {
                            lineVisit.Add(curTuple);
                        }
                        tmpLines.Add(curTuple);
                        if (nextPt == tuple.Item1) // had find a circle
                        {
                            break;
                        }
                    }
                    //if (LineDealer.Tuples2Polyline(tmpLines).Closed == true && tmpLines.Count > 1 && flag != 1)
                    if (flag != 1)
                    {
                        foreach (var tmpLine in tmpLines)
                        {
                            if (!findPolylineFromLines.ContainsKey(tmpLine))
                            {
                                findPolylineFromLines.Add(tmpLine, tmpLines);
                            }
                        }
                    }
                }
            }
        }
        public static void BuildPolygonsC(Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
        {
            List<Line> lines = new List<Line>();
            foreach (var dicTuple in dicTuples)
            {
                foreach (var point in dicTuple.Value)
                {
                    lines.Add(new Line(dicTuple.Key, point));
                }
            }
            List<Polyline> polylines = lines.ToCollection().PolygonsEx().Cast<Entity>().Where(o => o is Polyline).Cast<Polyline>().ToList();
            foreach (var polyline in polylines)
            {
                var tuples = TypeConvertor.Polyline2Tuples(polyline);
                foreach (var tuple in tuples)
                {
                    if (!findPolylineFromLines.ContainsKey(tuple))
                    {
                        findPolylineFromLines.Add(tuple, tuples);
                    }
                }
            }
        }

        /// <summary>
        /// Get the next point in the polyline based on this corrent line
        /// </summary>
        /// <returns>if the return value equals to baseStPt，means there is no next point，also means this is a leaf point</returns>
        public static Point3d GetNextConnectPoint(Point3d baseStPt, Point3d baseEdPt, Dictionary<Point3d, HashSet<Point3d>> dicTuples)
        {
            double minDegree = double.MaxValue;
            Vector3d baseVec = baseStPt - baseEdPt;
            Point3d aimEdPt = baseStPt;
            double curDegree;
            foreach (var curEdPt in dicTuples[baseEdPt])
            {
                if (curEdPt == baseStPt)
                {
                    continue;
                }
                curDegree = (curEdPt - baseEdPt).GetAngleTo(baseVec, Vector3d.ZAxis);
                if (curDegree < minDegree)
                {
                    minDegree = curDegree;
                    aimEdPt = curEdPt;
                }
            }
            return aimEdPt;
        }


        /// <summary>
        /// split block
        /// </summary>
        /// <param name="findPolylineFromLines"></param>
        public static void SplitBlock(ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines, double SplitArea)
        {
            //record line state: 0.init(exist & havenot seen); 1.visited and chose to stay; 2.vistited and chose to delete
            HashSet<Tuple<Point3d, Point3d>> lineVisit = new HashSet<Tuple<Point3d, Point3d>>();
            List<List<Tuple<Point3d, Point3d>>> splitedPolylines = new List<List<Tuple<Point3d, Point3d>>>();
            List<Tuple<Point3d, Point3d>> lines = findPolylineFromLines.Keys.ToList();
            foreach (var line in lines)
            {
                if (!lineVisit.Contains(line) && findPolylineFromLines.ContainsKey(line))
                {
                    var polyline = TypeConvertor.Tuples2Polyline(findPolylineFromLines[line], 1.0);
                    if ((findPolylineFromLines[line].Count == 5 && (SplitArea != 0 && polyline.Area > SplitArea)) || findPolylineFromLines[line].Count > 5)
                    {
                        SplitPolylineB(findPolylineFromLines[line], ref splitedPolylines, SplitArea);
                        var lList = findPolylineFromLines[line].ToList();
                        foreach (var l in lList)
                        {
                            if (!lineVisit.Contains(l))
                            {
                                lineVisit.Add(l);
                            }
                        }
                    }
                }
            }
            // change structure
            AddPolylinesToDic(splitedPolylines, ref findPolylineFromLines);
        }
        /// <summary>
        /// Merge neighbor fragments to one and split if it can
        /// </summary>
        /// <param name="findPolylineFromLines"></param>
        public static void MergeFragments(ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines, HashSet<Point3d> borderPts, double SplitArea)
        {
            HashSet<Tuple<Point3d, Point3d>> lineVisit = new HashSet<Tuple<Point3d, Point3d>>();
            List<List<Tuple<Point3d, Point3d>>> mergedPolylines = new List<List<Tuple<Point3d, Point3d>>>();
            List<Tuple<Point3d, Point3d>> lines = findPolylineFromLines.Keys.ToList();
            double minDegree;
            double curDegree;
            foreach (var line in lines)
            {
                if (!lineVisit.Contains(line) && findPolylineFromLines[line].Count == 3)
                {
                    int cnt = 0;
                    //如果存在于边界的点的数量大于2，则不进行合并处理
                    foreach (var tuple in findPolylineFromLines[line])
                    {
                        if (borderPts.Contains(tuple.Item1))
                        {
                            ++cnt;
                        }
                        if (borderPts.Contains(tuple.Item2))
                        {
                            ++cnt;
                        }
                        if (cnt > 2)
                        {
                            break;
                        }
                    }
                    if (cnt > 2)
                    {
                        if (!lineVisit.Contains(line))
                        {
                            lineVisit.Add(line);
                        }
                        continue;
                    }
                    mergedPolylines.Clear();
                    Tuple<Point3d, Point3d> curConverseLine;
                    Tuple<Point3d, Point3d> bestSplitLine = line;
                    List<Tuple<Point3d, Point3d>> curEvenLines = new List<Tuple<Point3d, Point3d>>();
                    List<Tuple<Point3d, Point3d>> beastEvenLines = new List<Tuple<Point3d, Point3d>>();
                    minDegree = Math.PI * 2;
                    //合并
                    //找到最合适的那个合并线
                    foreach (var curSplitLine in findPolylineFromLines[line])
                    {
                        curConverseLine = new Tuple<Point3d, Point3d>(curSplitLine.Item2, curSplitLine.Item1);
                        if (!findPolylineFromLines.ContainsKey(curConverseLine) || !findPolylineFromLines.ContainsKey(curSplitLine))
                        {
                            continue;
                        }
                        if (findPolylineFromLines[curConverseLine].Count >= 5)
                        {
                            beastEvenLines = MergePolyline(findPolylineFromLines[curSplitLine], findPolylineFromLines[curConverseLine]);
                            bestSplitLine = curSplitLine;
                            break;
                        }
                        else if (findPolylineFromLines[curConverseLine].Count == 3)
                        {
                            curEvenLines = MergePolyline(findPolylineFromLines[curSplitLine], findPolylineFromLines[curConverseLine]);
                            //该四边形最大角的度数，最小的那个
                            curDegree = LineDealer.GetBiggestAngel(curEvenLines, borderPts);
                            if (curDegree == -1)
                            {
                                continue;
                            }
                            if (curDegree < minDegree)
                            {
                                minDegree = curDegree;
                                bestSplitLine = curSplitLine;
                                beastEvenLines = curEvenLines;
                            }
                        }
                    }
                    foreach (var l in findPolylineFromLines[bestSplitLine])
                    {
                        if (!lineVisit.Contains(l))
                        {
                            lineVisit.Add(l);
                        }
                    }
                    if (LineDealer.ObtuseAngleCount(beastEvenLines) != 0)
                    {
                        continue;
                    }
                    curConverseLine = new Tuple<Point3d, Point3d>(bestSplitLine.Item2, bestSplitLine.Item1);
                    if (!findPolylineFromLines.ContainsKey(curConverseLine))
                    {
                        continue;
                    }
                    foreach (var l in findPolylineFromLines[curConverseLine])
                    {
                        if (!lineVisit.Contains(l))
                        {
                            lineVisit.Add(l);
                        }
                    }
                    if (findPolylineFromLines.ContainsKey(bestSplitLine))
                    {
                        findPolylineFromLines.Remove(bestSplitLine);
                    }
                    curConverseLine = new Tuple<Point3d, Point3d>(bestSplitLine.Item2, bestSplitLine.Item1);
                    if (findPolylineFromLines.ContainsKey(curConverseLine))
                    {
                        findPolylineFromLines.Remove(curConverseLine);
                    }
                    //分割
                    SplitPolylineB(beastEvenLines, ref mergedPolylines, SplitArea);

                    AddPolylinesToDic(mergedPolylines, ref findPolylineFromLines);
                }
            }
        }

        public static void AddPolylinesToDic(List<List<Tuple<Point3d, Point3d>>> splitedPolylines,
            ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
        {
            foreach (var splitedPolyline in splitedPolylines)
            {
                if (splitedPolyline.Count > 0)
                {
                    foreach (var l in splitedPolyline)
                    {
                        if (findPolylineFromLines.ContainsKey(l))
                        {
                            findPolylineFromLines.Remove(l);
                        }
                        findPolylineFromLines.Add(l, splitedPolyline);
                    }
                }
            }
        }

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
                if (TypeConvertor.Tuples2Polyline(tuples).Closed == true)
                {
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
                Split2Order(tuples, splitA, splitB, ref tmpTuplesA, ref tmpTuplesB);
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
        public static void SplitPolylineB(List<Tuple<Point3d, Point3d>> tuples, ref List<List<Tuple<Point3d, Point3d>>> tupleLines, double SplitArea)
        {
            //Recursion boundary
            int n = tuples.Count;
            if (n == 0 || n > 10)
            {
                return;
            }
            var polyline = TypeConvertor.Tuples2Polyline(tuples);

            if (n < 5 || (n == 5 && (SplitArea == 0 || polyline.Area < SplitArea)))
            {
                if (TypeConvertor.Tuples2Polyline(tuples).Closed == true)
                {
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
            //double minCross = double.MaxValue;
            //double halfArea = polyline.Area / 2;
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
                Split2Order(tuples, splitA, splitB, ref tmpTuplesA, ref tmpTuplesB);
                //var polylineA = LineDealer.Tuples2Polyline(tmpTuplesA);
                //var polylineB = LineDealer.Tuples2Polyline(tmpTuplesB);
                //var curCross = Math.Pow(curdis, 2) * (polylineA.Area - halfArea);
                //if (curCross < minCross)
                if (curdis < mindis)
                {
                    //minCross = curCross;
                    mindis = curdis;
                    tuplesA = tmpTuplesA;
                    tuplesB = tmpTuplesB;
                }
            }
            //Tail Recursion
            SplitPolylineB(tuplesA, ref tupleLines, SplitArea);
            SplitPolylineB(tuplesB, ref tupleLines, SplitArea);
        }

        /// <summary>
        /// Split a polylin from certain point to two polyline
        /// </summary>
        /// <param name="tuples"></param>
        /// <param name="splitA"></param>
        /// <param name="splitB"></param>
        /// <param name="tuplesA">in & out</param>
        /// <param name="tuplesB">in & out</param>
        public static void Split2Order(List<Tuple<Point3d, Point3d>> tuples, int splitA, int splitB, ref List<Tuple<Point3d, Point3d>> tuplesA, ref List<Tuple<Point3d, Point3d>> tuplesB)
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
    }
}
