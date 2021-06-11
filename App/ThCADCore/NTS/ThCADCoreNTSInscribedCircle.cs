using NetTopologySuite.Geometries;
using NetTopologySuite.Algorithm.Construct;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSInscribedCircle
    {
        /// <summary>
        /// 获取几何学的最大内切圆圆心
        /// </summary>
        /// <param name="polygonal">a polygonal geometry. You can enter Polygon or MultiPolygon or other</param>
        /// <param name="tolerance">the distance tolerance for computing the center point</param>
        /// <returns></returns>
        public static Point3d GetMaximumInscribedCircleCenter(this Geometry polygonal, double tolerance = 1.0)
        {
            return MaximumInscribedCircle.GetCenter(polygonal, tolerance).ToAcGePoint3d();
        }

        /// <summary>
        /// 获取几何学的最大内切圆
        /// </summary>
        /// <param name="polygonal"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static Circle GetMaximumInscribedCircle(this Geometry polygonal, double tolerance = 1.0)
        {
            var calculator = new MaximumInscribedCircle(polygonal, tolerance);
            var center = calculator.GetCenter().ToAcGePoint3d();
            var radius = calculator.GetCenter().Distance(calculator.GetRadiusPoint());
            return new Circle(center, Vector3d.ZAxis, radius);
        }
    }
}
