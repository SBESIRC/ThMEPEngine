using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPEngineCore.Geom
{
    public static class GeoAlgorithm
    {
        public static Vector3d ToVector3d(this Point3d pt) => new Vector3d(pt.X, pt.Y, pt.Z);
        public static Vector2d ToVector2d(this Point2d pt) => new Vector2d(pt.X, pt.Y);
        public static LinearRing ToLinearRing(this GRect r)
        {
            return new LinearRing(ConvertToCoordinateArray(r));
        }
        public static Coordinate[] ConvertToCoordinateArray(GRect r)
        {
            var pt = r.LeftTop.ToNTSCoordinate();
            return new Coordinate[] { pt, r.RightTop.ToNTSCoordinate(), r.RightButtom.ToNTSCoordinate(), r.LeftButtom.ToNTSCoordinate(), pt, };
        }
        public static Point2d MidPoint(Point2d pt1, Point2d pt2)
        {
            Point2d midPoint = new Point2d((pt1.X + pt2.X) / 2.0,
                                        (pt1.Y + pt2.Y) / 2.0);
            return midPoint;
        }
        public static GLineSegment ToGLineSegment(this Line line)
        {
            return new GLineSegment(line.StartPoint.ToPoint2D(), line.EndPoint.ToPoint2D());
        }
        public static GRect ToGRect(this Extents3d extents3D)
        {
            return new GRect(extents3D.MinPoint, extents3D.MaxPoint);
        }
        public static Point3d MidPoint(Point3d pt1, Point3d pt2)
        {
            Point3d midPoint = new Point3d((pt1.X + pt2.X) / 2.0,
                                        (pt1.Y + pt2.Y) / 2.0,
                                        (pt1.Z + pt2.Z) / 2.0);
            return midPoint;
        }
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
        public static double AngleFromDegree(this double degree)
        {
            return degree * Math.PI / 180;
        }
    }
}
