using System.Collections.Generic;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO.SVG;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThArchSvgInput
    {
        public List<ThGeometry> Geos { get; set; }
        public List<ThFloorInfo> FloorInfos { get; set; }
        public List<ThComponentInfo> ComponentInfos { get; set; }
        public Dictionary<string, string> DocProperties { get; set; }
        public ThArchSvgInput()
        {
            Geos = new List<ThGeometry>();
            FloorInfos = new List<ThFloorInfo>();
            ComponentInfos = new List<ThComponentInfo>();
            DocProperties = new Dictionary<string, string>();   
        }
    }
}
