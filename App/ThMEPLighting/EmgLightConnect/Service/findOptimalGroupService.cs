using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Service;


namespace ThMEPLighting.EmgLightConnect.Service
{
    class findOptimalGroupService
    {
        //public static List<List<List<int>>> findGroupPath(List<List<List<int>>> allPath, List<ThSingleSideBlocks> singleSideBlocks)
        //{
        //    List<List<List<int>>> allGroupPath = new List<List<List<int>>>();
        //    //Dictionary<List<int>, List<List<int>>> calculatedGroupPath();

        //    foreach (var path in allPath)
        //    {
        //        var groupPath = new List<List<int>>();
        //        var groupPathBlocks = new List<List<ThSingleSideBlocks>>();

        //        foreach (var part in path)
        //        {
        //            int blockSum = 0;
        //            var partPath = new List<int>();
        //            groupPath.Add(partPath);

        //            var partPathBlocks = new List<ThSingleSideBlocks>();
        //            groupPathBlocks.Add(partPathBlocks);

        //            foreach (int laneSideIndex in part)
        //            {

        //                var laneSideBlocks = singleSideBlocks.Where(x => x.laneSideNo == laneSideIndex).FirstOrDefault();
        //                var blockCount = laneSideBlocks.Count;

        //                if (sumNo(blockSum, blockCount) && distanceInTol(partPathBlocks, laneSideBlocks))
        //                {
        //                    blockSum = blockSum + blockCount;
        //                    partPath.Add(laneSideIndex);
        //                    partPathBlocks.Add(laneSideBlocks);

        //                }
        //                else
        //                {
        //                    blockSum = blockCount;
        //                    partPath = new List<int>();
        //                    partPath.Add(laneSideIndex);
        //                    groupPath.Add(partPath);

        //                    partPathBlocks = new List<ThSingleSideBlocks>();
        //                    partPathBlocks.Add(laneSideBlocks);
        //                    groupPathBlocks.Add(partPathBlocks);
        //                }
        //            }
        //        }
        //        allGroupPath.Add(groupPath);
        //    }

        //    return allGroupPath;
        //}


        public static List<List<List<int>>> findGroupPath(List<List<List<int>>> allPath, List<ThSingleSideBlocks> singleSideBlocks, List<Polyline> holes,int groupMin, int groupMax)
        {
            List<List<List<int>>> allGroupPathTemp = new List<List<List<int>>>();
            List<List<List<int>>> allGroupPath = new List<List<List<int>>>();
            Dictionary<int, int> sideCountDict = new Dictionary<int, int>();

            foreach (var path in allPath)
            {
                var groupPath = new List<List<int>>();
                var groupPathBlocks = new List<List<ThSingleSideBlocks>>();

                foreach (var part in path)
                {
                    int blockSum = 0;
                    var partPath = new List<int>();
                    groupPath.Add(partPath);

                    var partPathBlocks = new List<ThSingleSideBlocks>();
                    groupPathBlocks.Add(partPathBlocks);

                    foreach (int laneSideIndex in part)
                    {
                        var laneSideBlocks = singleSideBlocks.Where(x => x.laneSideNo == laneSideIndex).FirstOrDefault();
                        var blockCount = laneSideBlocks.Count;

                        if (sideCountDict.ContainsKey(laneSideIndex) == false)
                        {
                            sideCountDict.Add(laneSideIndex, blockCount);
                        }

                        if (sumNo(blockSum, blockCount,groupMax ) && distanceInTol(partPathBlocks, laneSideBlocks, holes))
                        {
                            blockSum = blockSum + blockCount;
                            partPath.Add(laneSideIndex);
                            partPathBlocks.Add(laneSideBlocks);
                        }
                        else
                        {
                            blockSum = blockCount;
                            partPath = new List<int>();
                            partPath.Add(laneSideIndex);
                            groupPath.Add(partPath);

                            partPathBlocks = new List<ThSingleSideBlocks>();
                            partPathBlocks.Add(laneSideBlocks);
                            groupPathBlocks.Add(partPathBlocks);
                        }
                    }
                }

                allGroupPathTemp.Add(groupPath);

                if (minBlockCount(groupPath, sideCountDict, groupMin) == false)
                {
                    allGroupPath.Add(groupPath);
                }
            }

            if (allGroupPath.Count == 0)
            {
                allGroupPath = allGroupPathTemp;
            }


            return allGroupPath;
        }


        private static bool sumNo(int blockSum, int blockCount,int groupMax)
        {
            var bReturn = false;

            if ((blockSum + blockCount) <= groupMax)
            {
                bReturn = true;
            }
            return bReturn;
        }

        private static bool distanceInTol(List<ThSingleSideBlocks> partPathBlocks, ThSingleSideBlocks laneSideBlocks, List<Polyline> holes)
        {
            var bReturn = false;
            var tol = EmgConnectCommon.TolSaperateGroupMaxDistance;

            var partPathBlocksPoints = partPathBlocks.SelectMany(x => x.getTotalBlock()).ToList();
            var laneSideBlocksPoints = laneSideBlocks.getTotalBlock();

            if (partPathBlocksPoints.Count == 0)
            {
                bReturn = true;
            }
            if (laneSideBlocksPoints.Count == 0)
            {
                bReturn = true;
            }

            foreach (var pointInGroup in partPathBlocksPoints)
            {
                if (bReturn == true)
                {
                    break;
                }

                foreach (var pointInNextLane in laneSideBlocksPoints)
                {

                    double interDist =   intersectWithHoles(pointInNextLane, pointInGroup, holes);

                    double dist = pointInNextLane.DistanceTo(pointInGroup) + interDist;
                    if (dist <= tol)
                    {
                        bReturn = true;
                        break;
                    }
                }

            }

            return bReturn;
        }

        private static double intersectWithHoles(Point3d pt1, Point3d pt2, List<Polyline> holes)
        {
            double intersect = 0;
            Line l = new Line(pt1, pt2);

            foreach (var hole in holes)
            {
                var intersectPt = hole.Intersect(l, Intersect.OnBothOperands);
                if (intersectPt.Count > 0)
                {
                    intersect = intersect + Math.Sqrt(hole.Area) * 2;
                }
            }

            return intersect;
        }

        private static bool minBlockCount(List<List<int>> groupPath, Dictionary<int, int> sideCountDict,int groupMin)
        {
            var bTooSmallCount = false;

            for (int i = 0; i < groupPath.Count; i++)
            {
                var count = groupPath[i].Select(x => sideCountDict[x]).Sum();
                if (0 < count && count < groupMin)
                {
                    bTooSmallCount = true;
                    break;
                }
            }

            return bTooSmallCount;
        }

        public static List<List<ThSingleSideBlocks>> findOptimalGroup(List<List<List<int>>> allGroupPath, List<ThSingleSideBlocks> singleSideBlocks)
        {
            List<List<int>> OptimalGroup = new List<List<int>>();
            List<List<ThSingleSideBlocks>> OptimalGroupBlocks = new List<List<ThSingleSideBlocks>>();

            Dictionary<List<List<int>>, double> Variance = new Dictionary<List<List<int>>, double>();
            double minVariance = -1;

            foreach (var path in allGroupPath)
            {
                List<int> pathCount = new List<int>();

                foreach (var part in path)
                {

                    var groupCount = 0;

                    foreach (int laneSideIndex in part)
                    {
                        var laneSideBlocks = singleSideBlocks.Where(x => x.laneSideNo == laneSideIndex).FirstOrDefault();
                        var blockCount = laneSideBlocks.Count;

                        groupCount = groupCount + blockCount;
                    }
                    pathCount.Add(groupCount);
                }
                double groupVariance = GetVariance(pathCount);

                if (groupVariance < minVariance || minVariance == -1)
                {
                    minVariance = groupVariance;
                    OptimalGroup = path;
                }
            }

            foreach (var path in OptimalGroup)
            {
                if (path.Count > 0)
                {
                    var pathBlocks = new List<ThSingleSideBlocks>();
                    foreach (int laneSideIndex in path)
                    {
                        var laneSideBlocks = singleSideBlocks.Where(x => x.laneSideNo == laneSideIndex).FirstOrDefault();

                        pathBlocks.Add(laneSideBlocks);
                    }
                    OptimalGroupBlocks.Add(pathBlocks);
                }
            }

            return OptimalGroupBlocks;
        }

        private static double GetVariance(List<int> distX)
        {
            double avg = 0;
            double variance = 0;

            avg = distX.Sum() / distX.Count;

            for (int i = 0; i < distX.Count; i++)
            {
                variance += Math.Pow(distX[i] - avg, 2);
            }
            variance = Math.Sqrt(variance / distX.Count);

            return variance;
        }
    }
}
