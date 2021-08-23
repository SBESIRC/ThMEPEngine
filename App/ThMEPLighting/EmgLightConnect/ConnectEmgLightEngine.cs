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
using ThMEPLighting.EmgLight.Common;

namespace ThMEPLighting.EmgLightConnect
{
    public class ConnectEmgLightEngine
    {
        public static List<Polyline> ConnectLight(List<List<Line>> mergedOrderedLane, Dictionary<EmgBlkType.BlockType, List<BlockReference>> blockSourceList, Polyline frame, List<Polyline> holes, int groupMin, int groupMax)
        {
            var newLink = new List<Polyline>();

            ////单侧车道灯
            //block块分主副组。主：应急灯和疏散灯。 副：出口灯其他块
            var blockDict = new Dictionary<EmgConnectCommon.BlockGroupType, List<BlockReference>>();
            var blkSource = blockSourceList.SelectMany(x => x.Value).ToList();
            var ALE = blockSourceList[EmgBlkType.BlockType.ale].First();

            SingleSideBlockService.addMainGroupBlockList(blockSourceList, ref blockDict, out var emgEvacGroup);
            SingleSideBlockService.addSecBlockList(blockSourceList, ref blockDict);

            //沿车线将block成单链
            var singleSideBlocks = SingleSideBlockService.groupSingleSideBlocks(mergedOrderedLane, blockDict, emgEvacGroup);

            //var singleSideBlocks = SingleSideBlockService.classifyMainBlocks(mergedOrderedLane, blockDict, emgEvacGroup);
            //SingleSideBlockService.classifySecBlocks(singleSideBlocks, blockDict, ALE.Position);

            SingleSideBlockService.restBlockToSingleSideBlocks(blockDict, singleSideBlocks, emgEvacGroup);

            //blockDict应为空，如果有block存在则有问题
            foreach (var a in blockDict)
            {
                foreach (var ba in a.Value)
                {
                    DrawUtils.ShowGeometry(ba.Position, EmgConnectCommon.LayerBlockCenter, 50, 35);
                }
            }

            //for (int i = 0; i < singleSideBlocks.Count; i++)
            //{
            //    var side = singleSideBlocks[i];

            //    int color = i % 8;

            //    side.mainBlk.ForEach(x => DrawUtils.ShowGeometry(x, EmgConnectCommon.LayerBlockCenter, color, 35));
            //    side.secBlk.ForEach(x => DrawUtils.ShowGeometry(x, EmgConnectCommon.LayerBlockCenter, color, 35, 200, "S"));

            //}


            ////分组
            //沿车道线成环分区
            orderSingleSideLaneService.orderOutterSingleSideLane(mergedOrderedLane, frame, out var pointList, out var notTravelledList, out var orderedOutterLaneSideList);
            orderSingleSideLaneService.orderInnerSigleSideLane(pointList, notTravelledList, orderedOutterLaneSideList, out var orderedAllLaneSideList);

            //合并共线且不被打断的车道
            MergeSideService.mergeSide(orderedAllLaneSideList, out var sideDict);
            MergeSideService.mergeSigleSideBlocks(sideDict, singleSideBlocks);

            ConnectSingleSideService.forDebugLaneSideNo(singleSideBlocks);

            mergeOneSecBlkSideService.mergeScatterBlockSide(singleSideBlocks);
            ////mergeOneMainBlkSideService.mergeOneBlockSide(singleSideBlocks, pointList, sideGraph);
            mergeOneSecBlkSideService.mergeOneSecBlockSide(singleSideBlocks);


            //连线数据
            reclassMainSec.regroupMainSec(singleSideBlocks, frame, holes);
            connectSingleSideBlkService.connectMainToMain(singleSideBlocks);
            mergeOneSecBlkSideService.relocateSecBlockSide(singleSideBlocks);
            connectSingleSideBlkService.connecSecToMain(ALE.Position, singleSideBlocks, frame);

            ////////debug 打图，要删
            ConnectSingleSideService.forDebugSingleSideBlocks(singleSideBlocks);

            //var bDebug = true;
            //if (bDebug ==true)
            //{
            //    return newLink;
            //}


            //车道道路成图
            graphService.createOutterGraph(orderedAllLaneSideList[0], sideDict, singleSideBlocks, out var sideGraph);
            graphService.createInnerGraph(orderedAllLaneSideList, sideDict, sideGraph);

            //找出图所有的可能路径
            var allPath = graphService.SeachGraph(sideGraph);

            //找分组方式
            var allGroupPath = findOptimalGroupService.findGroupPath(allPath, singleSideBlocks, holes, groupMin, groupMax);

            //找最佳分组方式
            var OptimalGroupBlocks = findOptimalGroupService.findOptimalGroup(allGroupPath, singleSideBlocks);

            ////////debug 打图，要删
            ConnectSingleSideService.forDebugOptimalGroup(OptimalGroupBlocks);
            ////////

            ////组内连线
            var connectList = connectSingleSideInGroupService.connectAllSingleSide(ALE.Position, OptimalGroupBlocks);

            ////////debug 打图，要删
            ConnectSingleSideService.forDebugConnectLine(connectList);

            //连线
            List<Polyline> linkLine = new List<Polyline>();
            var mainLink = drawMainBlkService.drawMainToMain(singleSideBlocks, blkSource, frame, holes, out var blkList, ref linkLine);
            var secLink = drawSecBlkService.drawSecToMain(singleSideBlocks, frame, blkList, ref linkLine, holes);
            var groupLink = drawSecBlkService.drawGroupToGroup(connectList, frame, blkList, ref linkLine, holes);

            ConnectSingleSideService.forDebugBlkOutline(blkList);

            DrawUtils.ShowGeometry(mainLink, EmgConnectCommon.LayerConnectLineFinal, 130);
            DrawUtils.ShowGeometry(secLink, EmgConnectCommon.LayerConnectLineFinal, 70);
            DrawUtils.ShowGeometry(groupLink, EmgConnectCommon.LayerConnectLineFinal, 30);

            newLink = drawCorrectLinkService.CorrectIntersectLink(linkLine, blkList);
            DrawUtils.ShowGeometry(newLink, EmgConnectCommon.LayerFinalFinal, 241);


            return newLink;
        }

        public static void ResetResult(ref List<Polyline> newLink, ThMEPOriginTransformer transformer)
        {
            if (newLink != null && newLink.Count > 0)
            {
                newLink.ForEach(x =>
                {
                    transformer.Reset(x);

                });
            }


        }


    }
}
