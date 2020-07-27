using GeoAPI.Geometries;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace ThCADCore.NTS
{
    public static class ThCADCoreNTSGeExtension
    {
        public static Point3d ToAcGePoint3d(this Coordinate coordinate)
        {
            if (!double.IsNaN(coordinate.Z))
            {
                return new Point3d(
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.X),
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.Y),
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.Z)
                    );
            }
            else
            {
                return new Point3d(
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.X),
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.Y),
                    0);
            }
        }

        public static Point3d ToAcGePoint3d(this IPoint point)
        {
            return point.Coordinate.ToAcGePoint3d();
        }

        public static Point2d ToAcGePoint2d(this Coordinate coordinate)
        {
            return new Point2d(
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.X),
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(coordinate.Y)
                );
        }

        public static Point2d ToAcGePoint2d(this IPoint point)
        {
            return point.Coordinate.ToAcGePoint2d();
        }

        public static Coordinate ToNTSCoordinate(this Point3d point)
        {
            return new Coordinate(
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.X),
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.Y),
                    ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.Z)
                    );

        }

        public static Coordinate ToNTSCoordinate(this Point2d point)
        {
            return new Coordinate(
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.X),
                ThCADCoreNTSService.Instance.PrecisionModel.MakePrecise(point.Y)
                );
        }

        public static Coordinate[] ToNTSCoordinates(this Point3dCollection points)
        {
            var coordinates = new List<Coordinate>();
            foreach(Point3d pt in points)
            {
                coordinates.Add(pt.ToNTSCoordinate());
            }
            return coordinates.ToArray();
        }

        public static bool IsReflex(Coordinate p1, Coordinate p2, Coordinate p3)
        {
            return (p3.Y - p1.Y) * (p2.X - p1.X) - (p3.X - p1.X) * (p2.Y - p1.Y) < 0;
        }

        public static bool IsConvex(Coordinate p1, Coordinate p2, Coordinate p3)
        {
            return (p3.Y - p1.Y) * (p2.X - p1.X) - (p3.X - p1.X) * (p2.Y - p1.Y) > 0;
        }
    }
}
