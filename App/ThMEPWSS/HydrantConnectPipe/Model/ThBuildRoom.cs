using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThBuildRoom : ThIfcBuildingElement
    {
        public Polyline ElementObb { get; set; }
        public static ThBuildRoom Create(Entity data)
        {
            var buildRoom = new ThBuildRoom();
            buildRoom.Uuid = Guid.NewGuid().ToString();
            buildRoom.Outline = data;
            if (data is Polyline)
            {
                var polyline = data as Polyline;
                buildRoom.ElementObb = polyline;
            }

            return buildRoom;
        }
    }
}
