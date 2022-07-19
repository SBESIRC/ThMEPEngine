using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Geometries.Utilities;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSHatchExtension
    {
        private const double OFFSET_DISTANCE = 20.0;
        private const double DISTANCE_TOLERANCE = 1.0;

        /// <summary>
        /// 填充的轮廓（支持洞）
        /// </summary>
        /// <param name="hatch"></param>
        /// <returns></returns>
        public static DBObjectCollection BoundariesEx(this Hatch hatch)
        {
            // 存在一种特殊的填充，即带洞的填充，
            // 它的Shell和Holes是用一个多段线描述
            // 即在多段线中，Shell和Holes是连接的
            var objs = hatch.Boundaries().ToCollection();
            return GeometryFixer.Fix(objs.ToNTSMultiPolygon()).ToDbCollection(true);
        }

        [Obsolete("该方法已被弃用，请使用BoundariesEx代替")]
        public static List<Polygon> ToPolygons(this Hatch hatch)
        {
            var objs = new List<Polygon>();
            hatch.ToNTSMultiPolygon().Geometries
                .Cast<Polygon>()
                .ForEach(o => objs.Add(o));
            return objs;
        }

        private static Geometry ToNTSGeometry(this Hatch hatch)
        {
            var loops = hatch.Boundaries().ToCollection();
            return Simplify(Normalize(loops)).BuildAreaGeometry();
        }

        private static DBObjectCollection Simplify(DBObjectCollection loops)
        {
            var objs = new DBObjectCollection();
            loops.OfType<AcPolygon>().ForEach(l =>
            {
                l = l.DPSimplify(DISTANCE_TOLERANCE);
                objs.Add(l);
            });
            return objs;
        }

        private static DBObjectCollection Normalize(DBObjectCollection loops)
        {
            var objs = new DBObjectCollection();
            // NTS Buffer对于非常远的坐标（WCS下，>10E10)处理的不好
            // Workaround就是将位于非常远的图元临时移动到WCS原点附近，参与运算
            // 运算结束后将运算结果再按相同的偏移从WCS原点附近移动到其原始位置
            var transform = GetToNearOriginMatrix(loops);
            loops.OfType<AcPolygon>().ForEach(l => l.TransformBy(transform));
            loops.OfType<AcPolygon>().ForEach(l =>
            {
                var loop = RemoveSpikes(l);
                if (loop != null)
                {
                    objs.Add(loop);
                }
            });
            // 将结果恢复到原始位置
            var inverse = transform.Inverse();
            objs.OfType<AcPolygon>().ForEach(l => l.TransformBy(inverse));
            return objs;
        }

        private static AcPolygon RemoveSpikes(AcPolygon polygon)
        {
            var objs = polygon.Buffer(-OFFSET_DISTANCE);
            if (objs.Count == 0)
            {
                return null;
            }
            var offset = objs.OfType<AcPolygon>().OrderByDescending(o => o.Area).First();
            objs = offset.Buffer(OFFSET_DISTANCE);
            if (objs.Count == 0)
            {
                return null;
            }
            return objs.OfType<AcPolygon>().OrderByDescending(o => o.Area).First();
        }

        private static Matrix3d GetToNearOriginMatrix(DBObjectCollection loops)
        {
            var center = loops.GeometricExtents().CenterPoint();
            var vector = center.GetVectorTo(Point3d.Origin);
            return Matrix3d.Displacement(vector);
        }

        private static MultiPolygon ToNTSMultiPolygon(this Hatch hatch)
        {
            var geometry = hatch.ToNTSGeometry();
            if (geometry.IsEmpty)
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon();
            }
            if (geometry is Polygon polygon)
            {
                var polygons = new Polygon[] { polygon };
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons);
            }
            else if (geometry is MultiPolygon multiPolygon)
            {
                return multiPolygon;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
