using ThMEPLighting.Common;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Model
{
    public class ThLightNodeLink
    {
        public ThLightNode First { get; set; }
        public ThLightNode Second { get; set; }
        /// <summary>
        /// 记录两个灯节点之间的路径
        /// </summary>
        public List<Line> Edges { get; set; }
        /// <summary>
        /// 表示两张灯在同一直段上
        /// 两线外角小于45度表示一个直段
        /// </summary>
        public bool OnLinkPath { get; set; }
        /// <summary>
        /// 从First到Second的跳线
        /// </summary>
        public List<Curve> JumpWires { get; set; }
        public bool IsCrossLink { get; set; }
        public ThLightNodeLink()
        {
            Edges = new List<Line>();
            JumpWires = new List<Curve>();
        }
        public bool IsSameLink(ThLightNodeLink other)
        {
            if (this.First.Id == other.First.Id && this.Second.Id == other.Second.Id)
            {
                return true;
            }

            if (this.First.Id == other.Second.Id && this.Second.Id == other.First.Id)
            {
                return true;
            }
            return false;
        }
    }
}
