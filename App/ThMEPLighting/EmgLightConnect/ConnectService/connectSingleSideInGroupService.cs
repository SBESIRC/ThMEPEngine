using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPLighting.EmgLightConnect.Model;



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

                Dictionary<int, double> returnValueDict = returnValueCalculation.getReturnValueInGroup(ALE, blockList, thisLaneBlock);//key:blockListIndex value:returnValue
                List<(int, int, double)> closedDists = returnValueCalculation.getDistMatrix(blockList, thisLaneBlock); //(blocklist index, focused side index, distance)

                //for printing debug drawing info, save connection line in an additional list
                var connectListTemp = returnValueCalculation.findOptimalConnectionInGroup(returnValueDict, closedDists, blockList, thisLaneBlock, orderSigleSideGroup);

                if (connectListTemp.Count > 0)
                {
                    connectList.AddRange(connectListTemp);
                    orderSigleSideGroup[i].connectPt(connectList[0].Item1, connectList[0].Item2);
                }

                blockList.AddRange(thisLaneBlock);

            }

            return connectList;


        }

        private static List<ThSingleSideBlocks> orderSignleSideNoUse(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
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

        /// <summary>
        /// debug:用最小生成树排序
        /// </summary>
        /// <param name="ALE"></param>
        /// <param name="sigleSideGroup"></param>
        /// <returns></returns>
        private static List<ThSingleSideBlocks> orderSignleSide(BlockReference ALE, List<ThSingleSideBlocks> sigleSideGroup)
        {
            var sideDistDict = new Dictionary<ThSingleSideBlocks, double>();

            foreach (var side in sigleSideGroup)
            {
                //if (side.getTotalMainBlock().Count > 0)
                //{
                //    var dist = side.getTotalMainBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
                //    sideDistDict.Add(side, dist);
                //}
                if (side.getTotalBlock().Count > 0)
                {
                    var dist = side.getTotalBlock().Select(x => x.DistanceTo(ALE.Position)).Min();
                    sideDistDict.Add(side, dist);
                }

            }

            var orderSingleSide = sideDistDict.OrderBy(x => x.Value).Select(x => x.Key).ToList();

            return orderSingleSide;
        }

    }
}
