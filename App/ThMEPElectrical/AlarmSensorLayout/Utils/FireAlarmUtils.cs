using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Algorithm.Locate;
using NetTopologySuite.Algorithm.Distance;

namespace ThMEPElectrical.AlarmSensorLayout.Method
{
    public static class FireAlarmUtils
    {
        //点在区域内
        public static bool PolygonRealContainPoint(Polygon polygon, Coordinate point)
        {
            var locator = new IndexedPointInAreaLocator(polygon);
            var location = locator.Locate(point);
            return location == Location.Interior;
        }
        //点在区域内或在边界上
        public static bool PolygonContainPoint(Polygon polygon, Coordinate point)
        {
            var locator = new IndexedPointInAreaLocator(polygon);
            var location = locator.Locate(point);
            return location == Location.Interior || location == Location.Boundary;
        }
        //点在多个分区域构成的范围内
        public static bool MultiPolygonContainPoint(List<Polygon> polygons, Coordinate point)
        {
            foreach (var polygon in polygons)
                if (PolygonContainPoint(polygon, point))
                    return true;
            return false;
        }
        public static List<Coordinate> LineInteresectWithPolygon(LineSegment line, Polygon polygon)
        {
            var new_line = line.ToDbLine().ToNTSLineString();
            return polygon.Intersection(new_line).Coordinates.ToList();
        }
        public static List<LineSegment> LineIntersectWithMutiPolygon(LineSegment line, List<Polygon> polygons)
        {
            var ans = new List<LineSegment>();
            var linestring = line.ToDbLine().ToNTSLineString();
            foreach (var polygon in polygons)
            {
                var intersectLine = polygon.Intersection(linestring);
                if (intersectLine.NumPoints == 2)
                    ans.Add(new LineSegment(intersectLine.Coordinates[0], intersectLine.Coordinates[1]));
            }
            return ans;
        }
        public static Coordinate GetClosePointOnPolygon(Polygon polygon, Coordinate point)
        {
            var distance = new PointPairDistance();
            DistanceToPoint.ComputeDistance(polygon, point, distance);
            return distance.Coordinates.OrderByDescending(o => o.Distance(point)).First();
        }
        public static Coordinate GetClosePointOnMultiPolygon(MultiPolygon polygons, Coordinate point)
        {
            var ans = new List<Coordinate>();
            foreach (Polygon mpoly in polygons)
                ans.Add(GetClosePointOnPolygon(mpoly, point));
            return ans.OrderBy(o => o.Distance(point)).First();
        }
        public static List<Coordinate> LineInteresectWithLinestring(LineSegment line, LineString lineString)
        {
            var coodinates = new Coordinate[2];
            coodinates[0] = line.P0;
            coodinates[1] = line.P1;
            var ls = new LineString(coodinates);
            return ls.Intersection(lineString).Coordinates.ToList();
        }
        public static Coordinate AdjustedCenterPoint(Polygon polygon, double buffer = 10)
        {
            var center = Centroid.GetCentroid(polygon);
            if (PolygonContainPoint(polygon, center))
                return center;
            var bufferPoly = polygon.Buffer(-buffer);
            if (bufferPoly is Polygon polygon1)
                return GetClosePointOnPolygon(polygon1, center);
            if (bufferPoly is MultiPolygon multiPolygon)
                return GetClosePointOnMultiPolygon(multiPolygon, center);
            return null;
        }
    }
}
