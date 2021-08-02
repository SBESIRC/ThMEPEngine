using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThStairsRoom : ThIfcBuildingElement
    {
        public static ThStairsRoom Create(Entity data)
        {
            var stairsRoom = new ThStairsRoom();
            stairsRoom.Uuid = Guid.NewGuid().ToString();
            stairsRoom.Outline = data;
            return stairsRoom;
        }
    }
}
