using Autodesk.AutoCAD.Geometry;

namespace ThMEPTCH.Model
{
    public class ThTCHTelecInterface
    {
        /// <summary>
        /// 接口点
        /// </summary>
        public Point3d Position { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public double Breadth { get; set; }

        /// <summary>
        /// 法向量
        /// </summary>
        public Vector3d Normal { get; set; }

        /// <summary>
        /// 方向
        /// </summary>
        public Vector3d Direction { get; set; }
    }
}
