using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThAXISLine : ThIfcBuildingElement
    {
        public static ThAXISLine Create(Curve curve)
        {
            return new ThAXISLine()
            {
                Outline = curve,
            };
        }
    }
}
