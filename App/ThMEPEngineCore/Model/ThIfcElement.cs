using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public abstract class ThIfcElement : ThIfcProduct
    {
        public Entity Outline { get; set; }
        public string Uuid { get; set; }        
    }
}
