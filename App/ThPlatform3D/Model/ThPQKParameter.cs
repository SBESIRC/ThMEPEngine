using Autodesk.AutoCAD.Geometry;
using ThPlatform3D.Common;

namespace ThPlatform3D.Model
{
    public class ThPQKParameter
    {
        /// <summary>
        /// 剖切线起点
        /// </summary>
        public Point3d Start;
        /// <summary>
        /// 剖切线终点
        /// </summary>
        public Point3d End;
        /// <summary>
        /// 剖切框标注
        /// </summary>
        public string Mark { get; set; }
        /// <summary>
        /// 剖切深度
        /// </summary>
        public double Depth { get; set; }
        /// <summary>
        /// 剖切方向
        /// </summary>
        public ViewDirection Direction { get; set; }
        public Vector3d SectionDirection =>Direction.ToVector3d();       
        /// <summary>
        /// 文字距离剖切线的间距
        /// </summary>
        public double MarkInterval { get; set; } = 50;
        public string LineLayer { get; set; } = "TH-剖面视图位置";
        public string BlockLayer { get; set; } = "TH-剖面视图位置";
        public string MarkTextLayer { get; set; } = "TH-剖面视图位置";
        public string MarkTextStyle { get; set; } = "TH-STYLE3";
        public double MarkTextHeight { get; set; } = 300;
        public double MarkTextWidthFactor { get; set; } = 0.7;
        public string PQKBlockNamePrefix { get; set; } = "THBM_PQK_";
    }
}
