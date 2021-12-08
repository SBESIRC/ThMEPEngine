using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSHatchExtension
    {
        private static Geometry ToNTSGeometry(this Hatch hatch)
        {
            return Simplify(hatch.Boundaries().ToCollection()).BuildAreaGeometry();
        }

        private static DBObjectCollection Simplify(DBObjectCollection boundaries)
        {
            var objs = new DBObjectCollection();
            boundaries.OfType<Polyline>().ForEach(o =>
            {
                // 假定边界都是封闭的
                o.Closed = true;
                // 剔除重复点
                objs.Add(o.DPSimplify(1.0));
            });
            return objs;
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
        public static List<Polygon> ToPolygons(this Hatch hatch)
        {
            var objs = new List<Polygon>();
            hatch.ToNTSMultiPolygon().Geometries
                .Cast<Polygon>()
                .ForEach(o => objs.Add(o));
            if(objs.Count==0)
            {

            }
            return objs;
        }
    }
}
