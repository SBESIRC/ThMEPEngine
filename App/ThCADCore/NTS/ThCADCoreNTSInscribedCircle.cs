using System;
using NetTopologySuite.Simplify;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm.Construct;
using Autodesk.AutoCAD.Geometry;

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
        public static Point3d GetCenterMaximumInscribedCircle(this Geometry polygonal, double tolerance = 1.0)
        {
            return MaximumInscribedCircle.GetCenter(polygonal, tolerance).ToAcGePoint3d();
        }

        public static Polyline GetRadiusLine(this Geometry polygonal, double tolerance)
        {
            return MaximumInscribedCircle.GetRadiusLine(polygonal, tolerance).ToDbPolyline();
        }
    }
}
