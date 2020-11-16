using System;
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
            var polygons = new List<Polygon>();
            hatch.Boundaries().ForEach(o => polygons.Add(o.ToNTSPolygon()));
            ThCADCoreNTSBuildArea buildArea = new ThCADCoreNTSBuildArea();
            return buildArea.Build(ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon(polygons.ToArray()));
        }

        private static MultiPolygon ToNTSMultiPolygon(this Hatch hatch)
        {
            var geometry = hatch.ToNTSGeometry();
            if (geometry is GeometryCollection collection)
            {
                return ThCADCoreNTSService.Instance.GeometryFactory.CreateMultiPolygon();
            }
            else if (geometry is Polygon polygon)
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

        public static DBObjectCollection ToMPolygons(this Hatch hatch)
        {
            var objs = new DBObjectCollection();
            hatch.ToNTSMultiPolygon().Geometries
                .Cast<Polygon>()
                .ForEach(o => objs.Add(o.ToMPolygon()));
            return objs;
        }
    }
}
