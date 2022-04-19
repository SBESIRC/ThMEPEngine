using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Polygonize;

namespace ThParkingStall.Core.Tools
{
    public static class GeometryEx
    {
        public static IEnumerable<Polygon> Polygonize(this Geometry geo)
        {
            var polygonizer = new Polygonizer();
            polygonizer.Add(geo);
            return polygonizer.GetPolygons().Cast<Polygon>();
        }
    }
}
