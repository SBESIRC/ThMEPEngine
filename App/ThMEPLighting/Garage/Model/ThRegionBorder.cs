using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

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

        public List<ThLightNode> GetLightNodes()
        {
            var results = new List<ThLightNode>();
            return results;
        }
        public void EraseNumbers()
        {
        }
    }
}
