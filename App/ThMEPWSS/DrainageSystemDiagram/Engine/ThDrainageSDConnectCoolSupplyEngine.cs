#if (ACAD2016 || ACAD2018)
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

using CLI;
using ThMEPEngineCore.Diagnostics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;


namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDConnectCoolSupplyEngine
    {
        public static List<Line> ThConnectCoolSupplyEngine(List<ThExtractorBase> archiExtractor, List<ThTerminalToilet> allToiletList, ThDrainageSDDataExchange dataSet)
        {
            var allLink = new List<Line>();

            var toiletList = allToiletList.Where(x => x.SupplyCool.Count > 0).ToList();

            //所有空间建model 包括没有厕所的空间（后续建图需要）
            var roomPolyList = ThDrainageSDRoomService.getRoomList(archiExtractor);
            var roomList = ThDrainageSDRoomService.buildRoomModel(roomPolyList, toiletList);
            var filteredRoom = ThDrainageSDRoomService.filtRoomList(roomList);

            if (filteredRoom == null || toiletList == null || filteredRoom.Count == 0 || toiletList.Count == 0)
            {
                ThDrainageSDMessageServie.WriteMessage(ThDrainageSDMessageCommon.noRoomToilet);
                return allLink;
            }

            //确定每个厕所在墙上的给水点位,调整厕所方向
            ThDrainageSDCoolPtService.findCoolSupplyPt(roomList, toiletList, out var aloneToilet);

            foreach (var terminal in toiletList)
            {
                terminal.SupplyCoolOnWall.ForEach(pt => DrawUtils.ShowGeometry(pt, "l0SupplyOnWall", 50, 35, 20, "C"));

                Point3d leftBPt = terminal.Boundary.GetPoint3dAt(0);
                Point3d leftPt = terminal.Boundary.GetPoint3dAt(1);
                Point3d rightPt = terminal.Boundary.GetPoint3dAt(2);
                Point3d rightPt2 = terminal.Boundary.GetPoint3dAt(3);

                DrawUtils.ShowGeometry(leftBPt, "0", "l1bounary", 70, 25, 20);
                DrawUtils.ShowGeometry(leftPt, "1", "l1bounary", 30, 25, 20);
                DrawUtils.ShowGeometry(rightPt, "2", "l1bounary", 213, 25, 20);
                DrawUtils.ShowGeometry(rightPt2, "3", "l1bounary", 152, 25, 20);

                DrawUtils.ShowGeometry(leftBPt, "l1bounary", 70, 25, 20);
                DrawUtils.ShowGeometry(leftPt, "l1bounary", 30, 25, 20);
                DrawUtils.ShowGeometry(rightPt, "l1bounary", 213, 25, 20);
                DrawUtils.ShowGeometry(rightPt2, "l1bounary", 152, 25, 20);

                terminal.SupplyCool.ForEach(x => DrawUtils.ShowGeometry(x, "l1coolPt", 150, 30, 20, "X"));
            }
            roomList.ForEach(x => DrawUtils.ShowGeometry(x.outline, "l0room", 21));

            ////debug
            //return allLink;

            var supplyStart = dataSet.SupplyStart.Pt;
            toiletList.ForEach(x => x.AreaId = dataSet.AreaID);

            ///////////收缩外框
            //ThDrainageSDRoomService.shrinkRoom(archiExtractor);
            //var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToiletRoomExtractor)) as ThDrainageToiletRoomExtractor;
            //roomExtractor.Rooms.ForEach(room =>
            //{
            //    DrawUtils.ShowGeometry(room.Boundary, "l0room", 44);
            //});
            /////////////////


            //////////分组  
            var forGroupJson = ThDrainageSDToGJsonService.buildArchiGeometry(archiExtractor);
            forGroupJson.AddRange(dataSet.Region.BuildGeometries());
            forGroupJson.AddRange(dataSet.SupplyStart.BuildGeometries());
            forGroupJson.AddRange(ThDrainageSDToGJsonService.buildCoolPtGeometry(toiletList));
            var forGroupJsonString = ThGeoOutput.Output(forGroupJson);
            //string path = @"D:\project\2.drainage\jsonSample\2-1.input.geojson";
            //File.WriteAllText(path, forGroupJsonString);

            ThPipeSystemDiagramMgd SystemDiagramMethods = new ThPipeSystemDiagramMgd();
            var groupOutput = SystemDiagramMethods.ProcessGrouping(forGroupJsonString);
            //string path2 = @"D:\project\2.drainage\jsonSample\2-2.output.geojson";
            //File.WriteAllText(path2, groupOutput);

            //解析分组，更新点位信息
            var tGJList = ThDrainageSDDeserializeGJsonService.getGroupPt(groupOutput);
            var subLink = ThDrainageSDToGJsonService.updateToiletModel(tGJList, toiletList);
            allLink.AddRange(subLink);
            DrawUtils.ShowGeometry(subLink, "l05sub", 130);
            ////////////////////

            //找主线虚拟点位
            var groupList = ThDrainageSDCoolPtProcessService.classifyToilet(toiletList);
            ThDrainageSDCoolPtProcessService.classifySmallRoomGroup(ref groupList, roomList);

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
            forBranchJson.AddRange(dataSet.Region.BuildGeometries());
            forBranchJson.AddRange(dataSet.SupplyStart.BuildGeometries());
            forBranchJson.AddRange(ThDrainageSDToGJsonService.buildVirtualCoolPtGeomary(virtualPtList));
            forBranchJson.AddRange(ThDrainageSDToGJsonService.buildVirtualColumn(virtualColumn, dataSet.AreaID));

            var forBranchJsonString = ThGeoOutput.Output(forBranchJson);
            //string path3 = @"D:\project\2.drainage\jsonSample\2-3.input.geojson";
            //File.WriteAllText(path3, forBranchJsonString);

            var branchOutput = SystemDiagramMethods.ProcessMainBranchs(forBranchJsonString);
            //string path4 = @"D:\project\2.drainage\jsonSample\2-4.output.geojson";
            //File.WriteAllText(path4, branchOutput);

            var branchList = ThDrainageSDDeserializeGJsonService.getBranchLineList(branchOutput);
            allLink.AddRange(branchList);
            DrawUtils.ShowGeometry(branchList, "l05branch", 150);

            //连支干管
            var subBranchList = ThDrainageSDConnectSubService.linkGroupSub(groupList, ptForVirtualDict, islandPair, branchList);
            allLink.AddRange(subBranchList);
            DrawUtils.ShowGeometry(subBranchList, "l05subBranch", 140);

            //清理线和线头
            var lines = ThDrainageSDCleanLineService.simplifyLine(allLink);
            var ptOnWall = toiletList.SelectMany(x => x.SupplyCoolOnWall).ToList();
            ptOnWall.Add(supplyStart);
            ThDrainageSDCleanLineService.cleanNoUseLines(ptOnWall, ref lines);

            dataSet.roomList = roomList;
            dataSet.TerminalList = toiletList;
            dataSet.IslandPair = islandPair;
            dataSet.GroupList = groupList;
            dataSet.Pipes = lines;

            return lines;
        }
    }
}
#endif
