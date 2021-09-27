using System;
using NFox.Cad;
using System.Linq;
using Dreambuild.AutoCAD;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public class ThCADCoreNTSGeometryFilter
    {
        public static DBObjectCollection GeometryEquality(DBObjectCollection dbObjs)
        {
            var geometries = new Dictionary<DBObject, Geometry>();
            dbObjs.OfType<DBObject>().ForEach(o =>
            {
                if (!geometries.ContainsKey(o))
                {
                    geometries[o] = ToNTSGeometry(o);
                }
            });

            // 利用Lookup将"几何相等"的图元组在一起
            // "几何相等"又利用了NTS的Geometry Equality能力
            // NTS的Geometry.Equals(Geometry)是“Topological Equality”
            // https://coding.abel.nu/2014/09/net-and-equals/
            // https://locationtech.github.io/jts/javadoc/index.html
            var lookup = (Lookup<Geometry, DBObject>)geometries.ToLookup(p => p.Value, p => p.Key);
            return lookup.Select(o => o.First()).ToCollection();
        }

        private static Geometry ToNTSGeometry(DBObject obj)
        {
            // 利用NTS固定精度去除小数点容差
            using (var ov = new ThCADCoreNTSFixedPrecision())
            {
                if (obj is DBPoint dbPoint)
                {
                    return dbPoint.ToNTSPoint();
                }
                else if (obj is Line line)
                {
                    return line.ToNTSLineString();
                }
                else if (obj is Polyline polyline)
                {
                    return polyline.ToNTSLineString();
                }
                else if (obj is Arc arc)
                {
                    return arc.ToNTSGeometry();
                }
                else if (obj is Circle circle)
                {
                    return circle.ToNTSGeometry();
                }
                else if (obj is MPolygon mPolygon)
                {
                    return mPolygon.ToNTSPolygon();
                }
                else if (obj is Entity entity)
                {
                    try
                    {
                        return entity.GeometricExtents.ToNTSPolygon();
                    }
                    catch
                    {
                        // 若异常抛出，则返回一个“空”的Polygon
                        return ThCADCoreNTSService.Instance.GeometryFactory.CreatePolygon();
                    }
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
