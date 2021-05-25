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
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Assistant;


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
                    regroupMainSec(side, out var allBlkDict);
                    side.orderReMainBlk();

                    breakMainGroup(side, frame, holes);
                    side.orderReMainBlk();

                }
            }
        }

        private static void regroupMainSec(ThSingleSideBlocks side, out List<KeyValuePair<Point3d, Point3d>> allBlkDict)
        {
            int TolCloseDist = 100;
            List<Point3d> regroupMain = new List<Point3d>();
            List<double> yTransValue = new List<double>();
            var allBlk = side.getTotalBlock();

            allBlkDict = allBlk.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(item => item.Value.X).ToList();
            var laneEndPt = side.laneSide.Last().Item1.EndPoint.TransformBy(side.Matrix.Inverse());

            if (side.mainBlk.Count > 0)
            {
                yTransValue = allBlkDict.Where(x => side.mainBlk.Contains(x.Key)).Select(x => Math.Abs(x.Value.Y)).ToList();
                var yDebugDict = allBlkDict.Where(x => side.mainBlk.Contains(x.Key)).ToDictionary(x => x.Key, x => Math.Abs(x.Value.Y));

                double Ymin = 0;
                double YMax = 0;

                Ymin = yTransValue.Min() - EmgConnectCommon.TolRegroupMainYRange;
                YMax = yTransValue.Max() + EmgConnectCommon.TolRegroupMainYRange;

                if (yTransValue.Max() - yTransValue.Min() > 2000)
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

                regroupMain = allBlkDict.Where(x => Ymin <= Math.Abs(x.Value.Y) && Math.Abs(x.Value.Y) <= YMax && x.Value.X > -EmgConnectCommon.TolRegroupMainYRange && x.Value.X <= laneEndPt.X + EmgConnectCommon.TolRegroupMainYRange).Select(x => x.Key).ToList();

                //regroupMain.AddRange(side.mainBlk);
                regroupMain = regroupMain.Distinct().ToList();
            }
            else if (side.secBlk.Count > 0)
            {
                yTransValue = allBlkDict.Where(x => side.secBlk.Contains(x.Key)).Select(x => x.Value.Y).ToList();

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
                    movedLaneList.Add((moveLanePoly, side.reMainBlk));
                }

                for (int i = 0; i < offsetList.Count; i++)
                {

                    var newOffsetPt = getMoveLaneList(side, offsetList[i], lanePoly, -offSetDir);

                    if (newOffsetPt.Item1.NumberOfVertices > 0)
                    {
                        movedLaneList.Add(newOffsetPt);

                    }
                    newOffsetPt.Item2.Where(x => side.reMainBlk.Contains(x) == false).ForEach(y => side.reMainBlk.Add(y));
                }

                addRemainMainBlk(side.reMainBlk, ref movedLaneList);


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
                var minDist = movedLaneList[0].Item1.GetDistAtPoint(pt);
                var minIdx = 0;

                for (int j = 0; j < movedLaneList.Count; j++)
                {
                    var dist = movedLaneList[j].Item1.GetDistAtPoint(pt);
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
        /// <returns></returns>
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

        //private static List<(double, List<Line>)> checkMoveLineIntersectOutFrame(List<Line> tempMoveLine, Polyline frame, List<Polyline> holes, double offset, ThSingleSideBlocks side, out Polyline lanePoly, out Polyline moveLanePoly)
        //{
        //    List<(double, List<Line>)> offsetList = new List<(double, List<Line>)>();
        //    var lane = side.laneSide.Select(x => x.Item1).ToList();
        //    lanePoly = getCutLane(lane, side.reMainBlk.First(), side.reMainBlk.Last());
        //    var moveLineTemp = getCutLane(tempMoveLine, side.reMainBlk.First(), side.reMainBlk.Last());
        //    moveLanePoly = moveLineTemp.Clone() as Polyline;

        //    var checkIntersectPoly = lanePoly.Clone() as Polyline;

        //    for (int i = moveLineTemp.NumberOfVertices - 1; i >= 0; i--)
        //    {
        //        checkIntersectPoly.AddVertexAt(checkIntersectPoly.NumberOfVertices, moveLineTemp.GetPoint2dAt(i), 0, 0, 0);
        //    }
        //    checkIntersectPoly.Closed = true;

        //    var pts = moveLineTemp.Intersect(frame, Intersect.OnBothOperands);
        //    if (pts.Count > 0)
        //    {
        //        var polyCollection = new DBObjectCollection() { frame };
        //        var overlap = checkIntersectPoly.Intersection(polyCollection);

        //        if (overlap.Count > 0)
        //        {
        //            var overlapPoly = overlap.Cast<Polyline>().OrderByDescending(x => x.Area).First();
        //            offsetList = getNewOffsetSeg(overlapPoly, lanePoly, offset);
        //        }
        //    }

        //    return offsetList;

        //}

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

              //  DrawUtils.ShowGeometry(lanePoly, "l0lanePoly");

                var checkIntersectPoly = lanePoly.Clone() as Polyline;

                for (int i = moveLineTemp.NumberOfVertices - 1; i >= 0; i--)
                {
                    checkIntersectPoly.AddVertexAt(checkIntersectPoly.NumberOfVertices, moveLineTemp.GetPoint2dAt(i), 0, 0, 0);
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
                  //  DrawUtils.ShowGeometry(checkIntersectPoly, "l0asdf");
                }


            }
            return offsetList;

        }

        private static List<(double, List<Line>)> getNewOffsetSeg(Polyline overlapPoly, Polyline lanePoly, double offsetOri)
        {
            int TolDistDalta = 1000;
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

                if (TolCloseDist > dist)
                {
                    passLane = true;
                }

                if (TolCloseDist < dist && distDalta > TolDistDalta)
                {
                    offsetList.Add((dist, new List<Line> { lineSeg }));
                    minDist = dist;
                }
                else if (TolCloseDist < dist && passLane == true)
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
                               .OrderByDescending(x => x.Count())
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="side"></param>
        /// <param name="offsetList"></param>
        /// <param name="lanePoly"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static (Polyline, List<Point3d>) getMoveLaneList(ThSingleSideBlocks side, (double, List<Line>) offsetList, Polyline lanePoly, int dir)
        {
            int TolDistDalta = 1000;
            int TolCloseDist = 100;

            var offsetMax = offsetList.Item1;

            var distanceList = new List<double>();
            double distance = -1;

            var allBlk = side.getTotalBlock();
            var allBlkDict = allBlk.ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse())).OrderBy(item => item.Value.X).ToList();

            var laneDict = new Dictionary<Point3d, Point3d>();

            foreach (var l in offsetList.Item2)
            {
                if (laneDict.ContainsKey(l.StartPoint) == false)
                {
                    var pt = l.StartPoint.TransformBy(side.Matrix.Inverse());
                    laneDict.Add(l.StartPoint, pt);
                }
                if (laneDict.ContainsKey(l.EndPoint) == false)
                {
                    var pt = l.EndPoint.TransformBy(side.Matrix.Inverse());
                    laneDict.Add(l.EndPoint, pt);
                }

            }

            //var laneDict = offsetList.Item2.Select(x => x.StartPoint).ToDictionary(x => x, x => x.TransformBy(side.Matrix.Inverse()));
            //laneDict.Add(offsetList.Item2.Last().EndPoint, offsetList.Item2.Last().EndPoint.TransformBy(side.Matrix.Inverse()));


            laneDict = laneDict.OrderBy(x => x.Value.X).ToDictionary(x => x.Key, x => x.Value);

            var lanePartS = laneDict.First();
            var lanePartE = laneDict.Last();

            var newPointDistList = new List<Point3d>();
            var newReMainPointList = new List<Point3d>();

            Polyline moveLine = new Polyline();

            foreach (var blk in allBlkDict)
            {
                //y比maxoffset小且移动不大的，在线段内的点都算 为了计算真实的offset
                if (Math.Abs(blk.Value.Y) <= offsetMax && Math.Abs(Math.Abs(blk.Value.Y) - offsetMax) <= TolDistDalta
                    && ((lanePartS.Value.X - TolCloseDist <= blk.Value.X && blk.Value.X <= lanePartE.Value.X + TolCloseDist) ||
                        (lanePartE.Value.X - TolCloseDist <= blk.Value.X && blk.Value.X <= lanePartS.Value.X + TolCloseDist)))
                {
                    distanceList.Add((Math.Abs(blk.Value.Y)));
                    newPointDistList.Add(blk.Key);
                }

                //比maxoffset大且在线段内且为主块的
                if (side.reMainBlk.Contains(blk.Key) == true
                    && ((lanePartS.Value.X - TolCloseDist <= blk.Value.X && blk.Value.X <= lanePartE.Value.X + TolCloseDist) ||
                        (lanePartE.Value.X - TolCloseDist <= blk.Value.X && blk.Value.X <= lanePartS.Value.X + TolCloseDist)))
                {

                    newReMainPointList.Add(blk.Key);
                }

                if (Math.Abs(Math.Abs(blk.Value.Y) - offsetMax) <= TolDistDalta
                   && ((lanePartS.Value.X - TolCloseDist <= blk.Value.X && blk.Value.X <= lanePartE.Value.X + TolCloseDist) ||
                        (lanePartE.Value.X - TolCloseDist <= blk.Value.X && blk.Value.X <= lanePartS.Value.X + TolCloseDist)))
                {

                    newReMainPointList.Add(blk.Key);
                }
            }

            newReMainPointList.AddRange(newPointDistList);
            newReMainPointList = newReMainPointList.Distinct().ToList();

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
                distance = offsetMax - EmgConnectCommon.TolLinkOffsetWithFrame;
            }

            var moveLineTemp = lanePoly.GetOffsetCurves(distance * dir)[0] as Polyline;

            moveLine = drawEmgPipeService.cutPolyline(lanePartS.Key, lanePartE.Key, moveLineTemp);

            return (moveLine, newReMainPointList);
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
