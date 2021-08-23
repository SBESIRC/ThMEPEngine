using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;


namespace ThMEPLighting.EmgLightConnect.Service
{
    public class mergeOneMainBlkSideService
    {
        public static void mergeOneBlockSide(List<ThSingleSideBlocks> singleSideBlocks, Dictionary<Point3d, List<Line>> pointList, ThLaneSideGraph sideGraph, int groupMax)
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
                    targetSide = findConnectSide(side, singleSideBlocks, sideGraph, groupMax);
                }

                if (targetSide == null)
                {
                    //找跨边直线
                    //debug 相连边0 个block跳过
                    targetSide = findStrightSide(side, singleSideBlocks, pointList, groupMax);
                }

                //if (targetSide == null)
                //{
                //    //单点就留着，不如直接后面用回头量找，否则会找到奇怪的
                //    var targetPt = singleSideBlocks.SelectMany(blk => blk.getTotalBlock()).OrderBy(x => x.DistanceTo(side.mainBlk[0])).ToList();
                //    targetSide = singleSideBlocks.Where(x => x.getTotalBlock().Contains(targetPt[1])).FirstOrDefault();
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

        private static ThSingleSideBlocks findConnectSide(ThSingleSideBlocks side, List<ThSingleSideBlocks> singleSideBlocks, ThLaneSideGraph sideGraph, int groupMax)
        {
            ThSingleSideBlocks fromSide = null;
            ThSingleSideBlocks toSide = null;
            ThSingleSideBlocks target = null;

            var thisLane = side.laneSide.Select(x => x.Item1).ToList();

            var toSideTemp = singleSideBlocks.Where(x => sideGraph.sideVertexNodeList[side.laneSideNo].firstEdge != null && x.laneSideNo == sideGraph.sideVertexNodeList[side.laneSideNo].firstEdge.nodeIndex).FirstOrDefault();
            if (toSideTemp != null && toSideTemp.getTotalMainBlock().Count > 0)
            {
                var toLane = toSideTemp.laneSide.Select(x => x.Item1).ToList();
                var sameLane = toLane.Where(x => thisLane.Contains(x)); //不是同一条线的对面
                var minDist = toSideTemp.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();
                var sumBlk = toSideTemp.Count + side.Count;

                if (sameLane.Count() == 0 && sumBlk <= groupMax && minDist <= EmgConnectCommon.TolSaperateGroupMaxDistance && turnNotOver180(side, toSideTemp))
                {
                    toSide = toSideTemp;
                }
            }

            var fromNode = sideGraph.sideVertexNodeList.Where(x => x.firstEdge.nodeIndex == side.laneSideNo).FirstOrDefault();
            var fromSideTemp = singleSideBlocks.Where(x => fromNode != null && x.laneSideNo == sideGraph.sideVertexNodeList.IndexOf(fromNode)).FirstOrDefault();
            if (fromSideTemp != null && fromSideTemp.getTotalMainBlock().Count > 0)
            {
                var fromLane = fromSideTemp.laneSide.Select(x => x.Item1).ToList();
                var sameLane = fromLane.Where(x => thisLane.Contains(x));
                var minDist = fromSideTemp.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();
                var sumBlk = fromSideTemp.Count + side.Count;

                if (sameLane.Count() == 0 && sumBlk <= groupMax && minDist <= EmgConnectCommon.TolSaperateGroupMaxDistance && turnNotOver180(side, fromSideTemp))
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
        /// 找直线上的
        /// </summary>
        /// <param name="side"></param>
        /// <param name="singleSideBlocks"></param>
        /// <param name="pointList"></param>
        /// <returns></returns>
        private static ThSingleSideBlocks findStrightSide(ThSingleSideBlocks side, List<ThSingleSideBlocks> singleSideBlocks, Dictionary<Point3d, List<Line>> pointList, int groupMax)
        {
            var ptTol = new Tolerance(1, 1);
            ThSingleSideBlocks target = null;
            ThSingleSideBlocks sPtConnSide = null;
            ThSingleSideBlocks ePtConnSide = null;

            double minDistS = EmgConnectCommon.TolSaperateGroupMaxDistance;
            double minDistE = EmgConnectCommon.TolSaperateGroupMaxDistance;

            var thisLane = side.laneSide.Select(x => x.Item1).ToList();
            Point3d startP = thisLane.First().StartPoint;
            Point3d endP = thisLane.Last().EndPoint;
            (Line, int) laneSide = side.laneSide[0];

            //找到头尾点
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

            //找到头尾链接的线
            orderSingleSideLaneService.ptInPtList(pointList, startP, out var startConnect);
            orderSingleSideLaneService.ptInPtList(pointList, endP, out var endConnect);

            //找到头尾链接线里面平行于本线的
            var sPtConnTemp = findParallelLane(laneSide, startConnect, singleSideBlocks);
            var ePtConnTemp = findParallelLane(laneSide, endConnect, singleSideBlocks);

            if (sPtConnTemp != null && sPtConnTemp.getTotalMainBlock().Count > 0)
            {
                minDistS = sPtConnTemp.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();
                var sumBlk = sPtConnTemp.Count + side.Count;

                if (sumBlk <= groupMax && minDistS <= EmgConnectCommon.TolSaperateGroupMaxDistance)
                {
                    sPtConnSide = sPtConnTemp;
                }

            }
            if (ePtConnTemp != null && ePtConnTemp.getTotalMainBlock().Count > 0)
            {
                minDistE = ePtConnTemp.getTotalMainBlock().Select(x => x.DistanceTo(side.mainBlk[0])).Min();
                var sumBlk = ePtConnTemp.Count + side.Count;

                if (sumBlk <= groupMax && minDistE <= EmgConnectCommon.TolSaperateGroupMaxDistance)
                {
                    ePtConnSide = ePtConnTemp;
                }
            }


            if (sPtConnSide != null && ePtConnSide != null)
            {
                if (minDistS <= minDistE)
                {
                    target = sPtConnSide;
                }
                else if (minDistE < minDistS)
                {
                    target = ePtConnSide;
                }
            }
            else if (sPtConnSide != null)
            {
                target = sPtConnSide;
            }
            else if (ePtConnSide != null)
            {
                target = ePtConnSide;
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

        private static void mergeToSide(ThSingleSideBlocks side, ThSingleSideBlocks targetSide, List<ThSingleSideBlocks> singleSideBlocks)
        {
            targetSide.addMainBlock.AddRange(side.mainBlk);
            targetSide.addMainBlock.AddRange(side.addMainBlock);
            targetSide.secBlk.AddRange(side.secBlk);
            side.groupBlock.ToList().ForEach(x => targetSide.groupBlock.Add(x.Key, x.Value));

            side.mainBlk.Clear();
            side.addMainBlock.Clear();
            side.secBlk.Clear();
            side.groupBlock.Clear();

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

    }
}
