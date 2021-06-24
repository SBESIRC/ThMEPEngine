#if ACAD2016
using AcHelper;
using System.IO;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.GeojsonExtractor;
using CLI;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThConnectToilateEngine
    {
        public static void ThConnectEngine(List<ThExtractorBase> archiExtractor, List<ThIfcSanitaryTerminalToilate> toilateList)
        {
            var roomPolyList = ThDrainageSDRoomService.getRoomList(archiExtractor);
            var roomList = ThDrainageSDRoomService.buildRoomModel(roomPolyList, toilateList);
            var filteredRoom = ThDrainageSDRoomService.filtRoomList(roomList);

            if (filteredRoom == null || toilateList == null || filteredRoom.Count == 0 || toilateList.Count == 0)
            {
                return;
            }

            var supplyStartExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageSDColdWaterSupplyStartExtractor)) as ThDrainageSDColdWaterSupplyStartExtractor;
            var supplyStart = (supplyStartExtractor.ColdWaterSupplyStarts[0].Geometry as DBPoint).Position;

            var areaId = ThDrainageSDToGJsonService.getAreaId(archiExtractor);
            toilateList.ForEach(x => x.AreaId = areaId);

            //所有空间建model 包括没有厕所的空间（后续建图需要）


            //确定每个厕所在墙上的给水点位
            ThDrainageSDFindColdPtService.findCoolSupplyPt(roomList, toilateList, out var aloneToilate);
            toilateList.ForEach(x => x.SupplyCoolOnWall.ForEach(pt => DrawUtils.ShowGeometry(pt, "l0SupplyOnWall", 50, 35, 20, "C")));

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
            forGroupJson.AddRange(ThDrainageSDToGJsonService.buildColdPtGeometry(toilateList));
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
            DrawUtils.ShowGeometry(subLink, "l5sub", 151, 30);
            ////////////////////

            //找主线虚拟点位
            var groupList = ThDrainageSDColdPtProcessService.classifyToilate(toilateList);


            var virtualPtList = ThDrainageSDGroupColdSupplyPtEngine.getVirtualPtOfGroup(supplyStart, groupList, roomList,out var ptForVirtualDict); 
            //debug drawing
            virtualPtList.ForEach(pt =>
            {
                DrawUtils.ShowGeometry(pt.Pt, "l4virtualPt", 220, 30, 40, "C");
            });

            //连支干管
            var subBranchList = ThDrainageSDConnectSubService.linkGroupSub(groupList, ptForVirtualDict);
            DrawUtils.ShowGeometry(subBranchList, "l5subBranch", 140, 35);

            var bDebug = false;
            if (bDebug == true)
            {
                return;
            }

            //////找主线
            var forBranchJson = ThDrainageSDToGJsonService.buildArchiGeometry(archiExtractor);
            forBranchJson.AddRange(ThDrainageSDToGJsonService.buildVirtualColdPtGeomary(virtualPtList));

            var forBranchJsonString = ThGeoOutput.Output(forBranchJson);
            string path3 = @"D:\project\2.drainage\jsonSample\2-3.input.geojson";
            File.WriteAllText(path3, forBranchJsonString);

            var branchOutput = SystemDiagramMethods.ProcessMainBranchs(forBranchJsonString);
            string path4 = @"D:\project\2.drainage\jsonSample\2-4.output.geojson";
            File.WriteAllText(path4, branchOutput);

            var branchList = ThDrainageSDDeserializeGJsonService.getBranchLineList(branchOutput);

            DrawUtils.ShowGeometry(branchList, "l5branch", 150, 35);

            
        }
    }
}
#endif
