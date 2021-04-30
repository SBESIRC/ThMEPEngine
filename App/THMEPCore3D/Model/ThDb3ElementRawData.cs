using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Model
{
    public class ThDb3ElementRawData
    {
        public Dictionary<string,string> Data { get; set; }
        public Entity Geometry { get; set; }
        public ThDb3ElementRawData()
        {
            Data = new Dictionary<string, string>();
        }
    }
}
