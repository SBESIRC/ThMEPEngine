using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcParkingStall : ThIfcRoom
    {
        public new static ThIfcParkingStall Create(Curve boundary)
        {
            return new ThIfcParkingStall()
            {
                Boundary = boundary,
            };
        }
    }
}
