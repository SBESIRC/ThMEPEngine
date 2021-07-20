using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Algorithm.Distance;
using NetTopologySuite.Operation.Distance;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSDistance
    {
        public static double Distance(this Polyline polyline, Point3d point)
        {
            var distance = new PointPairDistance();
            DistanceToPoint.ComputeDistance(polyline.ToNTSLineString(),
                point.ToNTSCoordinate(), distance);
            return distance.Distance;
        }

        public static double Dictance(this Polygon polygon,Point3d point)
        {
            var distance = new PointPairDistance();
            DistanceToPoint.ComputeDistance(polygon,
                point.ToNTSCoordinate(), distance);
            return distance.Distance;
        }

        public static Point3d GetClosePoint(this Polygon polygon, Point3d point)
        {
            var results = new List<Point3d>();
            var distance = new PointPairDistance();
            DistanceToPoint.ComputeDistance(polygon,
                point.ToNTSCoordinate(), distance);
            foreach(Coordinate coord in distance.Coordinates)
            {
                results.Add(coord.ToAcGePoint3d());
            }
            return results.OrderByDescending(o => o.DistanceTo(point)).First();
        }

        public static Point3d GetClosePoint(this Polyline polyline, Point3d point)
        {
            var results = new List<Point3d>();
            var distance = new PointPairDistance();
            DistanceToPoint.ComputeDistance(polyline.ToNTSLineString(),
                point.ToNTSCoordinate(), distance);
            foreach (Coordinate coord in distance.Coordinates)
            {
                results.Add(coord.ToAcGePoint3d());
            }
            return results.OrderByDescending(o => o.DistanceTo(point)).First();
        }

        public static double Distance(this Curve line, Curve curve)
        {
            return line.ToNTSGeometry().Distance(curve.ToNTSGeometry());
        }

        public static double IndexedDistance(this Curve line, Curve curve)
        {
            return IndexedFacetDistance.Distance(line.ToNTSGeometry(), curve.ToNTSGeometry());
        }
    }
}
