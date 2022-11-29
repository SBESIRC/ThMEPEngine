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
        public static Polygon RemoveHoles(this Polygon polygon)
        {
            if(polygon.NumInteriorRings > 0)
            {
                polygon = new Polygon(polygon.Shell);
            }
            return polygon;
        } 
        public static List<Polygon> GetPolygons(this List<Polygon> polygons)
        {
            var lineStrings = new List<LineString>();
            polygons.ForEach(p => lineStrings.Add(p.Shell));
            return lineStrings.GetPolygons();
        }
        public static Coordinate GetCenter(this Polygon polygon)
        {
            var x = polygon.Coordinates.Average(coor => coor.X);
            var y = polygon.Coordinates.Average(coor => coor.Y);
            return new Coordinate(x, y);
        }
        public static List<LineSegment> ToLineSegments(this Polygon polygon)
        {
            var lineSegs = polygon.Shell.ToLineSegments();
            foreach(var hole in polygon.Holes)
            {
                lineSegs.AddRange(hole.ToLineSegments());
            }
            return lineSegs;

        }
    }
}
