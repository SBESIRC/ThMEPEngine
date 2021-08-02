using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcSlab : ThIfcBuildingElement
    {
        public double Thickness { get; set; }
        public static ThIfcSlab Create(Curve curve)
        {
            return new ThIfcSlab()
            {
                Outline = curve,
            };
        }
    }
}
