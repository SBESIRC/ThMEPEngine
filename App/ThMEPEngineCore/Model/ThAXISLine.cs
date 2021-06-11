using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThAXISLine : ThIfcBuildingElement
    {
        public static ThAXISLine Create(Line line)
        {
            return new ThAXISLine()
            {
                Outline = line,
            };
        }
    }
}
