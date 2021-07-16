using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AcHelper;
using NFox.Cad;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Colors;
using ThCADExtension;
using ThCADCore.NTS;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public class ThDrainageSDRoomService
    {
        public static List<ThToilateRoom> filtRoomList(List<ThToilateRoom> roomList)
        {

            var rooms = roomList.Where(x => x.toilate.Count > 0).ToList();

            return rooms;
        }

        //public static void shrinkRoom(List<ThExtractorBase> archiExtractor)
        //{
        //    var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToilateRoomExtractor)) as ThDrainageToilateRoomExtractor;

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

            var roomExtractor = ThDrainageSDCommonService.getExtruactor(archiExtractor, typeof(ThDrainageToilateRoomExtractor)) as ThDrainageToilateRoomExtractor;

            roomList.AddRange(roomExtractor.Rooms);

            return roomList;
        }

        public static List<ThToilateRoom> buildRoomModel(List<ThIfcRoom> roomList, List<ThTerminalToilate> toilateList)
        {
            List<ThToilateRoom> roomModelList = new List<ThToilateRoom>();
            roomList.ForEach(x =>
            {
                var roomOutline = x.Boundary as Polyline;
                var roomToilateList = new List<ThTerminalToilate>();
                foreach (var terminal in toilateList)
                {
                    var toilateOutline = terminal.Boundary;
                    if (roomOutline.Contains(toilateOutline) || roomOutline.Intersects(toilateOutline))
                    {
                        roomToilateList.Add(terminal);
                    }
                }

                var room = new ThToilateRoom(roomOutline, string.Join(";",x.Tags.ToArray()), roomToilateList);
                roomModelList.Add(room);
            });

            return roomModelList;
        }
    }
}
