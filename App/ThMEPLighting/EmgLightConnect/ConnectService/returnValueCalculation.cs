using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLightConnect.Model;

namespace ThMEPLighting.EmgLightConnect.Service
{
    public class returnValueCalculation
    {
        public static Dictionary<int, double> getReturnValueInGroup(BlockReference ALE, List<Point3d> blockList, List<Point3d> singleSideBlock)
        {
            Dictionary<int, double> returnValueDict = new Dictionary<int, double>();

            for (int r = 0; r < blockList.Count; r++)
            {

                var baseVector = blockList[r] - ALE.Position;

                //double bx = baseVector.X;
                //double by = baseVector.Y;

                double bx = Math.Abs(baseVector.X) <= 4500 ? 0 : baseVector.X;
                double by = Math.Abs(baseVector.Y) <= 4500 ? 0 : baseVector.Y;

                var localReturnDict = new List<double>();


                for (int i = 0; i < singleSideBlock.Count; i++)
                {
                    var iVector = singleSideBlock[i] - blockList[r];
                    double returnValue = 0;

                    double vx = Math.Abs(iVector.X) <= 6000 ? 0 : iVector.X;
                    double vy = Math.Abs(iVector.Y) <= 6000 ? 0 : iVector.Y;

                    if (bx * vx >= 0 && by * vy >= 0)
                    {
                        returnValue = 0;
                    }
                    else
                    {
                        if (bx * vx < 0)
                        {
                            returnValue = Math.Abs(vx);
                        }
                        if (by * vy < 0)
                        {
                            returnValue = returnValue + Math.Abs(vy);
                        }

                    }
                    localReturnDict.Add(returnValue);

                }

                var maxReturnValue = localReturnDict.Max();

                returnValueDict.Add(r, maxReturnValue);

            }

            return returnValueDict;
        }

        /// <summary>
        /// (blocklist index, focused side index, distance)
        /// </summary>
        /// <param name="singleSideBlock"></param>
        /// <param name="blockList"></param>
        /// <returns></returns>
        public static List<(int, int, double)> getDistMatrix(List<Point3d> blockList, List<Point3d> singleSideBlock)
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

        public static List<(Point3d, Point3d)> findOptimalConnectionInGroupNoUse(Dictionary<int, double> returnValueDict, List<(int, int, double)> closedDists, List<Point3d> blockList,
                                              List<Point3d> thisLaneBlock, List<ThSingleSideBlocks> sideList)
        {
            var connectList = new List<(Point3d, Point3d)>();

            var filterReturnValueDict = returnValueDict.Where(x => overConnect(blockList[x.Key], sideList) == false).ToDictionary(x => x.Key, x => x.Value);

            var fileterDist = closedDists.Where(x => overConnect(blockList[x.Item1], sideList) == false &&
                                                  overConnect(blockList[x.Item1], sideList) == false &&
                                                  overConnect(thisLaneBlock[x.Item2], sideList) == false &&
                                                  overConnect(thisLaneBlock[x.Item2], sideList) == false
                                              ).ToList();

            if (filterReturnValueDict.Count > 0 && fileterDist.Count > 0)
            {
                var minHasReturnClosedPair = hasReturnClosePair(filterReturnValueDict, fileterDist);
                var distClosedPair = fileterDist.OrderBy(y => y.Item3).First();

                //    //if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturnValueDistCheck && returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolMaxReturnValue) ||
                //    //    minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolSaperateGroupMaxDistance ||
                //    //    distClosedPair.Item3 <= EmgConnectCommon.TolTooClosePt || minHasReturnClosedPair.Item3 > 13000)
                if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturnValueDistCheck && returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolMaxReturnValue) ||
                    minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolSaperateGroupMaxDistance ||
                    distClosedPair.Item3 <= EmgConnectCommon.TolTooClosePt)

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

        public static List<(Point3d, Point3d)> findOptimalConnectionInGroup(Dictionary<int, double> returnValueDict, List<(int, int, double)> closedDists, List<Point3d> blockList,
                                            List<Point3d> thisLaneBlock, List<ThSingleSideBlocks> sideList)
        {
            var bConn = false;
            var connectList = new List<(Point3d, Point3d)>();

            var filterReturnValueDict = returnValueDict.Where(x => overConnect(blockList[x.Key], sideList) == false).ToDictionary(x => x.Key, x => x.Value);

            var fileterDist = closedDists.Where(x => overConnect(blockList[x.Item1], sideList) == false &&
                                                  overConnect(blockList[x.Item1], sideList) == false &&
                                                  overConnect(thisLaneBlock[x.Item2], sideList) == false &&
                                                  overConnect(thisLaneBlock[x.Item2], sideList) == false
                                              ).ToList();

            if (filterReturnValueDict.Count > 0 && fileterDist.Count > 0)
            {
                var minHasReturnClosedPair = hasReturnClosePair(filterReturnValueDict, fileterDist);
                var distClosedPair = fileterDist.OrderBy(y => y.Item3).First();

                //if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturnValueDistCheck && returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolMaxReturnValue) ||
                //    minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolSaperateGroupMaxDistance ||
                //    distClosedPair.Item3 <= EmgConnectCommon.TolTooClosePt)

                //{
                //    connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
                //    bConn = true;
                //}
                //else
                //{
                //    connectList.Add((blockList[minHasReturnClosedPair.Item1], thisLaneBlock[minHasReturnClosedPair.Item2]));
                //    bConn = true;
                //}


                if (minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturnValueDistCheck && returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolMaxReturnValue)
                {
                    connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
                    bConn = true;
                }
                if (bConn == false && minHasReturnClosedPair.Item3 >= 20000)
                {
                    connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
                    bConn = true;
                }
                if (bConn == false && distClosedPair.Item3 <= EmgConnectCommon.TolTooClosePt)
                {
                    connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
                    bConn = true;
                }

                if (bConn == false)
                {
                    connectList.Add((blockList[minHasReturnClosedPair.Item1], thisLaneBlock[minHasReturnClosedPair.Item2]));
                    bConn = true;
                }

            }

            if (bConn == false)
            {
                var distClosedPair = closedDists.OrderBy(y => y.Item3).First();
                connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
            }

            return connectList;
        }


        private static bool overConnect(Point3d pt, List<ThSingleSideBlocks> sideList)
        {
            var bOver = false;

            var side = sideList.Where(x => x.getTotalBlock().Contains(pt)).First();
            if (side.blkConnectNo(pt) >= EmgConnectCommon.TolBlkMaxConnect)
            {
                bOver = true;
            }

            return bOver;
        }

        private static (int, int, double) hasReturnClosePair(Dictionary<int, double> hasReturn, List<(int, int, double)> closedDists)
        {

            //给个抖动范围 否则很容易选错
            var hasReturnMinReturnValue = new Dictionary<int, double>();
            var minReturnValue = hasReturn.Select(returnValue => returnValue.Value).Min();
            foreach (var hasReturnItem in hasReturn)
            {
                if (Math.Abs(hasReturnItem.Value - minReturnValue) <= EmgConnectCommon.TolReturnValueMinRange) //returnValue in min +-500 
                {
                    hasReturnMinReturnValue.Add(hasReturnItem.Key, hasReturnItem.Value);
                }
            }

            var hasReturnClosedPair = closedDists.Where(x => hasReturnMinReturnValue.ContainsKey(x.Item1)).OrderBy(y => y.Item3).First();

            return hasReturnClosedPair;
        }

        //////////////////////////////not used but may used/////////////////////

        public static Dictionary<int, double> getReturnValueInGroup2(BlockReference ALE, List<Point3d> blockList, List<Point3d> singleSideBlock)
        {
            Dictionary<int, double> returnValueDict = new Dictionary<int, double>();

            for (int r = 0; r < blockList.Count; r++)
            {

                var baseVector = blockList[r] - ALE.Position;

                double bx = Math.Abs(baseVector.X) <= 1000 ? 0 : baseVector.X;
                double by = Math.Abs(baseVector.Y) <= 1000 ? 0 : baseVector.Y;

                var localReturnDict = new List<double>();


                for (int i = 0; i < singleSideBlock.Count; i++)
                {
                    var iVector = singleSideBlock[i] - blockList[r];
                    double returnValue = 0;

                    double vx = getVectorReturnValue(iVector.X);
                    double vy = getVectorReturnValue(iVector.Y);

                    if (bx * vx >= 0 && by * vy >= 0)
                    {
                        returnValue = 0;
                    }
                    else
                    {
                        if (bx * vx < 0)
                        {
                            returnValue = Math.Abs(vx);
                        }
                        if (by * vy < 0)
                        {
                            returnValue = returnValue + Math.Abs(vy);
                        }

                    }
                    localReturnDict.Add(returnValue);

                }

                var maxReturnValue = localReturnDict.Max();

                returnValueDict.Add(r, maxReturnValue);

            }

            return returnValueDict;
        }

        private static double getVectorReturnValue(double oriValue)
        {
            double n = -1;

            if (Math.Abs(oriValue) <= 1000)
            {
                n = 0;
            }
            else if (Math.Abs(oriValue) <= 6000)
            {
                n = Math.Abs(oriValue) / oriValue * 600;
                n = Math.Abs(oriValue) <= 600 ? oriValue : n;
            }
            else
            {
                n = oriValue;
            }
            return n;
        }


        public static List<(Point3d, Point3d)> findOptimalConnectionInGroup2(Dictionary<int, double> returnValueDict, List<(int, int, double)> closedDists, List<Point3d> blockList,
                                     List<Point3d> thisLaneBlock, List<ThSingleSideBlocks> sideList)
        {
            var connectList = new List<(Point3d, Point3d)>();
            bool bAdd = false;
            var filterReturnValueDict = returnValueDict.Where(x => overConnect(blockList[x.Key], sideList) == false).ToDictionary(x => x.Key, x => x.Value);

            var fileterDist = closedDists.Where(x => overConnect(blockList[x.Item1], sideList) == false &&
                                                      overConnect(blockList[x.Item1], sideList) == false &&
                                                      overConnect(thisLaneBlock[x.Item2], sideList) == false &&
                                                      overConnect(thisLaneBlock[x.Item2], sideList) == false
                                              ).ToList();

            if (filterReturnValueDict.Count > 0 && fileterDist.Count > 0)
            {

                fileterDist = fileterDist.OrderBy(x => x.Item3).ToList();
                filterReturnValueDict = filterReturnValueDict.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);





            }
            if (bAdd == false)
            {

                var distClosedPair = closedDists.OrderBy(y => y.Item3).First();
                connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
            }
            return connectList;
        }








        ///////////////////////////////for sec to main return value/////////////////
        /// <summary>
        /// 副点连主点的回头量计算
        /// </summary>
        /// <param name="ALE"></param>
        /// <param name="mainBlks"></param>
        /// <param name="secPt"></param>
        /// <returns></returns>
        public static Dictionary<int, double> getReturnValueInSide(BlockReference ALE, List<Point3d> mainBlks, Point3d secPt)
        {
            Dictionary<int, double> returnValueDict = new Dictionary<int, double>();

            for (int r = 0; r < mainBlks.Count; r++)
            {

                var baseVector = mainBlks[r] - ALE.Position;

                var iVector = secPt - mainBlks[r];
                double returnValue = 0;

                double vx = Math.Abs(iVector.X) <= EmgConnectCommon.TolReturnRangeInSide ? 0 : iVector.X;
                double vy = Math.Abs(iVector.Y) <= EmgConnectCommon.TolReturnRangeInSide ? 0 : iVector.Y;

                if (baseVector.X * vx >= 0 && baseVector.Y * vy >= 0)
                {
                    returnValue = 0;
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

                }
                returnValueDict.Add(r, returnValue);
            }


            return returnValueDict;
        }

        public static Point3d findOptimalConnectionInSide(Dictionary<int, double> returnValueDict, List<(int, int, double)> closedDists, List<Point3d> mainList,
                                                            Point3d secPt, ThSingleSideBlocks side)
        {
            var keyMainPt = new Point3d();

            var filterReturnValueDict = returnValueDict.Where(x => side.blkConnectNo(mainList[x.Key]) < EmgConnectCommon.TolBlkMaxConnect).ToDictionary(x => x.Key, x => x.Value);

            var fileterDist = closedDists.Where(x => side.blkConnectNo(mainList[x.Item1]) < EmgConnectCommon.TolBlkMaxConnect &&
                                                    side.blkConnectNo(mainList[x.Item1]) < EmgConnectCommon.TolBlkMaxConnect &&
                                                    side.blkConnectNo(secPt) < EmgConnectCommon.TolBlkMaxConnect &&
                                                    side.blkConnectNo(secPt) < EmgConnectCommon.TolBlkMaxConnect
                                                ).ToList();


            if (filterReturnValueDict.Count > 0 && fileterDist.Count > 0)
            {
                var minHasReturnClosedPair = hasReturnClosePair(filterReturnValueDict, fileterDist);
                var distClosedPair = fileterDist.OrderBy(y => y.Item3).First();

                if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturnValueDistCheck && returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolMaxReturnValue) ||
                    minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolSaperateGroupMaxDistance)
                {
                    keyMainPt = mainList[distClosedPair.Item1];
                }
                else
                {
                    keyMainPt = mainList[minHasReturnClosedPair.Item1];
                }
            }
            else
            {
                var distClosedPair = closedDists.OrderBy(y => y.Item3).First();
                keyMainPt = mainList[distClosedPair.Item1];
            }

            return keyMainPt;
        }
    }
}
