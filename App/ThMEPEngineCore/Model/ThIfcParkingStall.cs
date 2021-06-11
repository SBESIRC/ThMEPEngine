using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcParkingStall : ThIfcSpatialElement
    {
        public Curve Boundary { get; set; }

        public static ThIfcParkingStall Create(Curve boundary)
        {
            return new ThIfcParkingStall()
            {
                Boundary = boundary,
            };
        }
    }
}
