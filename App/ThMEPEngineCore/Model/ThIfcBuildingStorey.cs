using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcBuildingStorey : ThIfcSpatialStructureElement
    {
        public Polyline Boundary { get; set; }
    }
}
