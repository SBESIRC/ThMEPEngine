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
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public static Geometry MitreBuffer(this Geometry geo, double distance, bool removeHoles = true,double Obt_tol = 0.1)
        {
            var inner = distance < 0;
            var basics = geo.ToBasic(removeHoles);
            var gpolys = new List<Polygon>();
            foreach(var basic in basics)
            {
                if(basic == null) continue;
                if (basic is Polygon polygon)
                {
                    gpolys.AddRange(polygon.Obtusify(inner,Obt_tol).Buffer(distance, MitreParam).Get<Polygon>(removeHoles));
                }
                    
                else
                {
                    gpolys.AddRange(basic.Buffer(distance, MitreParam).Get<Polygon>(removeHoles));
                }
            }
            var result = new MultiPolygon(gpolys.ToArray()).Union().Get<Polygon>(removeHoles);
            return new MultiPolygon(result.ToArray());
        }
        public static Geometry GetVoronoiDiagram(this Geometry geo,double distance = 1000)
        {
            var coords = new List<Coordinate>();
            //coords.AddRange(geo.Get<Point>().Select(p =>p.Coordinate));
            geo.Get<Polygon>(false).ForEach(p => coords.AddRange(p.ToCoordinates(distance)));
            //geo.Get<LineString>().ForEach(lstr => coords.AddRange(lstr.ToCoordinates(distance)));
            var vdBuilder = new VoronoiDiagramBuilder();
            vdBuilder.SetSites(coords);
            return vdBuilder.GetDiagram(GeometryFactory.Default);

        }
        public static List<LineSegment> GetVoronoiEdge (this Geometry geo)
        {
            var vdBuilder = new VoronoiDiagramBuilder();
            vdBuilder.SetSites(geo);
            var subdiv = vdBuilder.GetSubdivision();
            return subdiv.GetVertexUniqueEdges(false).Select(e =>e.ToLineSegment()).ToList();

        }
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
        public static double GetBound(this List<Geometry> geos,bool UpperBound,bool Verticle)
        {
            var coors = new List<Coordinate>();
            geos.ForEach(geo => coors.AddRange(geo.Coordinates));
            if (Verticle)
            {
                if(UpperBound) return coors.Max(coor => coor.Y);
                else return coors.Min(coor => coor.Y);
            }
            else
            {
                if (UpperBound) return coors.Max(coor => coor.X);
                else return coors.Min(coor => coor.X);
            }
        }
        //removeHoles Only active when T is polygon 
        public static List<T> Get<T>(this Geometry geometry,bool removeHoles = true)
        {
            var objs = new List<T>();
            if(typeof(T) == null) return objs;
            var typeToGet = typeof(T);
            var geoType = typeof(Geometry);
            if (!(typeToGet.IsSubclassOf(geoType)|| typeToGet == geoType)) throw new NotSupportedException();
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
