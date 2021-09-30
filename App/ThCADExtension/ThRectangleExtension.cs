using Autodesk.AutoCAD.Geometry;
using AcRectangle = Autodesk.AutoCAD.DatabaseServices.Polyline;

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
        public static AcRectangle GetTransformedRectangle(this AcRectangle rectangle, Matrix3d matrix)
        {
            var solid = rectangle.ToSolid();
            solid.TransformBy(matrix);
            return solid.ToPolyline();
        }

        /// <summary>
        /// 投影到XY平面（Z=0）
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static AcRectangle FlattenRectangle(this AcRectangle rectangle)
        {
            Plane XYPlane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            Matrix3d matrix = Matrix3d.Projection(XYPlane, XYPlane.Normal);
            return GetTransformedRectangle(rectangle, matrix);
        }
    }
}
