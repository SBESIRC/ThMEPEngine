using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.AFAS.Data
{
    public class ThAFASDataUtils
    {
        public static ThAFASRoomExtractor CloneRoom(ThAFASRoomExtractor origRoomExtractor)
        {
            //房间元素后期会改，需要clone
            var roomExtractorClone = new ThAFASRoomExtractor();
            foreach (var room in origRoomExtractor.Rooms)
            {
                var boundary = room.Boundary.Clone() as Entity;
                var roomNew = ThIfcRoom.CreateWithTags(boundary, room.Tags);
                roomNew.Name = room.Name;
                roomExtractorClone.Rooms.Add(roomNew);
            }
            roomExtractorClone.Transformer = origRoomExtractor.Transformer;
            return roomExtractorClone;
        }
    }
}
