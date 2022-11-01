using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Geom
{
    public static class PointExtensions
    {
        public static Point3d Rotate(this Point3d point, double radius, double degree)
        {
            var phi = GeoAlgorithm.AngleFromDegree(degree);
            return point.OffsetXY(radius * Math.Cos(phi), radius * Math.Sin(phi));
        }
        public static Point2d Rotate(this Point2d point, double radius, double degree)
        {
            var phi = GeoAlgorithm.AngleFromDegree(degree);
            return point.OffsetXY(radius * Math.Cos(phi), radius * Math.Sin(phi));
        }
        public static Point3d OffsetX(this Point3d point, double delta) => new(point.X + delta, point.Y, point.Z);
        public static Point3d OffsetY(this Point3d point, double delta) => new(point.X, point.Y + delta, point.Z);
        public static Point3d OffsetXY(this Point3d point, double deltaX, double deltaY) => new(point.X + deltaX, point.Y + deltaY, point.Z);
        public static Point3d ReplaceX(this Point3d point, double x) => new(x, point.Y, point.Z);
        public static Point3d ReplaceY(this Point3d point, double y) => new(point.X, y, point.Z);
        public static Point2d OffsetX(this Point2d point, double delta) => new(point.X + delta, point.Y);
        public static Point2d OffsetY(this Point2d point, double delta) => new(point.X, point.Y + delta);
        public static Point2d OffsetXY(this Point2d point, double deltaX, double deltaY) => new(point.X + deltaX, point.Y + deltaY);
        public static Point2d ReplaceX(this Point2d point, double x) => new(x, point.Y);
        public static Point2d ReplaceY(this Point2d point, double y) => new(point.X, y);
        public static Point2d Offset(this Point2d point, Vector2d v) => new Point2d(point.X + v.X, point.Y + v.Y);
        public static Point3d Offset(this Point3d point, Vector3d v) => new Point3d(point.X + v.X, point.Y + v.Y, point.Z + v.Z);
    }
}
