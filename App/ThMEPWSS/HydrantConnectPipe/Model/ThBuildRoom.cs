using System;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThBuildRoom : ThIfcBuildingElement
    {
        public static ThBuildRoom Create(Entity data)
        {
            var buildRoom = new ThBuildRoom();
            buildRoom.Uuid = Guid.NewGuid().ToString();
            buildRoom.Outline = data;
            return buildRoom;
        }
    }
}
