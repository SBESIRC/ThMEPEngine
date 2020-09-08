using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThMEPEngineCore.BeamInfo.Model
{
    public class ThBeamZone
    { 
        public AcPolygon Region { get; private set; }
        public ThBeamZone(AcPolygon polygon)
        {
            //
        }
    }
}
