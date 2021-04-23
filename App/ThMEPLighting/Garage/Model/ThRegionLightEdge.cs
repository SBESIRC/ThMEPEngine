using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Model
{
    public class ThRegionLightEdge
    {
        public Polyline RegionBorder { get; set; }
        public List<BlockReference> Lights { get; set; }
        /// <summary>
        /// Dwg中区域内已布灯的边
        /// </summary>
        public List<Line> Edges { get; set; }
        public List<DBText> Texts { get; set; }
        /// <summary>
        /// 车道线
        /// </summary>
        public List<Line> LaneLines { get; set; }
        /// <summary>
        /// 布灯返回的边
        /// </summary>
        public List<ThLightEdge> LightEdges { get; set; }
        /// <summary>
        /// 边界到原点的偏移
        /// </summary>
        public ThMEPOriginTransformer Transformer { get; set; }
        public ThRegionLightEdge()
        {
            RegionBorder = new Polyline();
            Lights = new List<BlockReference>();
            Edges = new List<Line>();
            Texts = new List<DBText>();
            LightEdges = new List<ThLightEdge>();
        }
    }
}
