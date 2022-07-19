using Autodesk.AutoCAD.Geometry;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSInsertBlockInfo
    {
        /// <summary>
        /// 负载插入点
        /// </summary>
        public Point3d InsertPoint { get; set; }

        /// <summary>
        /// 标注插入点
        /// </summary>
        public Point3d FirstPoint { get; set; }

        /// <summary>
        /// 引线端点
        /// </summary>
        public Point3d SecondPoint { get; set; }

        /// <summary>
        /// 标注线方向
        /// </summary>
        public Point3d ThirdPoint { get; set; }

        /// <summary>
        /// 插入比例
        /// </summary>
        public Scale3d Scale { get; set; }
    }
}
