using System.Collections.Generic;

using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.ChargerDistribution.Model
{
    public class ThParkingStallInfo
    {
        /// <summary>
        /// 车位外轮廓
        /// </summary>
        public Polyline Outline { get; set; }

        /// <summary>
        /// 车位散线
        /// </summary>
        public List<Line> Lines { get; set; }

        /// <summary>
        /// 中心
        /// </summary>
        public Point3d Centroid { get; set; }

        /// <summary>
        /// 周围车道线方向
        /// </summary>
        public List<Line> LaneLines { get; set; }

        /// <summary>
        /// 方向（由车头指向车尾）
        /// </summary>
        public Vector3d Direction { get; set; }

        /// <summary>
        /// 是否已被搜索
        /// </summary>
        public bool Searched { get; set; }

        /// <summary>
        /// 是否设置位置
        /// </summary>
        public bool SetValue { get; set; }

        /// <summary>
        /// 布置位置
        /// </summary>
        public Point3d LayOutPosition { get; set; }

        /// <summary>
        /// 块旋转角度
        /// </summary>
        public double Rotation { get; set; }

        public ThParkingStallInfo()
        {

        }
    }
}
