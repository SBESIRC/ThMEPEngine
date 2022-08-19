using Autodesk.AutoCAD.Geometry;

namespace ThMEPLighting.Garage.Model
{
    public class ElbowInput
    {
        /// <summary>
        /// 相交点
        /// </summary>
        public Point3d IntersectPoint { get; set; }

        /// <summary>
        /// 方向1
        /// </summary>
        public Vector3d FirstDirection { get; set; }

        /// <summary>
        /// 方向2
        /// </summary>
        public Vector3d SecondDirection { get; set; }
    }

    public class TeeInput
    {
        /// <summary>
        /// 相交点
        /// </summary>
        public Point3d IntersectPoint { get; set; }

        /// <summary>
        /// 主方向
        /// </summary>
        public Vector3d FirstDirection { get; set; }

        /// <summary>
        /// 方向2
        /// </summary>
        public Vector3d SecondDirection { get; set; }

        /// <summary>
        /// 方向3
        /// </summary>
        public Vector3d ThirdDirection { get; set; }
    }

    public class CrossInput
    {
        /// <summary>
        /// 相交点
        /// </summary>
        public Point3d IntersectPoint { get; set; }

        /// <summary>
        /// 主方向
        /// </summary>
        public Vector3d FirstDirection { get; set; }

        /// <summary>
        /// 方向2
        /// </summary>
        public Vector3d SecondDirection { get; set; }

        /// <summary>
        /// 方向3
        /// </summary>
        public Vector3d ThirdDirection { get; set; }

        /// <summary>
        /// 方向4
        /// </summary>
        public Vector3d ForthDirection { get; set; }
    }
}
