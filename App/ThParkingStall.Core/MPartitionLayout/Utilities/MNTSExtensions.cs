using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.OverlayNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ThParkingStall.Core.MPartitionLayout.MGeoUtilities;

namespace ThParkingStall.Core.MPartitionLayout
{
    public static class MNTSExtensions
    {
        public static LineString ToLineString(this LineSegment line)
        {
            return new LineString(new Coordinate[] { line.P0, line.P1 });
        }
        public static Coordinate Start(this Polygon polygon)
        {
            return polygon.Coordinates[0];
        }
        public static Coordinate End(this Polygon polygon)
        {
            return polygon.Coordinates[polygon.Coordinates.Count() - 1];
        }
        public static List<LineSegment> GetEdges(this Polygon polygon)
        {
            List<LineSegment> edges = new List<LineSegment>();
            for (int i = 0; i < polygon.Coordinates.Count() - 1; i++)
                edges.Add(new LineSegment(polygon.Coordinates[i], polygon.Coordinates[i + 1]));
            return edges;
        }
        public static List<LineSegment> GetEdges(this LineString lineString)
        {
            List<LineSegment> edges = new List<LineSegment>();
            for (int i = 0; i < lineString.Coordinates.Count() - 1; i++)
                edges.Add(new LineSegment(lineString.Coordinates[i], lineString.Coordinates[i + 1]));
            return edges;
        }
        public static Coordinate ClosestPoint(this Polygon polygon, Coordinate point)
        {
            Coordinate p = new Coordinate(0, 0);
            var edges = polygon.GetEdges();
            var dis = double.PositiveInfinity;
            foreach (var edge in edges)
            {
                var d = edge.ClosestPoint(point).Distance(point);
                if (d < dis)
                {
                    dis = d;
                    p = edge.ClosestPoint(point);
                }
            }
            return p;
        }
        public static Coordinate ClosestPoint(this LineString polygon, Coordinate point)
        {
            Coordinate p = new Coordinate(0, 0);
            var edges = polygon.GetEdges();
            var dis = double.PositiveInfinity;
            foreach (var edge in edges)
            {
                var d = edge.ClosestPoint(point).Distance(point);
                if (d < dis)
                {
                    dis = d;
                    p = edge.ClosestPoint(point);
                }
            }
            return p;
        }
        public static Coordinate ClosestPoint(this LineSegment line, Coordinate point, bool extented = false)
        {        
            if (extented)
            {
                var pt = line.MidPoint;
                line = line.Scale(999999999 / line.Length);
                line = line.Translation(new Vector2D(line.MidPoint, pt));
                return line.ClosestPoint(point);
            }
            return line.ClosestPoint(point);
        }
        public static Polygon PolyJoin(this Polygon poly_a, Polygon poly_b)
        {
            var points = poly_a.Coordinates.ToList();
            if (poly_a.Coordinates[poly_a.Coordinates.Count() - 1] == poly_b.Coordinates[0])
                points.RemoveAt(points.Count - 1);
            points.AddRange(poly_b.Coordinates);
            var poly = new Polygon(new LinearRing(points.ToArray()));
            return poly;
        }
        public static LineString PolyJoin(this LineString poly_a, LineString poly_b)
        {
            var points = poly_a.Coordinates.ToList();
            if (poly_a.Coordinates[poly_a.Coordinates.Count() - 1].Distance(poly_b.Coordinates[0]) < 0.0001)
                points.RemoveAt(points.Count - 1);
            points.AddRange(poly_b.Coordinates);
            var poly = new LineString(points.ToArray());
            return poly;
        }
        public static Vector2D GetPerpendicularVector(this Vector2D vec)
        {
            return new Vector2D(-vec.Y, vec.X);
        }
        public static Polygon Buffer(this LineSegment line, double dis)
        {
            AffineTransformation transformation = new AffineTransformation();
            var vec = Vector(line).GetPerpendicularVector().Normalize();
            transformation.SetToTranslation(vec.X * dis, vec.Y * dis);
            var a = (LineString)transformation.Transform(line.ToGeometry(new GeometryFactory()));
            transformation = new AffineTransformation();
            transformation.SetToTranslation(-vec.X * dis, -vec.Y * dis);
            var b = (LineString)transformation.Transform(line.ToGeometry(new GeometryFactory()));
            var poly = new Polygon(new LinearRing(new Coordinate[] {
                a.StartPoint.Coordinate,a.EndPoint.Coordinate,b.EndPoint.Coordinate,
                b.StartPoint.Coordinate,a.StartPoint.Coordinate}));
            return poly;
        }
        public static Coordinate Translation(this Coordinate point, Vector2D vec)
        {
            return new Coordinate(point.X + vec.X, point.Y + vec.Y);
        }
        public static LineString Translation(this LineString line, Vector2D vec)
        {
            AffineTransformation transformation = new AffineTransformation();
            transformation.SetToTranslation(vec.X, vec.Y);
            return (LineString)transformation.Transform(line);
        }
        public static LineSegment Translation(this LineSegment line, Vector2D vec)
        {
            AffineTransformation transformation = new AffineTransformation();
            transformation.SetToTranslation(vec.X, vec.Y);
            var linestring = (LineString)transformation.Transform(line.ToGeometry(new GeometryFactory()));
            return new LineSegment(linestring.StartPoint.Coordinate, linestring.EndPoint.Coordinate);
        }
        public static Polygon Translation(this Polygon ply, Vector2D vec)
        {
            AffineTransformation transformation = new AffineTransformation();
            transformation.SetToTranslation(vec.X, vec.Y);
            return (Polygon)transformation.Transform(ply);
        }
        public static LineSegment Scale(this LineSegment line, double factor)
        {
            AffineTransformation transformation = new AffineTransformation();
            transformation.SetToScale(factor, factor);
            var linestring = (LineString)transformation.Transform(line.ToGeometry(new GeometryFactory()));
            var ls = new LineSegment(linestring.StartPoint.Coordinate, linestring.EndPoint.Coordinate);
            ls = ls.Translation(new Vector2D(ls.MidPoint, line.MidPoint));
            return ls;
        }
        public static Polygon Scale(this Polygon poly, double factor)
        {
            AffineTransformation transformation = new AffineTransformation();
            transformation.Scale(factor, factor);
            var p = (Polygon)transformation.Transform(poly);
            p = p.Translation(new Vector2D(p.Envelope.Centroid.Coordinate, poly.Envelope.Centroid.Coordinate));
            return p;
        }
        public static Coordinate[] IntersectPoint(this Geometry p, Geometry ply)
        {
            if (p is Polygon) p = RemoveDuplicatedPointOnPolygon((Polygon)p).Shell;
            if (ply is Polygon) ply = RemoveDuplicatedPointOnPolygon((Polygon)ply).Shell;
            var g = OverlayNGRobust.Overlay(p, ply, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
            var results=new List<Coordinate>();
            if (g is Point) results.Add(((Point)g).Coordinate);
            else if (g is LineString) results.AddRange(((LineString)g).Coordinates);
            else if (g is MultiPoint) results.AddRange(((MultiPoint)g).Coordinates);
            else if (g is Polygon)
            {
                if (ply is Polygon && p is Polygon)
                    results.AddRange(((Polygon)OverlayNGRobust.Overlay(p, ply, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection)).Coordinates.Where(e =>
           ((Polygon)p).ClosestPoint(e).Distance(e) < 1 && ((Polygon)ply).ClosestPoint(e).Distance(e) < 1));
            }
            else if (g is GeometryCollection collection)
            {
                foreach (var geo in collection.Geometries)
                {
                    if (geo is Point) results.Add(((Point)geo).Coordinate);
                    else if (geo is LineString) results.AddRange(((LineString)geo).Coordinates);
                    else if (geo is MultiPoint) results.AddRange(((MultiPoint)geo).Coordinates);
                }
            }
            return results.ToArray();
        }
        public static Coordinate[] IntersectPoint(this LineSegment line, Polygon ply)
        {
            ply = RemoveDuplicatedPointOnPolygon(ply);
            var shell = ply.Shell;
            var p = new LineString(new Coordinate[] { line.P0, line.P1 });
            var g = OverlayNGRobust.Overlay(p, shell, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
            if (g is Point) return new Coordinate[] { ((Point)g).Coordinate };
            else if (g is MultiPoint)
                return ((MultiPoint)g).Coordinates;
            //出现了两曲线为相交但判为相交的bug，用where筛除一下
            else if (g is GeometryCollection)
            {
                return g.Coordinates.Where(t => line.ClosestPoint(t).Distance(t) < 0.001
                    && shell.ClosestPoint(t).Distance(t) < 0.001).ToArray();
            }
            return ((LineString)g).Coordinates
                .Where(t => line.ClosestPoint(t).Distance(t)<0.001
                && shell.ClosestPoint(t).Distance(t) < 0.001).ToArray();
        }
        public static Coordinate[] IntersectPoint(this LineSegment line, LineString ply)
        {
            var p = new LineString(new Coordinate[] { line.P0, line.P1 });
            var g = OverlayNGRobust.Overlay(p, ply, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
            if (g is Point) return new Coordinate[] { ((Point)g).Coordinate };
            else if (g is MultiPoint)
                return ((MultiPoint)g).Coordinates;
            //出现了两曲线为相交但判为相交的bug，用where筛除一下
            return ((LineString)g).Coordinates
                .Where(t => line.ClosestPoint(t).Distance(t) < 0.001
                && ply.ClosestPoint(t).Distance(t)<0.001).ToArray();
        }
        public static Coordinate[] IntersectPoint(this LineSegment line, LineSegment pl)
        {
            var ply = new LineString(new Coordinate[] { pl.P0, pl.P1 });
            var p = new LineString(new Coordinate[] { line.P0, line.P1 });
            var g = OverlayNGRobust.Overlay(p, ply, NetTopologySuite.Operation.Overlay.SpatialFunction.Intersection);
            if (g is Point) return new Coordinate[] { ((Point)g).Coordinate };
            else if (g is MultiPoint)
                return ((MultiPoint)g).Coordinates;
            else if (g is MultiLineString)
            {
                if (g.Coordinates.Count() > 0) return (new List<Coordinate>() { g.Coordinates.First(), g.Coordinates.Last() })
                        .Where(t => line.ClosestPoint(t).Distance(t) < 0.001
                && ply.ClosestPoint(t).Distance(t) < 0.001).ToArray();
                else return g.Coordinates;
            }
            return ((LineString)g).Coordinates.Where(t => line.ClosestPoint(t).Distance(t) < 0.001
                && ply.ClosestPoint(t).Distance(t) < 0.001).ToArray();
        }
        public static bool Contains(this Polygon ply, Coordinate p)
        {
            return ply.Contains(new Point(p));
        }
        public static List<LineString> GetSplitCurves(this LineString lineString, List<Coordinate> points)
        {
            points=SortAlongCurve(points, lineString);
            List<LineString> results = new List<LineString>();
            List<List<Coordinate>> coords = new List<List<Coordinate>>();
            coords.Add(new List<Coordinate>() { lineString.Coordinates.ToList()[0] });
            for (int i = 1; i < lineString.Coordinates.Count(); i++)
            {
                var seg = new LineSegment(lineString.Coordinates.ToList()[i - 1], lineString.Coordinates.ToList()[i]);
                for (int j = 0; j < points.Count; j++)
                {
                    var pt = points[j];
                    if (seg.ClosestPoint(pt).Distance(pt) < 0.001)
                    {
                        coords[coords.Count - 1].Add(pt);
                        coords.Add(new List<Coordinate>() { pt });
                        points.RemoveAt(j);
                        j--;
                    }
                    else
                    {
                        coords[coords.Count - 1].Add(lineString.Coordinates.ToList()[i]);
                    }
                }
                coords[coords.Count - 1].Add(lineString.Coordinates.ToList()[i]);
            }
            for (int i = 0; i < coords.Count; i++)
                coords[i] = RemoveDuplicatePts(coords[i]);
            foreach (var coos in coords)
            {
                if (coos.Count < 2) continue;
                results.Add(new LineString(coos.ToArray()));
            }             
            return results;
        }
        public static Coordinate GetMidPoint(this LineString line)
        {
            return GetPointAtDisPara(line, line.Length / 2);
        }
        public static Polygon Clone(this Polygon polygon)
        {
            return new Polygon(new LinearRing(polygon.Coordinates));
        }
        public static IEnumerable<Coordinate> GetPointsByDist(this LineSegment cv, double distDelta)
        {
            for (var dist = 0d; dist <= cv.Length; dist += distDelta)
            {
                yield return GetPointAtDisPara(cv.ToLineString(), dist);
            }
        }
        public static Polygon Simplify(this Polygon polygon)
        {
            var points = polygon.Coordinates.ToList();
            points.RemoveAt(points.Count - 1);
            if (points.Count < 3) return polygon;
            for (int i = 0; i < points.Count; i++)
            {
                if (i == 0)
                {
                    var a = new Vector2D(points[points.Count - 1], points[i]);
                    var b=new Vector2D(points[i],points[i + 1]);
                    if (a.IsParallel(b) && a.Dot(b) > 0)
                    {
                        points.RemoveAt(i);
                        i--;
                    }
                }
                else if (i == points.Count - 1)
                {
                    var a =new Vector2D(points[i-1],points[i]);
                    var b = new Vector2D(points[i], points[0]);
                    if (a.IsParallel(b) && a.Dot(b) > 0)
                    {
                        points.RemoveAt(i);
                        i--;
                    }
                }
                else
                {
                    var a = new Vector2D(points[i - 1], points[i]);
                    var b = new Vector2D(points[i], points[i + 1]);
                    if (a.IsParallel(b) && a.Dot(b) > 0)
                    {
                        points.RemoveAt(i);
                        i--;
                    }
                }
            }
            points.Add(points[0]);
            return new Polygon(new LinearRing(points.ToArray()));
        }
        public static Geometry BufferPL(this Polygon polyline, double distance)
        {
            var buffer = new BufferOp(new LineString(polyline.Coordinates), new BufferParameters()
            {
                JoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle.Mitre,
                EndCapStyle = EndCapStyle.Square,
            });
            return buffer.GetResultGeometry(distance);
        }
    }
}
