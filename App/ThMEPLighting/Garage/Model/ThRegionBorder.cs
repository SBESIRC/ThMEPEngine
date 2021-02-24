using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPLighting.Common;

namespace ThMEPLighting.Garage.Model
{
    public class ThRegionBorder
    {
        /// <summary>
        /// 布灯的边界
        /// </summary>
        public Polyline RegionBorder { get; set; }
        /// <summary>
        /// 布灯线槽中心线
        /// </summary>
        public List<Line> DxCenterLines { get; set; }
        /// <summary>
        /// 非布灯线槽中心线
        /// </summary>
        public List<Line> FdxCenterLines { get; set; }
        /// <summary>
        /// 布灯的边
        /// </summary>
        public List<ThLightEdge> LightEdges { get; set; }
        /// <summary>
        /// 线槽的边
        /// </summary>
        public List<Line> CableTraySides { get; set; }
        /// <summary>
        /// 线槽中心线
        /// </summary>
        public List<Line> CableTrayCenters { get; set; }
        /// <summary>
        /// 线槽中心线和两边线配对
        /// </summary>
        public Dictionary<Line, List<Line>> CableTrayGroups { get; set; }
        /// <summary>
        /// 线槽中心线和端口线配对
        /// </summary>
        public Dictionary<Line, List<Line>> CableTrayPorts { get; set; }
        /// <summary>
        /// 车道灯
        /// </summary>
        public List<BlockReference> Lights { get; set; }
        /// <summary>
        /// 边界到原点的偏移
        /// </summary>
        public ThMEPOriginTransformer Transformer { get; set; }
        public ThRegionBorder()
        {
            DxCenterLines = new List<Line>();
            FdxCenterLines = new List<Line>();
            CableTraySides = new List<Line>();
            CableTrayCenters = new List<Line>();
            LightEdges = new List<ThLightEdge>();
            CableTrayGroups = new Dictionary<Line, List<Line>>();
            CableTrayPorts = new Dictionary<Line, List<Line>>();
        }
    }
}
