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
using NetTopologySuite.Algorithm;

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

        //get Oriented Bounding Box(minimum bounding rectangle (MBR))
        public static Polygon GetObb(this Geometry geometry)
        {
            if (geometry == null) return null;
            var convexhull = geometry.ConvexHull();
            if (convexhull == null) return null;
            var points = convexhull.Coordinates.ToList();
            Envelope minBox = null;
            var minAngle = 0d;
            //foreach edge of the convex hull
            for (var i = 0; i < points.Count; i++)
            {
                var nextIndex = i + 1;
                var current = points[i];
                var next = points[nextIndex % points.Count];
                //min / max points
                var top = double.MinValue;
                var bottom = double.MaxValue;
                var left = double.MaxValue;
                var right = double.MinValue;
                //get angle of segment to x axis

                double angle;
                if(current.X == next.X)
                {
                    angle = -AngleUtility.PiOver2;
                }
                else angle = -Math.Atan(Math.Abs(current.Y - next.Y) / Math.Abs(current.X-next.X));
                //var angle = AngleUtility.Angle(current, next);
                //rotate every point and get min and max values for each direction
                foreach (var p in points)
                {
                    var rotatedPoint =p.RotateToXAxis(angle);

                    top = Math.Max(top, rotatedPoint.Y);
                    bottom = Math.Min(bottom, rotatedPoint.Y);

                    left = Math.Min(left, rotatedPoint.X);
                    right = Math.Max(right, rotatedPoint.X);
                }
                //create axis aligned bounding box
                var box = new Envelope(left, right, top, bottom);
                if (minBox == null || minBox.Area > box.Area)
                {
                    minBox = box;
                    minAngle = angle;
                }
            }
            //rotate axis algined box back
            var bottomleft = new Coordinate(minBox.MinX, minBox.MinY).RotateToXAxis(-minAngle);
            var topleft = new Coordinate(minBox.MinX, minBox.MaxY).RotateToXAxis(-minAngle);
            var topright = new Coordinate(minBox.MaxX, minBox.MaxY).RotateToXAxis(-minAngle);
            var bottomright = new Coordinate(minBox.MaxX, minBox.MinY).RotateToXAxis(-minAngle);
            var ring = new Coordinate[] { bottomleft, topleft, topright, bottomright, bottomleft };
            return new Polygon(new LinearRing(ring) );
        }
    }
}