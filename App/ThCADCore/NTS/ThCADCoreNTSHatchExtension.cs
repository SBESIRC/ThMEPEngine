using System;
using NFox.Cad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using AcPolygon = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSHatchExtension
    {
        private const double OFFSET_DISTANCE = 20.0;
        private const double DISTANCE_TOLERANCE = 1.0;

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

        public static DBObjectCollection Normalize(DBObjectCollection loops)
        {
            var objs = new DBObjectCollection();
            loops.OfType<AcPolygon>().ForEach(l =>
            {
                l = l.Buffer(-OFFSET_DISTANCE).OfType<AcPolygon>().OrderByDescending(o => o.Area).First();
                l = l.Buffer(OFFSET_DISTANCE).OfType<AcPolygon>().OrderByDescending(o => o.Area).First();
                objs.Add(l);
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
