using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace ThMEPEngineCore.Model
{
    public class ThIfcSpatialStructureElement : ThIfcSpatialElement
    {
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<string> Tags { get; set; } = new List<string>();

        public Curve Boundary { get; set; }
    }
}
