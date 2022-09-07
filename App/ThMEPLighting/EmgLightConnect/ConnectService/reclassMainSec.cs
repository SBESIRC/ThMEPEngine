using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThMEPEngineCore.Diagnostics;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class reclassMainSec
    {
        public static void regroupMainSec(List<ThSingleSideBlocks> singleSideBlocks, Polyline frame, List<Polyline> holes)
        {
            for (int i = 0; i < singleSideBlocks.Count; i++)
            {
                var side = singleSideBlocks[i];
                if (side.Count > 0)
                {
                    regroupMainSec(side);
                    side.orderReMainBlk();

                    breakMainGroup(side, frame, holes);
                    side.orderReMainBlk();

                }
            }
        }

        private static void regroupMainSec(ThSingleSideBlocks side)
        {
            int TolCloseDist = 100;
            List<Point3d> regroupMain = new List<Point3d>();
            List<double> yTransValue = new List<double>();
            var allBlk = side.getTotalBlock();

            //var allBlkDict = allBlk.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(item => item.Value.X).ToList();

            var allBlkDict = allBlk.ToDictionary(x => x, x => side.transformPtToLaneWithAccurateY(x)).OrderBy(item => item.Value.X).ToList();
            var laneEndPt = side.laneSide.Last().Item1.EndPoint.TransformBy(side.Matrix.Inverse());

            if (side.mainBlk.Count > 0)
            {
                yTransValue = allBlkDict.Where(x => side.mainBlk.Contains(x.Key)).Select(x => Math.Abs(x.Value.Y)).ToList();
                var yDebugDict = allBlkDict.Where(x => side.mainBlk.Contains(x.Key)).ToDictionary(x => x.Key, x => Math.Abs(x.Value.Y));

                double Ymin = 0;
                double YMax = 0;

                Ymin = yTransValue.Min() - EmgConnectCommon.TolRegroupMainYRange;
                YMax = yTransValue.Max() + EmgConnectCommon.TolRegroupMainYRange;

                if (yTransValue.Max() - yTransValue.Min() > 3000)
                {
                    double ySecMean = 20000;
                    ySecMean = yTransValue
                               .OrderBy(x => x)
                               .GroupBy(x => Math.Floor(x / 10))
                               .OrderByDescending(x => x.Count())
                               .First()
                               .ToList()
                               .First();
                    Ymin = ySecMean - EmgConnectCommon.TolRegroupMainYRange;
                    YMax = ySecMean + EmgConnectCommon.TolRegroupMainYRange;
                }
                double dSide = allBlkDict.Where(x => x.Key.IsEqualTo(side.mainBlk[0], new Tolerance(10, 10))).FirstOrDefault().Value.Y;

                regroupMain = allBlkDict.Where(x => dSide * x.Value.Y > 0
                                            && Ymin <= Math.Abs(x.Value.Y) && Math.Abs(x.Value.Y) <= YMax
                                            && x.Value.X > -EmgConnectCommon.TolRegroupMainYRange && x.Value.X <= laneEndPt.X + EmgConnectCommon.TolRegroupMainYRange)
                                        .Select(x => x.Key).ToList();

                //regroupMain.AddRange(side.mainBlk);
                regroupMain = regroupMain.Distinct().ToList();
            }
            else if (side.secBlk.Count > 0)
            {
                var tolLane = EmgConnectCommon.TolGroupBlkLane;
                //tolLane = 6000;
                //没有主块的线
                yTransValue = allBlkDict.Where(x => side.secBlk.Contains(x.Key) && Math.Abs(x.Value.Y) <= tolLane).Select(x => x.Value.Y).ToList();
                if (yTransValue.Count == 0)
                {
                    yTransValue = allBlkDict.Where(x => side.secBlk.Contains(x.Key)).Select(x => x.Value.Y).ToList();
                }

                double ySecMean = 20000;
                ySecMean = yTransValue
                           .OrderBy(x => x)
                           .GroupBy(x => Math.Floor(x / 10))
                           .OrderByDescending(x => x.Count())
                           .First()
                           .ToList()
                           .First();

                //if (ySecMean != 20000)
                if (ySecMean != 20000 && Math.Abs(ySecMean) > TolCloseDist) //不在车道线上的点
                {
                    regroupMain = allBlkDict.Where(x => Math.Abs(Math.Abs(x.Value.Y) - Math.Abs(ySecMean)) < EmgConnectCommon.TolRegroupMainYRange).Select(x => x.Key).ToList();
                }
            }

            var regroupSecBlk = allBlk.Where(x => regroupMain.Contains(x) == false).ToList();

            side.setReMainBlk(regroupMain);
            side.setReSecBlk(regroupSecBlk);

        }


        private static void breakMainGroup(ThSingleSideBlocks side, Polyline frame, List<Polyline> holes)
        {
            List<Point3d> regroupMain = new List<Point3d>();

            List<(Polyline, List<Point3d>)> movedLaneList = new List<(Polyline, List<Point3d>)>();

            var tempMoveLineList = moveLane(side, out double offset, out int offSetDir);
            if (tempMoveLineList.Count > 0)
            {

                var offsetList = checkMoveLineIntersectOutFrame(tempMoveLineList, frame, holes, offset, side, out var lanePoly, out var moveLanePoly);

                if (offsetList.Count == 0)
                {
                    //remain blk 只有一个块时
                    movedLaneList.Add((moveLanePoly, side.reMainBlk));
                }
                else
                {
                    var offsetStEdPt = getMoveLaneSegStartEndPt(offsetList, side.Matrix);
                    var offsetListBlk = classifyBlkToLaneSeg(side, offsetStEdPt);

                    for (int i = 0; i < offsetList.Count; i++)
                    {
                        if (offsetListBlk.ContainsKey(i))
                        {
                            var newOffsetPt = getMoveLaneListAll(side, offsetList[i], offsetListBlk[i], lanePoly, -offSetDir);

                            if (newOffsetPt.Item1.NumberOfVertices > 0)
                            {
                                movedLaneList.Add(newOffsetPt);

                            }
                            newOffsetPt.Item2.Where(x => side.reMainBlk.Contains(x) == false).ForEach(y => side.reMainBlk.Add(y));
                        }
                    }
                }


                //  addRemainMainBlk(side.reMainBlk, ref movedLaneList);


            }

            side.setMoveLaneList(movedLaneList);

        }

        private static void addRemainMainBlk(List<Point3d> reMBlk, ref List<(Polyline, List<Point3d>)> movedLaneList)
        {
            var reMBlkClone = new List<Point3d>();
            reMBlkClone.AddRange(reMBlk);

            movedLaneList.ForEach(l => l.Item2.ForEach(blk => reMBlkClone.Remove(blk)));

            for (int i = 0; i < reMBlkClone.Count; i++)
            {
                var pt = reMBlkClone[i];
                var minDist = movedLaneList[0].Item1.GetDistToPoint(pt);
                var minIdx = 0;

                for (int j = 0; j < movedLaneList.Count; j++)
                {
                    var dist = movedLaneList[j].Item1.GetDistToPoint(pt);
                    if (dist < minDist)
                    {
                        minIdx = j;
                    }
                }
                movedLaneList[minIdx].Item2.Add(pt);
            }
        }

        /// <summary>
        /// 平移车道线
        /// </summary>
        /// <param name="side"></param>
        /// <param name="offset"></param>
        /// <param name="offSetDir"></param>
        /// <returns>平移好的车道线</returns>
        private static List<Line> moveLane(ThSingleSideBlocks side, out double offset, out int offSetDir)
        {
            //找平移量
            List<Line> lane = side.laneSide.Select(x => x.Item1).ToList();
            offset = getLaneDisplacement(lane, side.reMainBlk, EmgConnectCommon.TolSaperateGroupMaxDistance);
            var tempMoveLineList = new List<Line>();
            offSetDir = 0;
            //平移车道线
            if (offset != 0)
            {
                for (int j = 0; j < side.laneSide.Count; j++)
                {
                    //GetOffsetCurves line 负值：右 正值：左  polyline 负值：左 正值： 右
                    offSetDir = side.laneSide[j].Item2 == 0 ? 1 : -1;
                    var tempMoveLine = side.laneSide[j].Item1.GetOffsetCurves(offset * offSetDir)[0] as Line;

                    if (j > 0)
                    {
                        if (tempMoveLine.StartPoint.IsEqualTo(tempMoveLineList.Last().EndPoint, new Tolerance(10, 10)) == false)
                        {
                            tempMoveLine.StartPoint = tempMoveLineList.Last().EndPoint;

                        }
                    }

                    tempMoveLineList.Add(tempMoveLine);
                }
            }
            return tempMoveLineList;
        }

        /// <summary>
        /// 根据外框和洞切割平移车道线并找到每段的偏移量
        /// </summary>
        /// <param name="tempMoveLine"></param>
        /// <param name="frame"></param>
        /// <param name="holes"></param>
        /// <param name="offset"></param>
        /// <param name="side"></param>
        /// <param name="lanePoly"></param>
        /// <param name="moveLanePoly"></param>
        /// <returns>偏移量，这个偏移量的line</returns>
        private static List<(double, List<Line>)> checkMoveLineIntersectOutFrame(List<Line> tempMoveLine, Polyline frame, List<Polyline> holes, double offset, ThSingleSideBlocks side, out Polyline lanePoly, out Polyline moveLanePoly)
        {
            List<(double, List<Line>)> offsetList = new List<(double, List<Line>)>();
            var lane = side.laneSide.Select(x => x.Item1).ToList();
            lanePoly = new Polyline();
            moveLanePoly = new Polyline();
            if (side.reMainBlk.Count > 1)
            {
                lanePoly = getCutLane(lane, side.reMainBlk.First(), side.reMainBlk.Last());
                var moveLineTemp = getCutLane(tempMoveLine, side.reMainBlk.First(), side.reMainBlk.Last());
                moveLanePoly = moveLineTemp.Clone() as Polyline;
                DrawUtils.ShowGeometry(moveLanePoly, "l0moveLanePoly", 76);
                var checkIntersectPoly = lanePoly.Clone() as Polyline;

                for (int i = moveLineTemp.NumberOfVertices - 1; i >= 0; i--)
                {
                    checkIntersectPoly.AddVertexAt(checkIntersectPoly.NumberOfVertices, moveLineTemp.GetPoint3dAt(i).ToPoint2d(), 0, 0, 0);
                }
                checkIntersectPoly.Closed = true;

                var pts = moveLineTemp.Intersect(frame, Intersect.OnBothOperands);
                if (pts.Count > 0)
                {
                    var polyCollection = new DBObjectCollection() { frame };
                    var overlap = checkIntersectPoly.Intersection(polyCollection);

                    if (overlap.Count > 0)
                    {
                        checkIntersectPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();
                    }
                }

                foreach (var f in holes)
                {
                    pts = moveLineTemp.Intersect(f, Intersect.OnBothOperands);
                    if (pts.Count > 0)
                    {
                        var polyCollection = new DBObjectCollection() { f };
                        var overlap = checkIntersectPoly.Difference(polyCollection);

                        if (overlap.Count > 0)
                        {
                            checkIntersectPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();
                        }
                    }
                }

                if (checkIntersectPoly != null && checkIntersectPoly.NumberOfVertices > 1)
                {
                    offsetList = getNewOffsetSeg(checkIntersectPoly, lanePoly, offset);
                    DrawUtils.ShowGeometry(checkIntersectPoly, "l0movedLanePoly");
                }

                //check offsetList：检查一些只有一个主点一个副点但是一组没卡出偏移线的情况
                for (int i = offsetList.Count - 1; i >= 0; i--)
                {
                    var offSetL = offsetList[i];
                    for (int j = offSetL.Item2.Count - 1; j >= 0; j--)
                    {
                        if (offSetL.Item2[j].Length < 1)
                        {
                            offSetL.Item2.RemoveAt(j);
                        }
                    }
                    if (offSetL.Item2.Count == 0)
                    {
                        offsetList.RemoveAt(i);
                    }
                }
                if (offsetList .Count ==0 )
                {
                    moveLanePoly = new Polyline();
                    lanePoly = new Polyline();
                }
            }

            return offsetList;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="overlapPoly"></param>
        /// <param name="lanePoly"></param>
        /// <param name="offsetOri"></param>
        /// <returns>overlapPoly 切分成line。找到连续且共同偏移量double，合并line 到list</returns>
        private static List<(double, List<Line>)> getNewOffsetSeg(Polyline overlapPoly, Polyline lanePoly, double offsetOri)
        {
            int TolDistDalta = 10;
            int TolCloseDist = 100;

            List<(double, List<Line>)> offsetList = new List<(double, List<Line>)>();
            double minDist = offsetOri;
            bool passLane = false;

            //turnOverLap poly!!!!

            for (int i = 1; i < overlapPoly.NumberOfVertices; i++)
            {
                var dist = lanePoly.GetDistToPoint(overlapPoly.GetPoint3dAt(i), false);
                var distDalta = Math.Abs(minDist - dist);
                var lineSeg = new Line(overlapPoly.GetPoint3dAt(i - 1), overlapPoly.GetPoint3dAt(i));


                var lineDir = (overlapPoly.GetPoint3dAt(i - 1) - overlapPoly.GetPoint3dAt(i)).GetNormal();
                var laneDir = (lanePoly.EndPoint - lanePoly.StartPoint).GetNormal();

                var angle = lineDir.GetAngleTo(laneDir, Vector3d.ZAxis);

                if (Math.Abs(Math.Cos(angle)) < Math.Cos(45 * Math.PI / 180))
                {
                    continue;
                }
                if (TolCloseDist > dist)
                {
                    passLane = true;
                }
                if (TolCloseDist < dist && distDalta > TolDistDalta)
                {
                    offsetList.Add((dist, new List<Line> { lineSeg }));
                    minDist = dist;
                }
                else if (passLane == true && TolCloseDist < dist)
                {
                    offsetList.Add((dist, new List<Line> { lineSeg }));
                    minDist = dist;
                    passLane = false;
                }
                else if (TolCloseDist < dist && distDalta <= TolDistDalta)
                {
                    if (dist <= minDist)
                    {
                        if (offsetList.Count > 0)
                        {
                            if (dist != minDist)
                            {
                                var segListTemp = offsetList.Last().Item2;
                                segListTemp.Add(lineSeg);
                                offsetList.Add((dist, segListTemp));
                                offsetList.RemoveAt(offsetList.Count - 2);
                            }
                            else
                            {
                                offsetList.Last().Item2.Add(lineSeg);
                            }
                        }
                        else
                        {
                            offsetList.Add((dist, new List<Line> { lineSeg }));
                        }

                        minDist = dist;
                    }
                    else
                    {
                        offsetList.Last().Item2.Add(lineSeg);
                    }
                }
            }

            return offsetList;
        }

        private static double getLaneDisplacement(List<Line> lanes, List<Point3d> blocks, double maxOffset)
        {
            var displacementList = new List<double>();
            double distance = 0;

            foreach (var blk in blocks)
            {
                displacementList.Add(lanes.Select(x => x.GetDistToPoint(blk, false)).Min());
            }

            var distanceListWithinOff = displacementList
                            .Where(x => x < maxOffset);

            if (distanceListWithinOff.Count() > 0)
            {
                distance = distanceListWithinOff
                               .OrderBy(x => x)
                               .GroupBy(x => Math.Floor(x / 10))
                               .OrderByDescending(x => x.Count()).ThenByDescending(x => x.Key)
                               .First()
                               .ToList()
                               .First();
            }
            else
            {
                distance = maxOffset;
            }

            return distance;
        }

        private static Dictionary<int, List<KeyValuePair<Point3d, Point3d>>> classifyBlkToLaneSeg(ThSingleSideBlocks side, List<(int, KeyValuePair<Point3d, Point3d>, KeyValuePair<Point3d, Point3d>)> offsetOrderList)
        {
            Dictionary<int, List<KeyValuePair<Point3d, Point3d>>> offsetBlk = new Dictionary<int, List<KeyValuePair<Point3d, Point3d>>>();

            var allBlk = side.getTotalBlock();
            //var allBlkDict = allBlk.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(item => item.Value.X).ToList();
            var allBlkDict = allBlk.ToDictionary(x => x, x => side.transformPtToLaneWithAccurateY(x)).OrderBy(item => item.Value.X).ToList();

            foreach (var blk in allBlkDict)
            {

                var offsetSegListTemp = offsetOrderList.Where(x => ptInSeg(blk.Value, x.Item2.Value, x.Item3.Value)).ToList();
                int offsetBlkKey = -1;

                if (offsetSegListTemp.Count() == 1)
                {
                    offsetBlkKey = offsetSegListTemp.First().Item1;
                }
                else if (offsetSegListTemp.Count() > 1)
                {
                    offsetBlkKey = findCloseSeg(blk, offsetSegListTemp);
                }
                else if (offsetSegListTemp.Count == 0)
                {
                    offsetBlkKey = findCloseSeg(blk, offsetOrderList);
                }


                if (offsetBlk.ContainsKey(offsetBlkKey) == false)
                {
                    offsetBlk.Add(offsetBlkKey, new List<KeyValuePair<Point3d, Point3d>> { blk });
                }
                else
                {
                    offsetBlk[offsetBlkKey].Add(blk);
                }

            }

            return offsetBlk;
        }

        private static int findCloseSeg(KeyValuePair<Point3d, Point3d> blk, List<(int, KeyValuePair<Point3d, Point3d>, KeyValuePair<Point3d, Point3d>)> offsetSegList)
        {
            int offsetKey = -1;
            double minDist = 200000;

            for (int i = 0; i < offsetSegList.Count; i++)
            {
                var dist = blk.Key.DistanceTo(offsetSegList[i].Item2.Key);
                if (dist <= minDist)
                {
                    minDist = dist;
                    offsetKey = offsetSegList[i].Item1;
                }
                dist = blk.Key.DistanceTo(offsetSegList[i].Item3.Key);
                if (dist <= minDist)
                {
                    minDist = dist;
                    offsetKey = offsetSegList[i].Item1;
                }
            }

            return offsetKey;
        }

        private static (Polyline, List<Point3d>) getMoveLaneListAll(ThSingleSideBlocks side, (double, List<Line>) offset, List<KeyValuePair<Point3d, Point3d>> offsetListBlk, Polyline lanePoly, int dir)
        {

            var distanceList = new List<double>();
            double distance = -1;
            Polyline moveLine = new Polyline();

            var newPointDistList = new List<Point3d>();
            var newReMainPointList = new List<Point3d>();

            int TolDistDalta = 1500;
            var offsetMax = offset.Item1 + 10;

            double sideY = side.reMainBlk[0].TransformBy(side.Matrix.Inverse()).Y;

            foreach (var blk in offsetListBlk)
            {
                //y比maxoffset小且移动不大的，在线段内的点都算 为了计算真实的offset
                if (blk.Value.Y * sideY > 0 &&
                    Math.Abs(blk.Value.Y) <= offsetMax
                    && Math.Abs(Math.Abs(blk.Value.Y) - offsetMax) <= TolDistDalta)
                {
                    distanceList.Add((Math.Abs(blk.Value.Y)));
                    newPointDistList.Add(blk.Key);
                }

                //比maxoffset大且在线段内且为主块的
                if (side.reMainBlk.Contains(blk.Key) == true)
                {

                    newReMainPointList.Add(blk.Key);
                }

                if (blk.Value.Y * sideY > 0 && Math.Abs(Math.Abs(blk.Value.Y) - offsetMax) <= TolDistDalta)
                {
                    newReMainPointList.Add(blk.Key);
                }
            }

            newReMainPointList = newReMainPointList.Distinct().ToList();
            newReMainPointList = newReMainPointList.OrderBy(x => x.TransformBy(side.Matrix.Inverse()).X).ToList();

            if (distanceList.Count() > 0)
            {
                distance = distanceList
                               .OrderBy(x => x)
                               .GroupBy(x => Math.Floor(x / 10))
                               .OrderByDescending(x => x.Count())
                               .First()
                               .ToList()
                               .First();
            }

            if (distance <= 0)
            {
                distance = offsetMax;
            }

            Algorithms.PolyClean_RemoveDuplicatedVertex(lanePoly);
            var movelineTempObj = lanePoly.GetOffsetCurves(distance * dir);
            if (movelineTempObj.Count > 0)
            {
                var moveLineTemp = movelineTempObj[0] as Polyline;

                if (newReMainPointList.Count > 1)
                {
                    moveLine = drawEmgPipeService.cutPolyline(newReMainPointList.First(), newReMainPointList.Last(), moveLineTemp);
                }
                else
                {
                    moveLine = moveLineTemp;
                }
            }
            return (moveLine, newReMainPointList);
        }

        private static bool ptInSeg(Point3d pt, Point3d segS, Point3d segE)
        {
            int TolCloseDist = 250;
            bool inSeg = false;

            if ((segS.X - TolCloseDist <= pt.X && pt.X <= segE.X + TolCloseDist) ||
             (segE.X - TolCloseDist <= pt.X && pt.X <= segS.X + TolCloseDist))
            {
                inSeg = true;
            }
            return inSeg;
        }

        /// <summary>
        /// 每一段偏移线对应的车道线点位和点位在车道线坐标系下的值
        /// </summary>
        /// <param name="offsetInfo"></param>
        /// <param name="sideMatrix"></param>
        /// <returns>idx of item in offsetInfo,(lane seg start pt, lane seg start pt in lane matrix）, （lane seg end pt, lane seg end pt in lane matrix) </returns>
        private static List<(int, KeyValuePair<Point3d, Point3d>, KeyValuePair<Point3d, Point3d>)> getMoveLaneSegStartEndPt(List<(double, List<Line>)> offsetList, Matrix3d sideMatrix)
        {
            List<(int, KeyValuePair<Point3d, Point3d>, KeyValuePair<Point3d, Point3d>)> offsetOrderList = new List<(int, KeyValuePair<Point3d, Point3d>, KeyValuePair<Point3d, Point3d>)>();

            for (int i = 0; i < offsetList.Count; i++)
            {
                var offsetInfo = offsetList[i];
                var laneDict = new Dictionary<Point3d, Point3d>();

                foreach (var l in offsetInfo.Item2)
                {
                    if (laneDict.ContainsKey(l.StartPoint) == false)
                    {
                        var pt = l.StartPoint.TransformBy(sideMatrix.Inverse());
                        laneDict.Add(l.StartPoint, pt);
                    }
                    if (laneDict.ContainsKey(l.EndPoint) == false)
                    {
                        var pt = l.EndPoint.TransformBy(sideMatrix.Inverse());
                        laneDict.Add(l.EndPoint, pt);
                    }
                }

                laneDict = laneDict.OrderBy(x => x.Value.X).ToDictionary(x => x.Key, x => x.Value);

                var lanePartS = laneDict.First();
                var lanePartE = laneDict.Last();

                offsetOrderList.Add((i, lanePartS, lanePartE));
            }

            return offsetOrderList;
        }

        private static Polyline getCutLane(List<Line> lineList, Point3d spt, Point3d ept)
        {
            bool bEnd = false;
            Polyline lineP = new Polyline();

            var sPtPrj = drawEmgPipeService.getPrjPt(lineList, spt, out var sPtEx);
            var ePtPrj = drawEmgPipeService.getPrjPt(lineList, ept, out var ePtEx);

            lineP.AddVertexAt(lineP.NumberOfVertices, sPtPrj.ToPoint2d(), 0, 0, 0);

            if (ePtEx == -1 || sPtEx >= lineList.Count)
            {
                lineP.AddVertexAt(lineP.NumberOfVertices, ePtPrj.ToPoint2d(), 0, 0, 0);
                bEnd = true;
            }

            if (bEnd == false && sPtEx == -1)
            {
                sPtEx = 0;
                lineP.AddVertexAt(lineP.NumberOfVertices, lineList[0].StartPoint.ToPoint2d(), 0, 0, 0);
            }

            if (bEnd == false)
            {
                for (int i = sPtEx; i < ePtEx; i++)
                {
                    if (i < lineList.Count - 1)
                    {
                        lineP.AddVertexAt(lineP.NumberOfVertices, lineList[i].EndPoint.ToPoint2d(), 0, 0, 0);
                    }
                }
                if (ePtEx >= lineList.Count)
                {
                    lineP.AddVertexAt(lineP.NumberOfVertices, lineList[lineList.Count - 1].EndPoint.ToPoint2d(), 0, 0, 0);
                }
                lineP.AddVertexAt(lineP.NumberOfVertices, ePtPrj.ToPoint2d(), 0, 0, 0);
            }

            return lineP;

        }

    }
}
