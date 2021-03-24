using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcLane : ThIfcRoom
    {
        public new static ThIfcLane Create(Curve curve)
        {
            return new ThIfcLane()
            {
                Boundary = curve.Clone() as Curve,
            };
        }
    }
}
