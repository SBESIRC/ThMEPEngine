using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPHVAC.FanConnect.Model
{
    public class ThFanRoomModel : ThIfcBuildingElement
    {
        public static ThFanRoomModel Create(Entity data)
        {
            var buildRoom = new ThFanRoomModel();
            buildRoom.Uuid = Guid.NewGuid().ToString();
            buildRoom.Outline = data;
            return buildRoom;
        }
    }
}
