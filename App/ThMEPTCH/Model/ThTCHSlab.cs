using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Model;

namespace ThMEPTCH.Model
{
    public class ThTCHSlab: ThIfcSlab
    {
        /// <summary>
        /// 降板高度
        /// </summary>
        public double DescendingHeight { get; set; }

        /// <summary>
        /// 降板厚度
        /// </summary>
        public double DescendingThickness { get; set; }
        
        /// <summary>
        /// 降板包围厚度
        /// </summary>
        public double DescendingWrapThickness { get; set; }

        /// <summary>
        /// 拉伸方向
        /// </summary>
        public Vector3d ExtrudedDirection { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="thickness"></param>
        /// <param name="extVector"></param>
        public ThTCHSlab(MPolygon polygon, double thickness, Vector3d extVector)
        {
            Outline = polygon;
            Thickness = thickness;
            ExtrudedDirection = extVector;
        }
    }
}
