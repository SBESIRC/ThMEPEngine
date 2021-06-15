using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcLineFoot : ThIfcBuildingElement
    {
        public static ThIfcLineFoot Create(Curve curve)
        {
            return new ThIfcLineFoot()
            {
                Outline = curve,
            };
        }
    }
}
