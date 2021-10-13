using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSPolylineExtension
    {
        public static Circle MinimumBoundingCircle(this Polyline polyline)
        {
            var mbc = new MinimumBoundingCircle(polyline.ToNTSLineString());
            return new Circle(mbc.GetCentre().ToAcGePoint3d(), Vector3d.ZAxis, mbc.GetRadius());
        }

        public static Polyline GetMinimumRectangle(this Polyline polyline)
        {
            // GetMinimumRectangle()对于非常远的坐标（WCS下，>10E10)处理的不好
            // Workaround就是将位于非常远的图元临时移动到WCS原点附近，参与运算
            // 运算结束后将运算结果再按相同的偏移从WCS原点附近移动到其原始位置
            var geom = polyline.ToNTSLineString();
            var rectangle = MinimumDiameter.GetMinimumRectangle(geom);
            if (rectangle is Polygon polygon)
            {
                return polygon.Shell.ToDbPolyline();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
