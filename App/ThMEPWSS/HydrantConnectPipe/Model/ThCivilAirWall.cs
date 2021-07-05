using Autodesk.AutoCAD.DatabaseServices;
using System;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThCivilAirWall : ThIfcBuildingElement
    {
        public Polyline ElementObb { get; set; }

        public static ThCivilAirWall Create(Entity data)
        {
            var civilAirWall = new ThCivilAirWall();
            civilAirWall.Uuid = Guid.NewGuid().ToString();
            civilAirWall.Outline = data;
            if(data is Line)
            {
                var line = data as Line;
                civilAirWall.ElementObb = line.Buffer(100);
            }

            return civilAirWall;
        }
    }
}
