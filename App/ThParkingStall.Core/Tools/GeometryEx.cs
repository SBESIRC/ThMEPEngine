using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Operation.Polygonize;
using NetTopologySuite.Triangulate;
using NetTopologySuite.Operation.Buffer;

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
        public static Polygon GetEnvelope(this IEnumerable<Geometry> geos)
        {
            var Envelope = new GeometryCollection(geos.ToArray()).Envelope;
            if (Envelope is Polygon polygon) return polygon;
            else return null;
        }
        public static double GetBound(this List<Polygon> polygons, bool UpperBound, bool Verticle)
        {
            return GetBound(polygons.Cast<Geometry>().ToList(), UpperBound, Verticle);
        }
        public static double GetBound(this List<Geometry> geos, bool UpperBound, bool Verticle)
        {
            var coors = new List<Coordinate>();
            geos.ForEach(geo => coors.AddRange(geo.Coordinates));
            if (Verticle)
            {
                if (UpperBound) return coors.Max(coor => coor.Y);
                else return coors.Min(coor => coor.Y);
            }
            else
            {
                if (UpperBound) return coors.Max(coor => coor.X);
                else return coors.Min(coor => coor.X);
            }
        }
        //removeHoles Only active when T is polygon 
        public static List<T> Get<T>(this Geometry geometry, bool removeHoles = true)
        {
            var objs = new List<T>();
            if (typeof(T) == null) return objs;
            var typeToGet = typeof(T);
            var geoType = typeof(Geometry);
            if (!(typeToGet.IsSubclassOf(geoType) || typeToGet == geoType)) throw new NotSupportedException();
            if (geometry.IsEmpty)
            {
                return objs;
            }
            if (geometry is T t)
            {
                if (t is Polygon polygon && removeHoles) objs.Add((T)Convert.ChangeType(polygon.RemoveHoles(), typeToGet));
                else objs.Add(t);
            }
            else if (geometry is MultiLineString lineStrings)
            {
                foreach (var geo in lineStrings.Geometries) objs.AddRange(geo.Get<T>(removeHoles));
            }
            else if (geometry is MultiPolygon polygons)
            {
                foreach (var geo in polygons.Geometries) objs.AddRange(geo.Get<T>(removeHoles));
            }
            else if (geometry is GeometryCollection geometries)
            {
                foreach (var geo in geometries.Geometries) objs.AddRange(geo.Get<T>(removeHoles));
            }
            else if (geometry is MultiPoint points)
            {
                foreach (var geo in points.Geometries) objs.AddRange(geo.Get<T>(removeHoles));
            }
            return objs;
        }

        public static List<Geometry> ToBasic(this Geometry geometry, bool removeHoles = true)
        {
            var objs = new List<Geometry>();
            objs.AddRange(geometry.Get<Polygon>(removeHoles));
            objs.AddRange(geometry.Get<LineString>(removeHoles));
            objs.AddRange(geometry.Get<Point>(removeHoles));
            return objs;
        }
    }
}