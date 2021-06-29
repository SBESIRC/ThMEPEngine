using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThCornice : ThIfcBuildingElement
    {
        public static ThCornice Create(Curve curve)
        {
            return new ThCornice()
            {
                Outline = curve,
            };
        }
    }
}
