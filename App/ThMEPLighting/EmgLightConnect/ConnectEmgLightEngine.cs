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

namespace ThMEPLighting.EmgLightConnect
{
    public class ConnectEmgLightEngine
    {
        public static void ConnectLight(List<List<Line>> mergedOrderedLane, Dictionary<EmgConnectCommon.BlockType, List<BlockReference>> blockSourceList, Polyline frame)
        {

            //if (emgLightList.Count == 0 && evacList.Count == 0 && emgExitList.Count == 0)
            //{
            //    return;
            //}

            ////单侧车道灯
            //block块分主副组。主：应急灯和疏散灯。 副：出口灯其他块
            var blockDict = new Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>>();

            SingleSideBlockService.addMainGroupBlockList(blockSourceList, ref blockDict, out var emgEvacGroup);
            SingleSideBlockService.addSecBlockList(blockSourceList, ref blockDict);

            //沿车线将block成单链
            var singleSideBlocks = SingleSideBlockService.groupSingleSideBlocks(mergedOrderedLane, blockDict, emgEvacGroup);
            SingleSideBlockService.restBlockToSingleSideBlocks(blockDict, singleSideBlocks, emgEvacGroup);

            //blockDict应为空，如果有block存在则有问题
            foreach (var a in blockDict)
            {
                foreach (var ba in a.Value)
                {
                    DrawUtils.ShowGeometry(ba.Position, EmgConnectCommon.LayerBlockCenter, Color.FromColorIndex(ColorMethod.ByColor, 50), LineWeight.LineWeight035);
                }
            }
            ////////

            ////分组
            //沿车道线成环分区
            orderSingleSideLaneService.orderOutterSingleSideLane(mergedOrderedLane, frame, out var pointList, out var notTravelledList, out var orderedOutterLaneSideList);
            orderSingleSideLaneService.orderInnerSigleSideLane(pointList, notTravelledList, orderedOutterLaneSideList, out var orderedAllLaneSideList);

            //合并共线且不被打断的车道
            MergeSideService.mergeSide(orderedAllLaneSideList, out var sideDict);
            MergeSideService.mergeSigleSideBlocks(sideDict, singleSideBlocks);

            //singleSideBlocks.ForEach(x => x.orderLane());
            //singleSideBlocks.ForEach(x => x.orderBlk(x.mainBlk));

            //车道道路成图分析可能的路径
            graphService.createOutterGraph(orderedAllLaneSideList[0], sideDict, singleSideBlocks, out var sideGraph);
            graphService.createInnerGraph(orderedAllLaneSideList, sideDict, sideGraph);

            MergeSideService.mergeOneBlockSide(singleSideBlocks, pointList, sideGraph);

            //连线
            var ALE = blockSourceList[EmgConnectCommon.BlockType.ale].First();
            connectSingleSideBlkService.connectMainToMain(singleSideBlocks);
            //connectSingleSideBlkService.connectSecToMain(ALE, singleSideBlocks);
            //connectSingleSideBlkService.connectSecToMain2(ALE, singleSideBlocks);
            connectSingleSideBlkService.connecSecToMain3(ALE, singleSideBlocks);

            ////////debug 打图，要删
            ConnectSingleSideService.forDebugSingleSideBlocks2(singleSideBlocks);

            bool b = true;
            if (b == true)
            {
                return;
            }

            //找出所有图可能的连通性
            var allPath = graphService.SeachGraph(sideGraph);

            //找分组方式
            var allGroupPath = findOptimalGroupService.findGroupPath(allPath, singleSideBlocks);

            //找最佳分组方式
            var OptimalGroupBlocks = findOptimalGroupService.findOptimalGroup(allGroupPath, singleSideBlocks);

            ////////debug 打图，要删
            ConnectSingleSideService.forDebugOptimalGroup(OptimalGroupBlocks);
            ////////

            ////组内连线
            var connectList = connectSingleSideInGroupService.connectAllSingleSide(ALE, OptimalGroupBlocks);
         
            ////////debug 打图，要删
            ConnectSingleSideService.forDebugConnectLine(connectList);





        }




    }
}
