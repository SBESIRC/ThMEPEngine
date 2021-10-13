using System.Collections.Generic;
using NetTopologySuite.Geometries;
using Autodesk.AutoCAD.Geometry;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSGeExtension
    {
        public static Point3d ToAcGePoint3d(this Coordinate coordinate)
        {
            return new Point3d(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.X),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.Y),
                0);
        }

        public static Point3d ToAcGePoint3d(this Point point)
        {
            return point.Coordinate.ToAcGePoint3d();
        }

        public static Point2d ToAcGePoint2d(this Coordinate coordinate)
        {
            return new Point2d(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.X),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.Y));
        }

        public static Point2d ToAcGePoint2d(this Point point)
        {
            return point.Coordinate.ToAcGePoint2d();
        }
        public static Point ToNTSPoint(this Point3d point)
        {
            return new Point(point.X, point.Y);
        }
        public static Point ToNTSPoint(this Point2d point)
        {
            return new Point(point.X, point.Y);
        }
        public static Coordinate ToNTSCoordinate(this Point3d point)
        {
            return new Coordinate(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.X),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.Y));
        }

        public static Coordinate ToNTSCoordinate(this Point2d point)
        {
            return new Coordinate(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.X),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.Y));
        }

        public static Coordinate[] ToNTSCoordinates(this Point3dCollection points)
        {
            var coordinates = new List<Coordinate>();
            foreach (Point3d pt in points)
            {
                coordinates.Add(pt.ToNTSCoordinate());
            }
            return coordinates.ToArray();
        }

        public static Point3dCollection ToAcGePoint3ds(this Coordinate[] coordinates)
        {
            var points = new Point3dCollection();
            foreach (var coordinate in coordinates)
            {
                points.Add(coordinate.ToAcGePoint3d());
            }
            return points;
        }
    }
}
