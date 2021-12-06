using System;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Common
{
    public class ThLightNode
    {
        public string Id { get; set; }
        public string Number { get; set; }
        public Point3d Position { get; set; }
        /// <summary>
        /// 灯接入线的根数
        /// </summary>
        public short WireNum { get; set; }
        public ThLightNode()
        {
            Number = "";
            Id = Guid.NewGuid().ToString();
        }
        public int GetIndex()
        {
            return Number.GetNumberIndex();
        }
        /// <summary>
        /// 是否可以继续连接
        /// </summary>
        public bool CanableLink
        {
            get
            {
                return WireNum <= 4;
            }
        }
        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrEmpty(Number);
            }
        }
    }
}
