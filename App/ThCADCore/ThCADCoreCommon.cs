using Autodesk.AutoCAD.Geometry;

namespace ThCADCore
{
    public class ThCADCoreCommon
    {
        /// <summary>
        /// 应用于方案的全局公差
        /// </summary>
        /// CAD默认全局公差equalVector为1E-12，equalPoint为1E-10
        /// 自定义全局公差equalVector为1E-6，equalPoint为1E-4
        public static readonly Tolerance global_tolerance_architecture = new Tolerance(1E-6, 1E-4);
    }
}
