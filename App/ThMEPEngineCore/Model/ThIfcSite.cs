using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcSite : ThIfcSpatialStructureElement
    {
        public Polyline Boundary { get; set; }
    }
}
