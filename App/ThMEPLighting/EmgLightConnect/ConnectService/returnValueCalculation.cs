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

                if (minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturnValueDistCheck &&
                    returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolReturnValueMax)
                {
                    connectList.Add((blockList[distClosedPair.Item1], thisLaneBlock[distClosedPair.Item2]));
                    bConn = true;
                }
                if (bConn == false && minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolReturnValueMaxDistance)
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

        public static Dictionary<int, double> getReturnValueInGroupAngle(Point3d ALE, List<Point3d> blockList, List<Point3d> singleSideBlock)
        {
            Dictionary<int, double> returnValueDict = new Dictionary<int, double>();

            for (int r = 0; r < blockList.Count; r++)
            {

                var baseVector = blockList[r] - ALE;

                double bx = Math.Abs(baseVector.X) <= EmgConnectCommon.TolReturnValue0Approx ? 0 : baseVector.X;
                double by = Math.Abs(baseVector.Y) <= EmgConnectCommon.TolReturnValue0Approx ? 0 : baseVector.Y;

                var angleX = baseVector.GetAngleTo(Vector3d.XAxis);
                if (Math.Abs(Math.Cos(angleX)) >= Math.Abs(Math.Cos(10 * Math.PI / 180)))
                {
                    by = 0;
                }
                if (Math.Abs(Math.Cos(angleX)) <= Math.Abs(Math.Cos(80 * Math.PI / 180)))
                {
                    bx = 0;
                }

                var localReturnDict = new List<double>();


                for (int i = 0; i < singleSideBlock.Count; i++)
                {
                    var iVector = singleSideBlock[i] - blockList[r];
                    double returnValue = 0;

                    double vx = getVectorReturnValue(iVector.X);
                    double vy = getVectorReturnValue(iVector.Y);


                    var angleIX = iVector.GetAngleTo(Vector3d.XAxis);

                    if (Math.Abs(Math.Cos(angleIX)) >= Math.Abs(Math.Cos(10 * Math.PI / 180)))
                    {
                        vy = 0;
                    }
                    if (Math.Abs(Math.Cos(angleIX)) <= Math.Abs(Math.Cos(80 * Math.PI / 180)))
                    {
                        vx = 0;
                    }

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

            if (Math.Abs(oriValue) <= EmgConnectCommon.TolReturnValue0Approx)
            {
                n = 0;
            }
            else if (Math.Abs(oriValue) <= EmgConnectCommon.TolReturnValueRange)
            {
                n = Math.Abs(oriValue) / oriValue * EmgConnectCommon.TolReturnValueRangeTo;
                n = Math.Abs(oriValue) <= EmgConnectCommon.TolReturnValueRangeTo ? oriValue : n;
            }
            else
            {
                n = oriValue;
            }
            return n;
        }


        ///////////InSide///////////////
        /// <summary>
        /// 副点连主点的回头量计算
        /// </summary>
        /// <param name="ALE"></param>
        /// <param name="mainBlks"></param>
        /// <param name="secPt"></param>
        /// <returns></returns>
        public static Dictionary<int, double> getReturnValueInSide(Point3d ALE, List<Point3d> mainBlks, Point3d secPt)
        {
            Dictionary<int, double> returnValueDict = new Dictionary<int, double>();

            for (int r = 0; r < mainBlks.Count; r++)
            {
                double returnValue = 0;

                if (mainBlks[r].DistanceTo(ALE) <= secPt.DistanceTo(ALE))
                {
                    returnValue = getReturnValueTwoPoint(ALE, mainBlks[r], secPt);
                }
                else
                {
                    returnValue = getReturnValueTwoPoint(ALE, secPt, mainBlks[r]);
                }

                //returnValue = getReturnValueTwoPoint(ALE, mainBlks[r], secPt);

                returnValueDict.Add(r, returnValue);
            }

            return returnValueDict;
        }


        private static double getReturnValueTwoPoint(Point3d ALE, Point3d basePt, Point3d connPt)
        {
            var baseVector = basePt - ALE;

            double bx = Math.Abs(baseVector.X) <= EmgConnectCommon.TolReturnValue0Approx ? 0 : baseVector.X;
            double by = Math.Abs(baseVector.Y) <= EmgConnectCommon.TolReturnValue0Approx ? 0 : baseVector.Y;

            var iVector = connPt - basePt;
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

            return returnValue;
        }


        public static Point3d findOptimalConnectionInSide(Dictionary<int, double> returnValueDict, List<(int, int, double)> closedDists, List<Point3d> mainList,
                                                            Point3d secPt, ThSingleSideBlocks side)
        {
            var keyMainPt = new Point3d();
            var bConn = false;
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

                if ((minHasReturnClosedPair.Item3 >= distClosedPair.Item3 + EmgConnectCommon.TolReturnValueDistCheck &&
                    returnValueDict[distClosedPair.Item1] < EmgConnectCommon.TolReturnValueMax))
                {
                    keyMainPt = mainList[distClosedPair.Item1];
                    bConn = true;
                }

                if (bConn == false && minHasReturnClosedPair.Item3 >= EmgConnectCommon.TolSaperateGroupMaxDistance)
                {
                    keyMainPt = mainList[distClosedPair.Item1];
                    bConn = true;
                }

                if (bConn == false)
                {
                    keyMainPt = mainList[minHasReturnClosedPair.Item1];
                    bConn = true;
                }
            }

            if (bConn == false)
            {
                var distClosedPair = closedDists.OrderBy(y => y.Item3).First();
                keyMainPt = mainList[distClosedPair.Item1];
            }

            return keyMainPt;

        }


    }
}
