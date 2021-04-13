using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.EmgLightConnect.Service;
using ThMEPLighting.EmgLight.Assistant;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Service;


namespace ThMEPLighting.EmgLightConnect.Service
{
    public class connectSingleSideInGroupService
    {
        public static List<(Point3d, Point3d)> connectAllSingleSide(BlockReference ALE, List<List<ThSingleSideBlocks>> OptimalGroupBlocks)
        {
            var connectList = new List<(Point3d, Point3d)>();

            for (int i = 0; i < OptimalGroupBlocks.Count; i++)
            {

                var sigleSideGroup = OptimalGroupBlocks[i];

                var sigleSideConnectList = connectSingleSide(ALE, sigleSideGroup);

                connectList.AddRange(sigleSideConnectList);
            }

            return connectList;
        }

        //private static List<(Point3d, Point3d)> connectSingleSide(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        //{
        //    var orderSigleSideGroup = orderSignleSide(ALE, sigleSideGroup);


        //    var blockList = new List<Point3d>();
        //    blockList.AddRange(getAllMainAndReMain (orderSigleSideGroup[0]));

        //    var connectList = new List<(Point3d, Point3d)>();

        //    for (int i = 1; i < orderSigleSideGroup.Count; i++)
        //    {
        //        //debug，有单点加入以后排序可能有问题
        //        var thisLaneBlock = getAllMainAndReMain(orderSigleSideGroup[i]);

        //        Dictionary<int, double> returnValueDict = new Dictionary<int, double>(); //key:blockListIndex value:returnValue

        //        checkReturn(ALE, blockList, thisLaneBlock, returnValueDict);
        //        var closedDists = findClosePoint(thisLaneBlock, blockList);

        //        var minHasReturnClosedPair = hasReturnClosePair(returnValueDict, closedDists);
        //        var distClosedPair = closedDists.OrderBy(y => y.Item3).First();

        //        if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturn && returnValueDict[distClosedPair.Item1] < 10000) ||
        //            minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolGroupDistance)
        //        {
        //            connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
        //        }
        //        else
        //        {
        //            connectList.Add((blockList[minHasReturnClosedPair.Item1], thisLaneBlock[minHasReturnClosedPair.Item2]));
        //        }


        //        blockList.AddRange(thisLaneBlock);

        //    }

        //    return connectList;


        //}

        private static List<(Point3d, Point3d)> connectSingleSide(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var orderSigleSideGroup = orderSignleSide(ALE, sigleSideGroup);


            var blockList = new List<Point3d>();
            //blockList.AddRange(getAllMainAndReMain(orderSigleSideGroup[0]));
            blockList.AddRange(orderSigleSideGroup[0].getTotalBlock());

            var connectList = new List<(Point3d, Point3d)>();

            for (int i = 1; i < orderSigleSideGroup.Count; i++)
            {

                //var thisLaneBlock = getAllMainAndReMain(orderSigleSideGroup[i]);
                var thisLaneBlock = orderSigleSideGroup[i].getTotalBlock();

                Dictionary<int, double> returnValueDict = new Dictionary<int, double>(); //key:blockListIndex value:returnValue

                checkReturn(ALE, blockList, thisLaneBlock, returnValueDict);
                var closedDists = findClosePoint(thisLaneBlock, blockList);

                //findOptimalConnection(returnValueDict, closedDists, blockList, thisLaneBlock, connectList);
                var connectListTemp = findOptimalConnection(returnValueDict, closedDists, blockList, thisLaneBlock, orderSigleSideGroup[i]);

                if (connectListTemp.Count > 0)
                {
                    connectList.AddRange(connectListTemp);
                    orderSigleSideGroup[i].connectPt(connectList[0].Item1, connectList[0].Item2);
                }

                blockList.AddRange(thisLaneBlock);

            }

            return connectList;


        }

        //private static List<(Point3d, Point3d)> findOptimalConnection(Dictionary<int, double> returnValueDict, List<(int, int, double)> closedDists, List<Point3d> blockList,
        //                                      List<Point3d> thisLaneBlock, ThSingleSideBlocks side)
        //{
        //    var connectList = new List<(Point3d, Point3d)>();
        //    var minHasReturnClosedPair = hasReturnClosePair(returnValueDict, closedDists);
        //    var distClosedPair = closedDists.OrderBy(y => y.Item3).First();

        //    if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturn && returnValueDict[distClosedPair.Item1] < 10000) ||
        //        minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolGroupDistance)
        //    {
        //        connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
        //    }
        //    else
        //    {
        //        connectList.Add((blockList[minHasReturnClosedPair.Item1], thisLaneBlock[minHasReturnClosedPair.Item2]));
        //    }

        //    return connectList;
        //}


        private static List<(Point3d, Point3d)> findOptimalConnection(Dictionary<int, double> returnValueDict, List<(int, int, double)> closedDists, List<Point3d> blockList,
                                                    List<Point3d> thisLaneBlock, ThSingleSideBlocks side)
        {
            var connectList = new List<(Point3d, Point3d)>();
            var filterReturnValueDict = returnValueDict.Where(x => side.blkConnectNo(blockList[x.Key]) < EmgConnectCommon.TolMaxConnect).ToDictionary(x => x.Key, x => x.Value);

            var fileterDist = closedDists.Where(x => side.blkConnectNo(blockList[x.Item1]) < EmgConnectCommon.TolMaxConnect ||
                                                    side.blkConnectNo(blockList[x.Item1]) < EmgConnectCommon.TolMaxConnect ||
                                                    side.blkConnectNo(thisLaneBlock[x.Item2]) < EmgConnectCommon.TolMaxConnect ||
                                                    side.blkConnectNo(thisLaneBlock[x.Item2]) < EmgConnectCommon.TolMaxConnect
                                                ).ToList();



            if (filterReturnValueDict.Count > 0 && fileterDist.Count > 0)
            {
                var minHasReturnClosedPair = hasReturnClosePair(filterReturnValueDict, fileterDist);
                var distClosedPair = fileterDist.OrderBy(y => y.Item3).First();

                if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturn && returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolMaxReturnValue) ||
                    minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolGroupDistance)
                {
                    connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
                }
                else
                {
                    connectList.Add((blockList[minHasReturnClosedPair.Item1], thisLaneBlock[minHasReturnClosedPair.Item2]));
                }
            }
            else
            {
                var distClosedPair = closedDists.OrderBy(y => y.Item3).First();
                connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
            }
            return connectList;
        }

        private static (int, int, double) hasReturnClosePair(Dictionary<int, double> hasReturn, List<(int, int, double)> closedDists)
        {

            //给个抖动范围 否则很容易选错
            var hasReturnMinReturnValue = new Dictionary<int, double>();
            var minReturnValue = hasReturn.Select(returnValue => returnValue.Value).Min();
            foreach (var hasReturnItem in hasReturn)
            {
                if (Math.Abs(hasReturnItem.Value - minReturnValue) <= EmgConnectCommon.TolMinReturnValueRange) //returnValue in min +-500 
                {
                    hasReturnMinReturnValue.Add(hasReturnItem.Key, hasReturnItem.Value);
                }
            }

            var hasReturnClosedPair = closedDists.Where(x => hasReturnMinReturnValue.ContainsKey(x.Item1)).OrderBy(y => y.Item3).First();

            return hasReturnClosedPair;
        }

        private static List<ThSingleSideBlocks> orderSignleSide(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var sideDistDict = new Dictionary<ThSingleSideBlocks, double>();

            foreach (var side in sigleSideGroup)
            {
                if (side.getTotalMainBlock().Count > 0)
                {
                    var dist = side.getTotalMainBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
                    sideDistDict.Add(side, dist);
                }
            }
            var orderSingleSide = sideDistDict.OrderBy(x => x.Value).Select(x => x.Key).ToList();

            return orderSingleSide;
        }

        private static void checkReturn(BlockReference ALE, List<Point3d> blockList, List<Point3d> singleSideBlock, Dictionary<int, double> returnValueDict)
        {
            for (int r = 0; r < blockList.Count; r++)
            {

                var baseVector = blockList[r] - ALE.Position;
                var localReturnDict = new List<double>();

                for (int i = 0; i < singleSideBlock.Count; i++)
                {
                    var iVector = singleSideBlock[i] - blockList[r];
                    double returnValue = 0;
                    //debug change 1 to parameter
                    double vx = Math.Abs(iVector.X) <= EmgConnectCommon.TolReturnRange ? 0 : iVector.X;
                    double vy = Math.Abs(iVector.Y) <= EmgConnectCommon.TolReturnRange ? 0 : iVector.Y;

                    if (baseVector.X * vx >= 0 && baseVector.Y * vy >= 0)
                    {
                        localReturnDict.Add(0);
                    }
                    else
                    {
                        if (baseVector.X * vx < 0)
                        {
                            returnValue = Math.Abs(vx);
                        }
                        if (baseVector.Y * vy < 0)
                        {
                            returnValue = returnValue + Math.Abs(vy);
                        }
                        localReturnDict.Add(returnValue);
                    }
                }

                var maxReturnValue = localReturnDict.Max();

                returnValueDict.Add(r, maxReturnValue);

            }
        }

        /// <summary>
        /// (blocklist index, focused side index, distance)
        /// </summary>
        /// <param name="singleSideBlock"></param>
        /// <param name="blockList"></param>
        /// <returns></returns>
        private static List<(int, int, double)> findClosePoint(List<Point3d> singleSideBlock, List<Point3d> blockList)
        {
            List<(int, int, double)> closedPoints = new List<(int, int, double)>(); // （ index of blockList，index of sigleSideBlock, distance)， distance）


            for (int r = 0; r < blockList.Count; r++)
            {

                for (int i = 0; i < singleSideBlock.Count; i++)
                {
                    var dist = singleSideBlock[i].DistanceTo(blockList[r]);
                    closedPoints.Add((r, i, dist));
                }

            }
            return closedPoints;
        }

    }
}
