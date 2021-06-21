using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThCivilAirWall : ThIfcBuildingElement
    {
        public Polyline ElementObb { get; set; }
    }
}
