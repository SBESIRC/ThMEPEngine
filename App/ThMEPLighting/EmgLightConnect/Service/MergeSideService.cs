using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Service;

namespace ThMEPLighting.EmgLightConnect.Service
{
    class MergeSideService
    {
        public static void mergeSide(Dictionary<int, List<List<(Line, int)>>> orderedAllLaneSideList, out Dictionary<int, List<(Line, int)>> sideDict)
        {
            sideDict = new Dictionary<int, List<(Line, int)>>();
            int mergeIndex = 0;
            int check = 0;


            foreach (var pair in orderedAllLaneSideList)
            {
                var orderedLaneSideList = pair.Value;

                for (int i = 0; i < orderedLaneSideList.Count; i++)
                {

                    sideDict.Add(sideDict.Count, new List<(Line, int)>() { orderedLaneSideList[i][0] });

                    for (int j = 1; j < orderedLaneSideList[i].Count; j++)
                    {
                        for (mergeIndex = j; mergeIndex < orderedLaneSideList[i].Count; mergeIndex++)
                        {
                            check = mergeIndex - 1;
                            if (mergeSameLaneSide(orderedLaneSideList[i][mergeIndex], orderedLaneSideList[i][check]))
                            {
                                var keyIndex = sideDict.Where(x => x.Value.Contains(orderedLaneSideList[i][check])).FirstOrDefault().Key;
                                sideDict[keyIndex].Add(orderedLaneSideList[i][mergeIndex]);
                            }
                            else
                            {
                                break;
                            }
                        }

                        j = mergeIndex;
                        if (j < orderedLaneSideList[i].Count)
                        {
                            sideDict.Add(sideDict.Count, new List<(Line, int)>() { orderedLaneSideList[i][j] });
                        }
                    }

                    //check if 0 and -1 are same lane
                    check = orderedLaneSideList[i].Count - 1;
                    if (mergeSameLaneSide(orderedLaneSideList[i][0], orderedLaneSideList[i][check]))
                    {
                        var lastIndex = sideDict.Where(x => x.Value.Contains(orderedLaneSideList[i][check])).First();
                        var firstIndex = sideDict.Where(x => x.Value.Contains(orderedLaneSideList[i][0])).First();
                        sideDict[firstIndex.Key].AddRange(sideDict[lastIndex.Key]);
                        sideDict.Remove(lastIndex.Key);
                    }
                }
            }
        }

        private static bool mergeSameLaneSide((Line, int) lane, (Line, int) laneNext)
        {
            bool bAngle = false;

            if (lane.Item1 != laneNext.Item1)
            {
                var laneDir = (lane.Item1.EndPoint - lane.Item1.StartPoint).GetNormal();
                var laneNextDir = (laneNext.Item1.EndPoint - laneNext.Item1.StartPoint).GetNormal();

                bAngle = Math.Abs(laneDir.DotProduct(laneNextDir)) / (laneDir.Length * laneNextDir.Length) > Math.Abs(Math.Cos(45 * Math.PI / 180));

            }

            return bAngle;
        }

        public static void mergeSigleSideBlocks(Dictionary<int, List<(Line, int)>> sideDict, List<ThSingleSideBlocks> singleSideBlocks)
        {
            foreach (var sidePair in sideDict)
            {
                var side = sidePair.Value;
                var singleSideBlocksInSameLane = singleSideBlocks.Where(y => side.Contains(y.laneSide[0])).ToList();

                singleSideBlocksInSameLane[0].laneSideNo = sidePair.Key;

                for (int i = 1; i < singleSideBlocksInSameLane.Count; i++)
                {
                    singleSideBlocksInSameLane[0].mainBlk.AddRange(singleSideBlocksInSameLane[i].mainBlk);
                    singleSideBlocksInSameLane[0].secBlk.AddRange(singleSideBlocksInSameLane[i].secBlk);
                    singleSideBlocksInSameLane[i].groupBlock.ToList().ForEach(x => singleSideBlocksInSameLane[0].groupBlock.Add(x.Key, x.Value));

                    singleSideBlocksInSameLane[0].laneSide.AddRange(singleSideBlocksInSameLane[i].laneSide);
                    singleSideBlocks.Remove(singleSideBlocksInSameLane[i]);

                }
            }

        }

        /// <summary>
        /// 需要先对side的车道线排序
        /// </summary>
        /// <param name="singleSideBlocks"></param>
        /// <param name="pointList"></param>
        public static void mergeOneBlockSide(List<ThSingleSideBlocks> singleSideBlocks, Dictionary<Point3d, List<Line>> pointList, ThLaneSideGraph sideGraph)
        {

            var oneBlockSide = singleSideBlocks.Where(x => x.mainBlk.Count == 1).ToList();
            Dictionary<int, int> mergeDict = new Dictionary<int, int>();

            for (int i = 0; i < oneBlockSide.Count; i++)
            {
                ThSingleSideBlocks targetSide = null;
                var side = oneBlockSide[i];

                if (targetSide == null)
                {
                    //找同边
                    targetSide = findConnectSide(side, singleSideBlocks, sideGraph);

                }

                if (targetSide == null)
                {
                    //找跨边直线
                    //debug 相连边0 个block跳过
                    targetSide = findStrightSide(side, singleSideBlocks, pointList);

                }

                //if (targetSide == null)
                //{
                //    //单点就留着，不如直接后面用回头量找，否则会找到奇怪的
                //    var targetPt = singleSideBlocks.SelectMany(blk => blk.totalMainBlock).OrderBy(x => x.DistanceTo(side.mainBlock[0])).ToList ();
                //    targetSide = singleSideBlocks.Where(x => x.totalMainBlock.Contains(targetPt[1])).FirstOrDefault();
                //}

                if (targetSide != null)
                {
                   
                    //createAddMainBlkLink(side, targetSide);

                    if (mergeDict.ContainsKey(targetSide.laneSideNo))
                    {
                        var newMergeNo = mergeDict[targetSide.laneSideNo];
                        targetSide = singleSideBlocks.Where(x => x.laneSideNo == newMergeNo).First();
                    }
                    if (mergeDict.ContainsValue(side.laneSideNo))
                    {
                        var changeKey = mergeDict.Where(x => x.Value == side.laneSideNo).Select(y => y.Key).ToList();
                        changeKey.ForEach(x => mergeDict[x] = targetSide.laneSideNo);
                    }

                    if (side.laneSideNo != targetSide.laneSideNo)
                    {
                        mergeDict.Add(side.laneSideNo, targetSide.laneSideNo);
                    }
                }
            }

            foreach (var merge in mergeDict)
            {
                var side = singleSideBlocks.Where(x => x.laneSideNo == merge.Key).First();
                var targetSide = singleSideBlocks.Where(x => x.laneSideNo == merge.Value).First();

                mergeToSide(side, targetSide, singleSideBlocks);
            }

        }

        //private static void createAddMainBlkLink(ThSingleSideBlocks side, ThSingleSideBlocks targetSide)
        //{
        //    var targetPt = targetSide.mainBlock.OrderBy(dist => dist.DistanceTo(side.mainBlock[0])).First();
        //    var line = new Line(side.mainBlock[0], targetPt);
        //    side.addMainBlkLine.Add(line);
        //}

        private static void mergeToSide(ThSingleSideBlocks side, ThSingleSideBlocks targetSide, List<ThSingleSideBlocks> singleSideBlocks)
        {
            targetSide.addMainBlock.AddRange(side.mainBlk);
            targetSide.addMainBlock.AddRange(side.addMainBlock);
            targetSide.secBlk.AddRange(side.secBlk);
            //targetSide.addMainBlkLine.AddRange(side.addMainBlkLine);
            side.groupBlock.ToList().ForEach(x => targetSide.groupBlock.Add(x.Key, x.Value));

            side.mainBlk.Clear();
            side.addMainBlock.Clear();
            side.secBlk.Clear();
            side.groupBlock.Clear();
            //side.addMainBlkLine.Clear();

        }

        private static ThSingleSideBlocks findConnectSide(ThSingleSideBlocks side, List<ThSingleSideBlocks> singleSideBlocks, ThLaneSideGraph sideGraph)
        {
            ThSingleSideBlocks fromSide = null;
            ThSingleSideBlocks toSide = null;
            ThSingleSideBlocks target = null;

            var thisLane = side.laneSide.Select(x => x.Item1).ToList();

            var toSideTemp = singleSideBlocks.Where(x => sideGraph.sideVertexNodeList[side.laneSideNo].firstEdge != null && x.laneSideNo == sideGraph.sideVertexNodeList[side.laneSideNo].firstEdge.nodeIndex).FirstOrDefault();
            if (toSideTemp != null)
            {
                var toLane = toSideTemp.laneSide.Select(x => x.Item1).ToList();
                var sameLane = toLane.Where(x => thisLane.Contains(x)); //不是同一条线的对面
                if (sameLane.Count() == 0 && toSideTemp.getTotalMainBlock().Count > 0 && turnNotOver180(side, toSideTemp))
                {
                    toSide = toSideTemp;
                }
            }

            var fromNode = sideGraph.sideVertexNodeList.Where(x => x.firstEdge.nodeIndex == side.laneSideNo).FirstOrDefault();
            var fromSideTemp = singleSideBlocks.Where(x => fromNode != null && x.laneSideNo == sideGraph.sideVertexNodeList.IndexOf(fromNode)).FirstOrDefault();
            if (fromSideTemp != null)
            {
                var fromLane = fromSideTemp.laneSide.Select(x => x.Item1).ToList();
                var sameLane = fromLane.Where(x => thisLane.Contains(x));
                if (sameLane.Count() == 0 && fromSideTemp.getTotalMainBlock().Count > 0 && turnNotOver180(side, fromSideTemp))
                {
                    fromSide = fromSideTemp;
                }
            }

            //找到相临边，且相临边有block
            if (toSide != null && fromSide != null)
            {

                if (tooCloseSide(side, toSide, fromSide, ref target) == false)
                {
                    decideToFrom(side, toSide, fromSide, ref target);
                }
            }

            else if (toSide != null)
            {
                target = toSide;
            }
            else if (fromSide != null)
            {
                target = fromSide;
            }

            return target;

        }

        private static void decideToFrom(ThSingleSideBlocks side, ThSingleSideBlocks toSide, ThSingleSideBlocks fromSide, ref ThSingleSideBlocks target)
        {

            var angleTo = parallelToLane(side, toSide);
            var angleFrom = parallelToLane(side, fromSide);

            if (angleTo.Item1 == true)
            {
                target = toSide;
            }
            else if (angleFrom.Item1 == true)
            {
                target = fromSide;
            }
            else if (angleTo.Item2 == true)
            {
                target = toSide;
            }
            else if (angleFrom.Item2 == true)
            {
                target = fromSide;
            }
            else
            {
                //if (toSide.mainBlock.Count > 0 && fromSide.mainBlock.Count > 0)
                //{
                //    var minDistTo = toSide.mainBlock.Select(x => x.DistanceTo(side.mainBlock[0])).Min();
                //    var minDistFrom = fromSide.mainBlock.Select(x => x.DistanceTo(side.mainBlock[0])).Min();
                //    if (minDistTo <= minDistFrom)
                //    {
                //        target = toSide;
                //    }
                //    else
                //    {
                //        target = fromSide;
                //    }
                //}
                //if (target == null)
                //{
                target = toSide;

                //}
            }
        }

        private static bool tooCloseSide(ThSingleSideBlocks side, ThSingleSideBlocks toSide, ThSingleSideBlocks fromSide, ref ThSingleSideBlocks target)
        {
            bool hasCloseSide = false;

            var minDistTo = toSide.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();
            var minDistFrom = fromSide.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();

            if (minDistTo <= EmgConnectCommon.TolTooClosePt)
            {
                target = toSide;
                hasCloseSide = true;
            }
            else if (minDistFrom <= EmgConnectCommon.TolTooClosePt)
            {
                target = fromSide;
                hasCloseSide = true;
            }


            return hasCloseSide;
        }

        /// <summary>
        /// 返回：（是否平行于隔壁车道线，是否垂直于隔壁车道线）
        /// </summary>
        /// <param name="side"></param>
        /// <param name="otherSide"></param>
        /// <returns></returns>
        private static (bool, bool) parallelToLane(ThSingleSideBlocks side, ThSingleSideBlocks otherSide)
        {

            var nearPt = otherSide.getTotalMainBlock().OrderBy(x => x.DistanceTo(side.mainBlk[0])).First();
            var blockVec = (nearPt - side.mainBlk[0]).GetNormal();
            var laneVec = (otherSide.laneSide[0].Item1.EndPoint - otherSide.laneSide[0].Item1.StartPoint).GetNormal();

            double angleCos = Math.Abs(blockVec.DotProduct(laneVec)) / (blockVec.Length * laneVec.Length);

            var bParallelAngle = angleCos > Math.Abs(Math.Cos(20 * Math.PI / 180));
            var bVerticalAngle = angleCos <= Math.Abs(Math.Cos(120 * Math.PI / 180));

            return (bParallelAngle, bVerticalAngle);
        }

        /// <summary>
        /// 1.找side 和 otherSide 的共同点，如果不是各线段的起点，则返回以这个点为起点的vector: vecSide, vecOther
        /// 2.共同点是side终点 : side.laneside(共同点所在的边） ==1 => 逆时针(vecSide,vecOther)<180; laneside == 0 顺时针(vecSide,vecOther)<180
        /// 3.共同点是side起点 : side.laneside(共同点所在的边） ==1 => 顺时针(vecSide,vecOther)<180; laneside == 0 逆时针(vecSide,vecOther)<180
        /// </summary>
        /// <param name="side"></param>
        /// <param name="otherSide"></param>
        /// <returns></returns>
        private static bool turnNotOver180(ThSingleSideBlocks side, ThSingleSideBlocks otherSide)
        {

            bool bReturn = false;

            int zAxis = 1;

            var isStart = findCommonPt(side, otherSide, out Vector3d vecSide, out Vector3d vecOther);

            if ((isStart.Item1 == true && isStart.Item2 == 1) || (isStart.Item1 == false && isStart.Item2 == 0))
            {
                //顺时针
                zAxis = -1;
            }

            var angle = vecSide.GetAngleTo(vecOther, zAxis * Vector3d.ZAxis);

            if (angle <= Math.PI) //大于180度
            {
                bReturn = true;
            }

            return bReturn;
        }

        /// <summary>
        /// 1.找到共同点
        /// 2.根据共同点返回参与角度计算的vector
        /// 3.return.item1 : 如果共同点是side 起点则为true， 如果为终点则为false
        /// return.item2:共同点side的线段的边 （有可能有多段）
        /// </summary>
        /// <param name="side"></param>
        /// <param name="otherSide"></param>
        /// <param name="vecSide"></param>
        /// <param name="vecOther"></param>
        /// <returns></returns>
        private static (bool, int) findCommonPt(ThSingleSideBlocks side, ThSingleSideBlocks otherSide, out Vector3d vecSide, out Vector3d vecOther)
        {
            bool isStart = false;
            int sideInt = -1;
            var tol = new Tolerance(1, 1);

            vecSide = side.laneSide[0].Item1.EndPoint - side.laneSide[0].Item1.StartPoint;
            vecOther = otherSide.laneSide[0].Item1.EndPoint - otherSide.laneSide[0].Item1.StartPoint;

            for (int i = 0; i < side.laneSide.Count; i++)
            {
                if (sideInt != -1)
                {
                    break;
                }

                var thisLane = side.laneSide[i].Item1;

                for (int j = 0; j < otherSide.laneSide.Count; j++)
                {
                    var otherLane = otherSide.laneSide[j].Item1;


                    if (thisLane.StartPoint.IsEqualTo(otherLane.StartPoint, tol))
                    {
                        vecSide = thisLane.EndPoint - thisLane.StartPoint;
                        vecOther = otherLane.EndPoint - otherLane.StartPoint;
                        isStart = true;
                        sideInt = side.laneSide[i].Item2;
                        break;
                    }
                    else if (thisLane.StartPoint.IsEqualTo(otherLane.EndPoint, tol))
                    {
                        vecSide = thisLane.EndPoint - thisLane.StartPoint;
                        vecOther = otherLane.StartPoint - otherLane.EndPoint;

                        isStart = true;
                        sideInt = side.laneSide[i].Item2;
                        break;
                    }
                    else if (thisLane.EndPoint.IsEqualTo(otherLane.StartPoint, tol))
                    {
                        vecSide = thisLane.StartPoint - thisLane.EndPoint;
                        vecOther = otherLane.EndPoint - otherLane.StartPoint;
                        isStart = false;
                        sideInt = side.laneSide[i].Item2;
                        break;
                    }
                    else if (thisLane.EndPoint.IsEqualTo(otherLane.EndPoint, tol))
                    {
                        vecSide = thisLane.StartPoint - thisLane.EndPoint;
                        vecOther = otherLane.StartPoint - otherLane.EndPoint;
                        isStart = false;
                        sideInt = side.laneSide[i].Item2;
                        break;
                    }
                }
            }
            return (isStart, sideInt);
        }

        private static ThSingleSideBlocks findStrightSide(ThSingleSideBlocks side, List<ThSingleSideBlocks> singleSideBlocks, Dictionary<Point3d, List<Line>> pointList)
        {
            var ptTol = new Tolerance(1, 1);
            ThSingleSideBlocks target = null;

            var thisLane = side.laneSide.Select(x => x.Item1).ToList();
            Point3d startP = thisLane.First().StartPoint;
            Point3d endP = thisLane.Last().EndPoint;
            (Line, int) laneSide = side.laneSide[0];

            if (thisLane.Count > 1)
            {
                if (thisLane[0].StartPoint.IsEqualTo(thisLane[1].StartPoint, ptTol) || thisLane[0].StartPoint.IsEqualTo(thisLane[1].EndPoint))
                {
                    startP = thisLane[0].EndPoint;
                }
                if (thisLane.Last().EndPoint.IsEqualTo(thisLane[thisLane.Count - 2].StartPoint, ptTol) || thisLane.Last().EndPoint.IsEqualTo(thisLane[thisLane.Count - 2].EndPoint, ptTol))
                {
                    endP = thisLane.Last().StartPoint;
                }
            }

            orderSingleSideLaneService.ptInPtList(pointList, startP, out var startConnect);
            orderSingleSideLaneService.ptInPtList(pointList, endP, out var endConnect);

            var targetS = findParallelLane(laneSide, startConnect, singleSideBlocks);
            var targetE = findParallelLane(laneSide, endConnect, singleSideBlocks);

            if (targetS != null && targetS.getTotalMainBlock().Count == 0)
            {
                targetS = null;
            }
            if (targetE != null && targetE.getTotalMainBlock().Count == 0)
            {
                targetE = null;
            }

            if (targetS != null && targetE != null)
            {
                findClosedSide(side, targetS, targetE, ref target);
            }
            else if (targetS != null)
            {
                target = targetS;
            }
            else if (targetE != null)
            {
                target = targetE;
            }


            return target;

        }

        private static ThSingleSideBlocks findParallelLane((Line, int) laneSide, KeyValuePair<Point3d, List<Line>> laneConnectPair, List<ThSingleSideBlocks> singleSideBlocks)
        {
            Line connectTemp = null;
            ThSingleSideBlocks targetTemp = null;

            var laneConnect = laneConnectPair.Value;
            double cosA = 0;

            for (int i = 0; i < laneConnect.Count; i++)
            {
                if (laneSide.Item1 != laneConnect[i])
                {
                    var laneDir = (laneSide.Item1.EndPoint - laneSide.Item1.StartPoint).GetNormal();
                    var laneNextDir = (laneConnect[i].EndPoint - laneConnect[i].StartPoint).GetNormal();

                    cosA = laneDir.DotProduct(laneNextDir) / (laneDir.Length * laneNextDir.Length);

                    if (Math.Abs(cosA) > Math.Abs(Math.Cos(20 * Math.PI / 180)))
                    {
                        connectTemp = laneConnect[i];
                        break;
                    }
                }
            }

            if (connectTemp != null)
            {
                for (int i = 0; i < singleSideBlocks.Count; i++)
                {
                    if (targetTemp != null)
                    {
                        break;
                    }
                    for (int j = 0; j < singleSideBlocks[i].laneSide.Count; j++)
                    {
                        if (singleSideBlocks[i].laneSide[j].Item1 == connectTemp && cosA > 0 && singleSideBlocks[i].laneSide[j].Item2 == laneSide.Item2)
                        {
                            targetTemp = singleSideBlocks[i];
                            break;
                        }
                        else if (singleSideBlocks[i].laneSide[j].Item1 == connectTemp && cosA < 0 && singleSideBlocks[i].laneSide[j].Item2 != laneSide.Item2)
                        {
                            targetTemp = singleSideBlocks[i];
                            break;
                        }
                    }
                }
            }

            return targetTemp;

        }

        private static void findClosedSide(ThSingleSideBlocks side, ThSingleSideBlocks targetS, ThSingleSideBlocks tartgetE, ref ThSingleSideBlocks target)
        {
            double minDistS = EmgConnectCommon.TolGroupDistance;
            double minDistE = EmgConnectCommon.TolGroupDistance;

            minDistS = targetS.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();
            minDistE = tartgetE.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();

            if (minDistS <= minDistE && minDistS < EmgConnectCommon.TolGroupDistance)
            {
                target = targetS;
            }
            else if (minDistE < minDistS && minDistE < EmgConnectCommon.TolGroupDistance)
            {
                target = tartgetE;
            }

            if (target == null)
            {
                target = targetS;
            }
        }
    }
}
