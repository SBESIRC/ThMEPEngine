#if (ACAD2016 || ACAD2018)
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;
using CLI;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDConnectCoolSupplyEngine
    {
        public static List<Line> ThConnectCoolSupplyEngine(List<ThExtractorBase> archiExtractor, List<ThIfcSanitaryTerminalToilate> allToilateList, ThDrainageSDDataExchange dataSet)
        {
            var allLink = new List<Line>();

            var toilateList = allToilateList.Where(x => x.SupplyCool.Count > 0).ToList();

            var roomPolyList = ThDrainageSDRoomService.getRoomList(archiExtractor);
            var roomList = ThDrainageSDRoomService.buildRoomModel(roomPolyList, toilateList);
            var filteredRoom = ThDrainageSDRoomService.filtRoomList(roomList);

            if (filteredRoom == null || toilateList == null || filteredRoom.Count == 0 || toilateList.Count == 0)
            {
                return allLink;
            }

            var supplyStartExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageSDColdWaterSupplyStartExtractor)) as ThDrainageSDColdWaterSupplyStartExtractor;
            var supplyStart = (supplyStartExtractor.ColdWaterSupplyStarts[0].Geometry as DBPoint).Position;

            toilateList.ForEach(x => x.AreaId = dataSet.AreaID);

            //所有空间建model 包括没有厕所的空间（后续建图需要）


            //确定每个厕所在墙上的给水点位
            ThDrainageSDCoolPtService.findCoolSupplyPt(roomList, toilateList, out var aloneToilate);
            //toilateList.ForEach(x => x.SupplyCoolOnWall.ForEach(pt => DrawUtils.ShowGeometry(pt, "l0SupplyOnWall", 50, 35, 20, "C")));

            ///////////收缩外框
            //ThDrainageSDRoomService.shrinkRoom(archiExtractor);
            //var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToilateRoomExtractor)) as ThDrainageToilateRoomExtractor;
            //roomExtractor.Rooms.ForEach(room =>
            //{
            //    DrawUtils.ShowGeometry(room.Boundary, "l0room", 44);
            //});
            /////////////////


            //////////分组  
            var forGroupJson = ThDrainageSDToGJsonService.buildArchiGeometry(archiExtractor);
            forGroupJson.AddRange(ThDrainageSDToGJsonService.buildCoolPtGeometry(toilateList));
            var forGroupJsonString = ThGeoOutput.Output(forGroupJson);
            string path = @"D:\project\2.drainage\jsonSample\2-1.input.geojson";
            File.WriteAllText(path, forGroupJsonString);

            ThPipeSystemDiagramMgd SystemDiagramMethods = new ThPipeSystemDiagramMgd();
            var groupOutput = SystemDiagramMethods.ProcessGrouping(forGroupJsonString);
            string path2 = @"D:\project\2.drainage\jsonSample\2-2.output.geojson";
            File.WriteAllText(path2, groupOutput);

            //解析分组，更新点位信息
            var tGJList = ThDrainageSDDeserializeGJsonService.getGroupPt(groupOutput);
            var subLink = ThDrainageSDToGJsonService.updateToilateModel(tGJList, toilateList);
            allLink.AddRange(subLink);
            DrawUtils.ShowGeometry(subLink, "l05sub", 130);
            ////////////////////

            //找主线虚拟点位
            var groupList = ThDrainageSDCoolPtProcessService.classifyToilate(toilateList);
            var islandPair = ThDrainageSDCoolPtProcessService.mergeIsland(groupList);
          
            var virtualPtList = ThDrainageSDVirtualPtEngine.getVirtualPtOfGroup(supplyStart, groupList, islandPair, roomList, out var ptForVirtualDict, out var allToiInGroup);
            ////debug drawing
            virtualPtList.ForEach(pt =>
            {
                DrawUtils.ShowGeometry(pt.Pt, "l04virtualPt", 220, 25, 40, "C");
            });

            //防止穿自身，虚拟柱子
            var virtualColumn = ThDrainageSDVirtualColumnEngine.getVirtualColumn(groupList, islandPair, allToiInGroup, ptForVirtualDict);
            DrawUtils.ShowGeometry(virtualColumn, "l04virtualColumn", 12);

            //////找主线
            var forBranchJson = ThDrainageSDToGJsonService.buildArchiGeometry(archiExtractor);
            forBranchJson.AddRange(ThDrainageSDToGJsonService.buildVirtualCoolPtGeomary(virtualPtList));
            forBranchJson.AddRange(ThDrainageSDToGJsonService.buildVirtualColumn(virtualColumn, dataSet.AreaID));

            var forBranchJsonString = ThGeoOutput.Output(forBranchJson);
            string path3 = @"D:\project\2.drainage\jsonSample\2-3.input.geojson";
            File.WriteAllText(path3, forBranchJsonString);

            var branchOutput = SystemDiagramMethods.ProcessMainBranchs(forBranchJsonString);
            string path4 = @"D:\project\2.drainage\jsonSample\2-4.output.geojson";
            File.WriteAllText(path4, branchOutput);

            var branchList = ThDrainageSDDeserializeGJsonService.getBranchLineList(branchOutput);
            allLink.AddRange(branchList);
            DrawUtils.ShowGeometry(branchList, "l05branch", 150);

            //连支干管
            var subBranchList = ThDrainageSDConnectSubService.linkGroupSub(groupList, ptForVirtualDict, islandPair, branchList);
            allLink.AddRange(subBranchList);
            DrawUtils.ShowGeometry(subBranchList, "l05subBranch", 140);

            //DrawUtils.ShowGeometry(allLink, "l06linkNoClean", 170);

            //清理线和线头
            var lines = ThDrainageSDCleanLineService.simplifyLine(allLink);
            var ptOnWall = toilateList.SelectMany(x => x.SupplyCoolOnWall).ToList();
            ptOnWall.Add(supplyStart);
            ThDrainageSDCleanLineService.cleanNoUseLines(ptOnWall, ref lines);

            dataSet.TerminalList = toilateList;
            dataSet.IslandPair = islandPair;
            dataSet.GroupList = groupList;
            dataSet.SupplyCoolStart = supplyStart;
            dataSet.Pipes = lines;

            return lines;
        }
    }
}
#endif
