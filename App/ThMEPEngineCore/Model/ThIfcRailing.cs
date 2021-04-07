using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcRailing : ThIfcBuildingElement
    {
        public static ThIfcRailing Create(Curve curve)
        {
            return new ThIfcRailing()
            {
                Outline = curve,
            };
        }
    }
}
