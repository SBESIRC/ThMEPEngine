using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;
using ThMEPLighting.EmgLightConnect.Model;
using ThMEPLighting.EmgLight.Service;


namespace ThMEPLighting.EmgLightConnect.Service
{
    class findOptimalGroupService
    {
        public static List<List<List<int>>> findGroupPath(List<List<List<int>>> allPath,  List<ThSingleSideBlocks> singleSideBlocks)
        {
            List<List<List<int>>> allGroupPath = new List<List<List<int>>>();
            //Dictionary<List<int>, List<List<int>>> calculatedGroupPath();

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

                        if (sumNo(blockSum, blockCount) && distanceInTol(partPathBlocks, laneSideBlocks))
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
                allGroupPath.Add(groupPath);
            }

            return allGroupPath;
        }

        private static bool sumNo(int blockSum, int blockCount)
        {
            var bReturn = false;

            if ((blockSum + blockCount) <= EmgConnectCommon.TolMaxLigthNo)
            {
                bReturn = true;
            }
            return bReturn;
        }

        private static bool distanceInTol(List<ThSingleSideBlocks> partPathBlocks, ThSingleSideBlocks laneSideBlocks)
        {
            var bReturn = false;


            var partPathBlocksPoints = partPathBlocks.SelectMany(x => x.getTotalMainBlock()).ToList();
            var laneSideBlocksPoints = laneSideBlocks.getTotalMainBlock();

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
                    double dist = pointInNextLane.DistanceTo(pointInGroup);
                    if (dist <= EmgConnectCommon.TolSaperateGroupMaxDistance)
                    {
                        bReturn = true;
                        break;
                    }
                }

            }


            return bReturn;
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
                var pathBlocks = new List<ThSingleSideBlocks>();
                foreach (int laneSideIndex in path)
                {
                    var laneSideBlocks = singleSideBlocks.Where(x => x.laneSideNo == laneSideIndex).FirstOrDefault();

                    pathBlocks.Add(laneSideBlocks);
                }
                OptimalGroupBlocks.Add(pathBlocks);
            }

            return OptimalGroupBlocks;
        }

        private static double GetVariance(List<int> distX)
        {
            double avg = 0;
            double variance = 0;

            avg = distX.Sum() / distX.Count;

            for (int i = 0; i < distX.Count ; i++)
            {
                variance += Math.Pow(distX[i] - avg, 2);
            }
            variance = Math.Sqrt(variance / distX.Count);

            return variance;
        }
    }
}
