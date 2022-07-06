using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Densify;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Triangulate;
using ThParkingStall.Core.MPartitionLayout;

namespace ThParkingStall.Core.Tools
{
    public static class PolygonEx
    {
        static BufferParameters MitreParam = new BufferParameters(8, EndCapStyle.Flat, JoinStyle.Mitre, 5.0);
        public static Polygon RemoveHoles(this Polygon polygon)
        {
            if (polygon.NumInteriorRings > 0)
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
        public static List<Coordinate> ToCoordinates(this Polygon polygon,double distance)
        {
            var coors = new List<Coordinate>();
            coors.AddRange(polygon.Shell.ToCoordinates(distance));
            foreach(var hole in polygon.Holes)
            {
                coors.AddRange(hole.ToCoordinates(distance));
            }
            return coors;
        }
        public static Polygon Obtusify(this Polygon polygon, bool inner, double tolerance = 0.1)
        {
            var shell = polygon.Shell.Obtusify(inner,tolerance);
            if(shell == null) return null;
            var holes = polygon.Holes.Select(h => h.Obtusify(!inner, tolerance)).Where(h => h != null).ToArray();
            return new Polygon(shell, holes);
        }
        //public static Geometry MitreBuffer(this Polygon polygon,double distance, double Obt_tol = 0.1)
        //{
        //    return polygon.Obtusify(Obt_tol).Buffer(distance, MitreParam);
        //}

        //public static List<LineSegment> GetVoronoiCenter(this Polygon polygon, double distance = 1000)//spatial index
        //{
        //    var coords = new List<Coordinate>();
        //    //coords.AddRange(geo.Get<Point>().Select(p =>p.Coordinate));
        //     coords.AddRange(polygon.ToCoordinates(distance));
        //    //geo.Get<LineString>().ForEach(lstr => coords.AddRange(lstr.ToCoordinates(distance)));
        //    var vdBuilder = new VoronoiDiagramBuilder();
        //    vdBuilder.SetSites(coords);
        //    var polys = vdBuilder.GetDiagram(GeometryFactory.Default).Get<Polygon>();
        //    var lstrs = new List<LineString>();
        //    polys.ForEach(p => lstrs.AddRange(p.Shell.ToLineSegments().ToLineStrings()));
        //    var edgeSPIdx = new MNTSSpatialIndex(lstrs);
        //    var plstrs = new List<LineString> { polygon.Shell};
        //    plstrs.AddRange(polygon.Holes);
        //    var mlstr = new MultiLineString(plstrs.ToArray());
        //    var selected = edgeSPIdx.SelectNOTCrossingGeometry(mlstr).Cast<LineString>();
        //    return selected.Where(l => polygon.Contains(l)).ToLineSegments();

        //}
        //public static List<LineSegment> GetVoronoiCenter(this Polygon polygon, double distance = 1000)//换种方式 不去重
        //{
        //    var coords = new List<Coordinate>();
        //    //coords.AddRange(geo.Get<Point>().Select(p =>p.Coordinate));
        //    coords.AddRange(polygon.ToCoordinates(distance));
        //    //geo.Get<LineString>().ForEach(lstr => coords.AddRange(lstr.ToCoordinates(distance)));
        //    var vdBuilder = new VoronoiDiagramBuilder();
        //    vdBuilder.SetSites(coords);
        //    var polys = vdBuilder.GetDiagram(GeometryFactory.Default).Get<Polygon>();
        //    var lstrs = new HashSet<LineString>();
        //    foreach(var p in polys)
        //    {
        //        foreach(var lstr in p.Shell.ToLineSegments().ToLineStrings())
        //        {
        //            lstrs.Add(lstr);
        //        }
        //    }
        //    var lines = new List<LineSegment>();
        //    foreach(var lstr in lstrs)
        //    {
        //        if (lstr.Within(polygon))
        //        {
        //            lines.AddRange(lstr.ToLineSegments());
        //        }
        //    }
        //    return lines;

        //}

        //public static List<LineSegment> GetVoronoiCenter(this Polygon polygon, double distance = 1000)//去重过后的

        //{
        //    var coords = new List<Coordinate>();
        //    //coords.AddRange(geo.Get<Point>().Select(p =>p.Coordinate));
        //    coords.AddRange(polygon.ToCoordinates(distance));
        //    //geo.Get<LineString>().ForEach(lstr => coords.AddRange(lstr.ToCoordinates(distance)));
        //    var vdBuilder = new VoronoiDiagramBuilder();
        //    vdBuilder.SetSites(coords);

        //    var shells = new MultiLineString(vdBuilder.GetDiagram(GeometryFactory.Default).Get<Polygon>().Select(p =>p.Shell).ToArray()).Union().Get<LineString>();
        //    var lstrs = new List<LineString>();
        //    foreach (var shell in shells)
        //    {

        //        foreach (var lstr in shell.ToLineSegments().ToLineStrings())
        //        {
        //            lstrs.Add(lstr);
        //        }
        //    }
        //    var lines = new List<LineSegment>();
        //    foreach (var lstr in lstrs)
        //    {
        //        if (lstr.Within(polygon))
        //        {
        //            lines.AddRange(lstr.ToLineSegments());
        //        }
        //    }
        //    return lines;

        //}

        //public static List<LineSegment> GetVoronoiCenter(this Polygon polygon, double distance = 1000)//去重过后的+找环

        //{
        //    var coords = new List<Coordinate>();
        //    //coords.AddRange(geo.Get<Point>().Select(p =>p.Coordinate));
        //    coords.AddRange(polygon.ToCoordinates(distance));
        //    //geo.Get<LineString>().ForEach(lstr => coords.AddRange(lstr.ToCoordinates(distance)));
        //    var vdBuilder = new VoronoiDiagramBuilder();
        //    vdBuilder.SetSites(coords);

        //    var shells = new MultiLineString(vdBuilder.GetDiagram(GeometryFactory.Default).Get<Polygon>().Select(p => p.Shell).ToArray()).Union().Get<LineString>();
        //    var lstrs = new List<LineString>();
        //    foreach (var shell in shells)
        //    {

        //        foreach (var lstr in shell.ToLineSegments().ToLineStrings())
        //        {
        //            lstrs.Add(lstr);
        //        }
        //    }
        //    var lines = new List<LineSegment>();
        //    foreach (var lstr in lstrs)
        //    {
        //        if (lstr.Within(polygon))
        //        {
        //            lines.AddRange(lstr.ToLineSegments());
        //        }
        //    }
        //    var result = new List<LineSegment>();
        //    lines.GetPolygons().ForEach(p => result.AddRange(p.Shell.ToLineSegments()));
        //    return result;

        //}
        public static MultiLineString GetVoronoiCenter(this Polygon polygon, double distance = 1000)//去重过后的+找环+返回multilinestr

        {
            //var coords = new List<Coordinate>();
            ////coords.AddRange(geo.Get<Point>().Select(p =>p.Coordinate));
            //coords.AddRange(polygon.ToCoordinates(distance));
            ////geo.Get<LineString>().ForEach(lstr => coords.AddRange(lstr.ToCoordinates(distance)));
            var vdBuilder = new VoronoiDiagramBuilder();
            vdBuilder.SetSites(Densifier.Densify(polygon, distance));

            var shells = new MultiLineString(vdBuilder.GetDiagram(GeometryFactory.Default).Get<Polygon>().Select(p => p.Shell).ToArray()).Union().Get<LineString>();
            var lstrs = new List<LineString>();
            foreach (var shell in shells)
            {

                foreach (var lstr in shell.ToLineSegments().ToLineStrings())
                {
                    lstrs.Add(lstr);
                }
            }
            var lines = new List<LineSegment>();
            foreach (var lstr in lstrs)
            {
                if (lstr.Within(polygon))
                {
                    lines.AddRange(lstr.ToLineSegments());
                }
            }
            var result = new List<LineSegment>();
            return new MultiLineString(lines.GetPolygons().Select(p => p.Shell).ToArray());
        }
    }
}
