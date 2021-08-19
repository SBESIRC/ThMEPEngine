using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcCornice : ThIfcBuildingElement
    {
        public static ThIfcCornice Create(Curve curve)
        {
            return new ThIfcCornice()
            {
                Outline = curve,
            };
        }
    }
}
