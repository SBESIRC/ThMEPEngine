using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThRectangleExtension
    {
        /// <summary>
        /// 矩形变换（支持non-uniform scale）
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Polyline GetTransformedRectangle(this Polyline rectangle, Matrix3d matrix)
        {
            var solid = rectangle.ToSolid();
            solid.TransformBy(matrix);
            return solid.ToPolyline();
        }
    }
}
