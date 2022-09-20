using System.Collections.Generic;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO.SVG
{
    public class ThSvgParseInfo
    {
        /// <summary>
        /// 单体名称
        /// </summary>
        public string BuildingName { get; set; }
        public List<ThGeometry> Geos { get; set; }
        public List<ThFloorInfo> FloorInfos { get; set; }
        public List<ThComponentInfo> ComponentInfos { get; set; }
        public Dictionary<string, string> DocProperties { get; set; }
        public ThSvgParseInfo()
        {
            BuildingName = "";
            Geos = new List<ThGeometry>();
            FloorInfos = new List<ThFloorInfo>();
            ComponentInfos = new List<ThComponentInfo>();
            DocProperties = new Dictionary<string, string>();   
        }
    }
}
