using Autodesk.AutoCAD.Geometry;

namespace ThMEPIFC
{
    public class ThTGL2IFCCommon
    {
        /// <summary>
        /// 全局精度
        /// </summary>
        public Tolerance Global = new Tolerance(1e-3, 1e-5);
    }
}
