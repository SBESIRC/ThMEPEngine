using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Electrical
{
    public class ThFireCompartment : ThIfcSpatialZone
    {
        public string Number { get; set; }
        public new Entity Boundary { get; set; }
        public ThFireCompartment()
        {
            Number = "";
        }
    }
}
