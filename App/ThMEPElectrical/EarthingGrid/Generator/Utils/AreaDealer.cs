using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class AreaDealer
    {
        /// <summary>
        /// Build a structure
        /// can find a polyline by any line in this polyline
        /// </summary>
        /// <param name="dicTuples"></param>
        /// <param name="findPolylineFromLines"></param>
        public static void BuildPolygons(Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
        {
            HashSet<Tuple<Point3d, Point3d>> lineVisit = new HashSet<Tuple<Point3d, Point3d>>();
            HashSet<Tuple<Point3d, Point3d>> tuppleList = LineDealer.Graph2Lines(dicTuples);
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

        //线集生成多边形
        public static void BuildPolygonsCustom(Dictionary<Point3d, HashSet<Point3d>> dicTuples, ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
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
                var tuples = LineDealer.Polyline2Tuples(polyline);
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
        /// 从findPolylineFromLines中删除掉outlineWithBorderLine相似的线
        /// </summary>
        /// <param name="findPolylineFromLines"></param>
        /// <param name="outlineWithBorderLine"></param>
        public static void DeleteBuildingLines(ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines,
            Dictionary<Polyline, List<Tuple<Point3d, Point3d>>> outlineWithBorderLine)
        {

        }


        /// <summary>
        /// split block
        /// </summary>
        /// <param name="findPolylineFromLines"></param>
        public static void SplitBlock(ref Dictionary<Tuple<Point3d, Point3d>, List<Tuple<Point3d, Point3d>>> findPolylineFromLines)
        {
            HashSet<Tuple<Point3d, Point3d>> lineVisit = new HashSet<Tuple<Point3d, Point3d>>();
            List<List<Tuple<Point3d, Point3d>>> splitedPolylines = new List<List<Tuple<Point3d, Point3d>>>();
            List<Tuple<Point3d, Point3d>> lines = findPolylineFromLines.Keys.ToList();
            foreach (var line in lines)
            {
                if (!lineVisit.Contains(line))
                {
                    if (findPolylineFromLines[line].Count > 5)
                    {
                        SplitPolyline(findPolylineFromLines[line], ref splitedPolylines);
                    }
                    foreach (var l in findPolylineFromLines[line])
                    {
                        if (!lineVisit.Contains(l))
                        {
                            lineVisit.Add(l);
                        }
                    }
                }
            }
            // change structure
            AddPolylinesToDic(splitedPolylines, ref findPolylineFromLines);
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
            if (n == 0 || n > 10)
            {
                return;
            }

            if (n <= 5)
            {
                var pl = LineDealer.Tuples2Polyline(tuples);
                if (pl.Closed == true)
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

                var plA = LineDealer.Tuples2Polyline(tmpTuplesA);
                var plB = LineDealer.Tuples2Polyline(tmpTuplesB);
                if(plA.Area < 1000000 || plB.Area < 1000000)
                {
                    continue;
                }
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
                            foreach(var ll in findPolylineFromLines[l])
                            {
                                findPolylineFromLines.Remove(ll);
                            }
                            //findPolylineFromLines.Remove(l); //上三行替换下面是重要调整
                        }
                        findPolylineFromLines.Add(l, splitedPolyline);
                    }
                }
            }
        }

        /// <summary>
        /// Split a polylin from certain point to two polyline
        /// </summary>
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
    }
}
