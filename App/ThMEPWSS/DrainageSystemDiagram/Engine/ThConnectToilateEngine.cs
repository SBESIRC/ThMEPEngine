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
            var roomList = getRoomList(archiExtractor);

            if (roomList == null || toilateList == null || roomList.Count == 0 || toilateList.Count == 0)
            {
                return;
            }
            var filteredRoom = filtRoomList(roomList, toilateList);

            if (filteredRoom == null || toilateList == null || filteredRoom.Count == 0 || toilateList.Count == 0)
            {
                return;
            }

            var ptList = ThDrainageSDCoolSupplyPt.findCoolSupplyPt(filteredRoom, toilateList);
            ptList.ForEach(x => DrawUtils.ShowGeometry(x.Value, "l0SupplyOnWall", 50, 35, 20, "C"));

            //////////分组
            var forGroupJson = ThDrainageSDExchangeThGeom.buildGeometry(archiExtractor, toilateList, false);
            var forGroupJsonString = ThGeoOutput.Output(forGroupJson);
            string path = @"D:\project\2.drainage\jsonSample\2-1.input.geojson";
            File.WriteAllText(path, forGroupJsonString);

            ThPipeSystemDiagramMgd SystemDiagramMethods = new ThPipeSystemDiagramMgd();
            var groupOutput = SystemDiagramMethods.ProcessGrouping(forGroupJsonString);
            string path2 = @"D:\project\2.drainage\jsonSample\2-2.output.geojson";
            File.WriteAllText(path2, groupOutput);
            ///////////

            //解析分组，更新点位信息
            var tGJList = ThDrainageSDDeserializeGJson.getGroupPt(groupOutput);
            var subLink = ThDrainageSDExchangeThGeom.updateToilateModel(tGJList, toilateList);
            DrawUtils.ShowGeometry(subLink, "l0subLink", 231, 30);

            //////找主线
            var forBranchJson = ThDrainageSDExchangeThGeom.buildGeometry(archiExtractor, toilateList, true);
            var forBranchJsonString = ThGeoOutput.Output(forBranchJson);
            string path3 = @"D:\project\2.drainage\jsonSample\2-3.input.geojson";
            File.WriteAllText(path3, forBranchJsonString);

            var branchOutput = SystemDiagramMethods.ProcessMainBranchs(forBranchJsonString);
            string path4 = @"D:\project\2.drainage\jsonSample\2-4.output.geojson";
            File.WriteAllText(path4, branchOutput);

            var branchList = ThDrainageSDDeserializeGJson.getBranchLineList(branchOutput);

            DrawUtils.ShowGeometry(branchList, "l0branchList", 230, 35);
        }


        private static List<Polyline> getRoomList(List<ThExtractorBase> archiExtractor)

        {
            List<Polyline> roomList = new List<Polyline>();

            foreach (var extractor in archiExtractor)
            {
                if (extractor is ThDrainageToilateRoomExtractor roomExtractor)
                {
                    roomExtractor.Rooms.ForEach(x =>
                    {
                        roomList.Add(x.Boundary as Polyline);
                    });
                }
            }

            return roomList;
        }


        private static List<Polyline> filtRoomList(List<Polyline> roomList, List<ThIfcSanitaryTerminalToilate> toilateList)
        {

            var hasTerminalRoom = ThDrainageSDCoolSupplyPt.hasTerminalRoom(roomList, toilateList);
            //var filteredRoom = hasTerminalRoom.Where(x => x.Area > DrainageSDCommon.TolSmallArea).ToList();

            return hasTerminalRoom;
        }

    }
}
#endif