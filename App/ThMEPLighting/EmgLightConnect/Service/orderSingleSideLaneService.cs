using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class orderSingleSideLaneService
    {
        public static void orderOutterSingleSideLane(List<List<Line>> mergedOrderedLane, Polyline frame, out Dictionary<Point3d, List<Line>> pointList, out List<(Line, int)> notTravelledList, out List<List<(Line, int)>> orderedOutterLaneSideList)
        {
            pointList = laneNode(mergedOrderedLane);
            notTravelledList = initialNotTravelledList(mergedOrderedLane);
            var ptOnFrame = initialPtOnFrame(pointList, frame);
            orderedOutterLaneSideList = new List<List<(Line, int)>>();
            var startLine = pointList[ptOnFrame[0]][0];

            //var orderedInSideList = new Dictionary<Line, List<List<(Line, int)>>>();

            //处理外环
            bool bEnd = false;
            while (bEnd == false)
            {
                var orderedLaneSide = new List<(Line, int)>();
                orderSingleSideToRing(pointList, startLine, 0, ref orderedLaneSide);

                if (orderedLaneSide.Count > 0)
                {
                    orderedOutterLaneSideList.Add(orderedLaneSide);
                }

                bEnd = removePassedPtOnFrame(orderedLaneSide, ptOnFrame);

                if (bEnd == false)
                {
                    startLine = pointList[ptOnFrame[0]][0];

                }

            }

            removeTravelledLane(orderedOutterLaneSideList, notTravelledList);

        }

        public static void orderInnerSigleSideLane(Dictionary<Point3d, List<Line>> pointList, List<(Line, int)> notTravelledList, List<List<(Line, int)>> orderedOutterLaneSideList, out Dictionary<int, List<List<(Line, int)>>> orderedInnerLaneSideList)
        {
            orderedInnerLaneSideList = new Dictionary<int, List<List<(Line, int)>>>();
            int dictIndex = 1;
            orderedInnerLaneSideList.Add(0, orderedOutterLaneSideList);

            //处理内环/////////
            while (notTravelledList.Count > 0)
            {
                var insideRingFrame = notTravelledList.GroupBy(x => x.Item1).Where(x => x.Count() == 1).Select(x => x.FirstOrDefault()).ToList();
                var inDiffRingDict = inDifferentRing(insideRingFrame, orderedInnerLaneSideList);

                //属于外层不同环的内层环
                foreach (var insideRingKeyValue in inDiffRingDict)
                {
                    var insideRingList = insideRingKeyValue.Value;
                    //内层环有可能被不同层包裹，先清除以加入list的边
                    removeTravelledLane(orderedInnerLaneSideList, insideRingList);

                    if (insideRingList.Count == 0)
                    {
                        continue;
                    }

                    var orderedLaneSideList = new List<List<(Line, int)>>();
                    
                    while (insideRingList.Count > 0)
                    {
                        var startLine = insideRingList[0].Item1;
                        var side = insideRingList[0].Item2;
                        var orderedLaneSide = new List<(Line, int)>();

                        orderSingleSideToRing(pointList, startLine, side, ref orderedLaneSide);

                        if (orderedLaneSide.Count > 0)
                        {
                            orderedLaneSideList.Add(orderedLaneSide);
                        }

                        removeTravelledLane(orderedLaneSideList, insideRingList);

                    }

                    orderedInnerLaneSideList.Add(dictIndex, orderedLaneSideList);
                    dictIndex = dictIndex + 1;

                }

                removeTravelledLane(orderedInnerLaneSideList, notTravelledList);

            }
        }

        private static bool removeTravelledLane(List<List<(Line, int)>> orderedLaneSideList, List<(Line, int)> notTravelledList)
        {
            var bEnd = false;
            orderedLaneSideList.ForEach(y => y.ForEach(x => notTravelledList.Remove((x.Item1, x.Item2))));

            if (notTravelledList.Count > 0)
            {
                bEnd = false;
            }
            else
            {
                bEnd = true;
            }

            return bEnd;
        }

        private static bool removeTravelledLane(Dictionary<int, List<List<(Line, int)>>> orderedInnerLaneSideList, List<(Line, int)> notTravelledList)
        {
            var bEnd = false;

            foreach (var orderedLaneSideList in orderedInnerLaneSideList)
            {
                orderedLaneSideList.Value.ForEach(y => y.ForEach(x => notTravelledList.Remove((x.Item1, x.Item2))));
            }


            if (notTravelledList.Count > 0)
            {
                bEnd = false;
            }
            else
            {
                bEnd = true;
            }

            return bEnd;
        }

        private static bool removePassedPtOnFrame(List<(Line, int)> orderedLaneSide, List<Point3d> ptOnFrame)
        {
            var bEnd = false;

            orderedLaneSide.ForEach(lane =>
                {
                    ptOnFrame.Remove(ptOnFrame.Where(pt => pt.IsEqualTo (lane.Item1.StartPoint,new Tolerance (1,1))).FirstOrDefault());
                    ptOnFrame.Remove(ptOnFrame.Where(pt => pt.IsEqualTo(lane.Item1.EndPoint , new Tolerance(1, 1))).FirstOrDefault());
                });

            if (ptOnFrame.Count > 0)
            {
                bEnd = false;
            }
            else
            {
                bEnd = true;
            }

            return bEnd;

        }

        /// <summary>
        /// 从线1开始,沿左边走。如果线1尾点连接别的线2，3...，将线2,3...向量 v2'(2另一点-2本点),v3'... 到 (-v1' 起点到终点，未必是线1的起点终点)的顺时针夹角最小，若v2与v2'同向,取v2左边，相反取v2右边
        /// 如果v2 的另一端点为端点，则新起一组从v2另一侧开始，.....到 (-v1)的顺时针夹角最小，若v2与v2'同反,取v2右边，相同取v2左边
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="thisLine"></param>
        /// <param name="side"></param>
        /// <param name="orderedLaneSide"></param>
        private static void orderSingleSideToRing(Dictionary<Point3d, List<Line>> pointList, Line thisLine, int side, ref List<(Line, int)> orderedLaneSide)
        {
            Point3d pt = thisLine.EndPoint;
            int zAxis = side == 0 ? -1 : 1;
            orderedLaneSide.Add((thisLine, side));
            var bEnd = false;

            while (bEnd == false)
            {
                var thisLineOrder = isStartPoint(pt, thisLine, out Vector3d thisLineVectorRevs);
                ptInPtList(pointList, pt, out var ptKey);
                if (ptKey.Value.Count > 1)
                {
                    var nextCollect = pointList[ptKey.Key].Where(x => x != thisLine).ToList();
                    var minAngle = 2 * Math.PI;
                    var nextLine = nextCollect[0];
                    var nextOrder = false;

                    foreach (var nextLineTemp in nextCollect)
                    {
                        var nextOrderTemp = isStartPoint(pt, nextLineTemp, out var nextLineVTemp);
                        var angle = thisLineVectorRevs.GetAngleTo(nextLineVTemp, zAxis * Vector3d.ZAxis);
                        if (angle <= minAngle)
                        {
                            minAngle = angle;
                            nextLine = nextLineTemp;
                            nextOrder = nextOrderTemp;
                        }
                    }

                    thisLine = nextLine;
                    if (nextOrder == true)
                    {
                        pt = thisLine.EndPoint;
                    }
                    else
                    {
                        pt = thisLine.StartPoint;
                    }

                    if (nextOrder == thisLineOrder)
                    {
                        side = side == 0 ? 1 : 0;
                    }

                    bEnd = checkLaneSideAlreadyTravelled(orderedLaneSide, thisLine, side);

                    if (bEnd == false)
                    {
                        orderedLaneSide.Add((thisLine, side));
                    }
                }
                else
                {

                    if (thisLineOrder == true)
                    {
                        pt = thisLine.EndPoint;
                    }
                    else
                    {
                        pt = thisLine.StartPoint;
                    }

                    side = side == 0 ? 1 : 0;

                    bEnd = checkLaneSideAlreadyTravelled(orderedLaneSide, thisLine, side);

                    if (bEnd == false)
                    {

                        orderedLaneSide.Add((thisLine, side));
                    }
                }
            }
        }

        private static bool checkLaneSideAlreadyTravelled(List<(Line, int)> orderedLaneSide, Line thisLine, int side)
        {
            var bEnd = false;
            var alreadyTravelled = orderedLaneSide.Where(x => x.Item1 == thisLine && x.Item2 == side).ToList();

            if (alreadyTravelled.Count > 0)
            {
                bEnd = true;
            }
            else
            {
                bEnd = false;
            }

            return bEnd;
        }

        private static bool isStartPoint(Point3d pt, Line line, out Vector3d lineVector)
        {
            bool isStartPoint = false;
            if (line.StartPoint.IsEqualTo(pt,new Tolerance (1,1)) )
            {
                lineVector = line.EndPoint - line.StartPoint;
                isStartPoint = true;
            }
            else
            {
                lineVector = line.StartPoint - line.EndPoint;
                isStartPoint = false;
            }

            return isStartPoint;
        }

        private static Dictionary<Point3d, List<Line>> laneNode(List<List<Line>> mergedOrderedLane)
        {
            var pointList = new Dictionary<Point3d, List<Line>>();


            for (var i = 0; i < mergedOrderedLane.Count; i++)
            {
                for (var j = 0; j < mergedOrderedLane[i].Count; j++)
                {

                    if (ptInPtList(pointList, mergedOrderedLane[i][j].StartPoint, out var ptKey) == false)
                    {
                        pointList.Add(mergedOrderedLane[i][j].StartPoint, new List<Line> { mergedOrderedLane[i][j] });
                    }
                    else
                    {
                        pointList[ptKey.Key].Add(mergedOrderedLane[i][j]);
                    }

                    if (ptInPtList(pointList, mergedOrderedLane[i][j].EndPoint, out ptKey) == false)
                    {
                        pointList.Add(mergedOrderedLane[i][j].EndPoint, new List<Line> { mergedOrderedLane[i][j] });
                    }
                    else
                    {
                        pointList[ptKey.Key].Add(mergedOrderedLane[i][j]);
                    }
                }
            }

            return pointList;
        }

        /// <summary>
        /// 必须有。只判断是否点是否被包含有机会xy是一样的但是containskey返回错误的值
        /// </summary>
        /// <returns></returns>
        public static bool ptInPtList(Dictionary<Point3d, List<Line>> pointList, Point3d pt, out KeyValuePair<Point3d, List<Line>> ptKey)
        {
            var bIn = false;
            ptKey = new KeyValuePair<Point3d, List<Line>>();

            foreach (var ptl in pointList)
            {
                if (ptl.Key.IsEqualTo (pt,new Tolerance (1,1)))
                {
                    ptKey = ptl;
                    bIn = true;
                    break;
                }
            }

            return bIn;

        }

        private static List<(Line, int)> initialNotTravelledList(List<List<Line>> mergedOrderedLane)
        {
            var notTravelledList = new List<(Line, int)>();
            mergedOrderedLane.ForEach(x => x.ForEach(l =>
            {
                notTravelledList.Add((l, 0));
                notTravelledList.Add((l, 1));
            }));

            return notTravelledList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pointList"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        private static List<Point3d> initialPtOnFrame(Dictionary<Point3d, List<Line>> pointList, Polyline frame)
        {
            var ptListOrder = new List<Point3d>();
            var ptList = pointList.Where(x => x.Value.Count == 1).Select(x => x.Key).ToList();


            var ptListOnFrame = ptList.ToDictionary(x => x, x => frame.GetClosestPointTo(x, false));
            ptListOnFrame = ptListOnFrame.Where(x => x.Value.DistanceTo(x.Key) <= EmgConnectCommon.TolLaneEndOnFrame).ToDictionary(x => x.Key, x => x.Value);

            for (var i = 0; i < frame.NumberOfVertices - 1; i++)
            {
                var seg = new Line(frame.GetPoint3dAt(i), frame.GetPoint3dAt(i + 1));
                var ptOnSeg = ptListOnFrame.Where(x => seg.ToCurve3d().IsOn(x.Value, new Tolerance(1, 1))).Select(x => x.Key).ToList();

                if (ptOnSeg.Count == 1)
                {
                    ptListOrder.Add(ptOnSeg[0]);
                }
                else if (ptOnSeg.Count > 1)
                {
                    ptOnSeg = ptOnSeg.OrderBy(x => x.DistanceTo(seg.StartPoint)).ToList();
                    ptListOrder.AddRange(ptOnSeg);
                }
            }

            return ptListOrder;

        }

        private static Dictionary<int, List<(Line, int)>> inDifferentRing(List<(Line, int)> insideRingFrame, Dictionary<int, List<List<(Line, int)>>> orderedInnerLaneSideList)
        {

            var dictInsideFrame = new Dictionary<int, List<(Line, int)>>();

            Dictionary<(int, int), int> areadyAdded = new Dictionary<(int, int), int>();


            for (int i = 0; i < insideRingFrame.Count; i++)
            {

                var side = insideRingFrame[i];

                for (int j = 0; j < orderedInnerLaneSideList.Count; j++)
                {
                    var upperLevelRing = orderedInnerLaneSideList.ElementAt(j);
                    var upperLevelRingLine = upperLevelRing.Value.Select(x => x.Select(y => y.Item1).ToList()).ToList();

                    var isInUpperRing = upperLevelRingLine.IndexOf(upperLevelRingLine.Where(x => x.Contains(side.Item1)).FirstOrDefault());
                    if (isInUpperRing >= 0)
                    {
                        if (areadyAdded.ContainsKey((j, isInUpperRing)) == false)
                        {
                            areadyAdded.Add((j, isInUpperRing), areadyAdded.Count);
                            var list = new List<(Line, int)>();
                            dictInsideFrame.Add(areadyAdded[(j, isInUpperRing)], list);

                        }

                        dictInsideFrame[areadyAdded[(j, isInUpperRing)]].Add(side);

                        break;
                    }
                }
            }

            return dictInsideFrame;
        }

    }
}
