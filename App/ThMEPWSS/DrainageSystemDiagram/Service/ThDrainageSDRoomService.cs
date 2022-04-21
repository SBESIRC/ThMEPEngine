using System;
using System.Collections.Generic;
using System.Linq;

using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Model;

using ThMEPWSS.DrainageSystemDiagram.Service;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDRoomService
    {
        public static List<ThToiletRoom> filtRoomList(List<ThToiletRoom> roomList)
        {

            var rooms = roomList.Where(x => x.toilet.Count > 0).ToList();

            return rooms;
        }

        //public static void shrinkRoom(List<ThExtractorBase> archiExtractor)
        //{
        //    var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToiletRoomExtractor)) as ThDrainageToiletRoomExtractor;

        //    roomExtractor.Rooms.ForEach(room =>
        //    {
        //        var roomOutline = room.Boundary as Polyline;
        //        roomOutline = ThMEPFrameService.Buffer(roomOutline, -100);
        //        room.Boundary = roomOutline;
        //    });

        //}

        public static List<ThIfcRoom> getRoomList(List<ThExtractorBase> archiExtractor)
        {
            List<ThIfcRoom> roomList = new List<ThIfcRoom>();

            var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToiletRoomExtractor)) as ThDrainageToiletRoomExtractor;

            roomList.AddRange(roomExtractor.Rooms);

            return roomList;
        }

        public static List<ThToiletRoom> buildRoomModel(List<ThIfcRoom> roomList, List<ThTerminalToilet> toiletList)
        {
            List<ThToiletRoom> roomModelList = new List<ThToiletRoom>();
            roomList.ForEach(x =>
            {
                var roomOutline = x.Boundary as Polyline;
                var roomToiletList = new List<ThTerminalToilet>();
                foreach (var terminal in toiletList)
                {
                    var toiletOutline = terminal.Boundary;
                    if (roomOutline.Contains(toiletOutline) || roomOutline.Intersects(toiletOutline))
                    {
                        roomToiletList.Add(terminal);
                    }
                }

                var room = new ThToiletRoom(roomOutline, string.Join(";",x.Tags.ToArray()), roomToiletList);
                roomModelList.Add(room);
            });

            return roomModelList;
        }
    }
}
