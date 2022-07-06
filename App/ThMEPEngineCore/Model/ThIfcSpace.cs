using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model
{
    public class ThIfcSpace : ThIfcSpatialStructureElement
    {
        public Curve Boundary { get; set; }
        public List<string> Tags { get; set; }
        public ThIfcSpace()
        {
            Tags = new List<string>();
            Uuid = Guid.NewGuid().ToString();
        }
    }
}
