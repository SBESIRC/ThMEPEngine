using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
namespace ThParkingStall.Core.Tools
{
    public static class PolygonEx
    {
        public static void RemoveHoles(this Polygon polygon)
        {
            if(polygon.NumInteriorRings > 0)
            {
                polygon = new Polygon(polygon.Shell);
            }
        } 
        public static List<Polygon> GetPolygons(this List<Polygon> polygons)
        {
            var lineStrings = new List<LineString>();
            polygons.ForEach(p => lineStrings.Add(p.Shell));
            return lineStrings.GetPolygons();
        }
    }
}
