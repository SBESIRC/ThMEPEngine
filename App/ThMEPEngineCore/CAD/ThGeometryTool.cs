using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public static class ThGeometryTool
    {
        /// <summary>
        /// 计算最大最小点
        /// </summary>
        /// <returns></returns>
        public static List<Point3d> CalBoundingBox(List<Point3d> points)
        {
            double maxX = points.Max(x => x.X);
            double minX = points.Min(x => x.X);
            double maxY = points.Max(x => x.Y);
            double minY = points.Min(x => x.Y);

            return new List<Point3d>(){                
                new Point3d(minX, minY, 0),
                new Point3d(maxX, maxY, 0)
            };
        }
        public static bool IsParallelToEx(this Vector3d vector, Vector3d other, double tolerance = 1.0)
        {
            double angle = vector.GetAngleTo(other) / Math.PI * 180.0;
            return (angle < tolerance) || ((180.0 - angle) < tolerance);
        }
        public static Point3d GetMidPt(Point3d pt1, Point3d pt2)
        {
            return new Point3d((pt1.X + pt2.X) / 2.0, (pt1.Y + pt2.Y) / 2.0, (pt1.Z + pt2.Z) / 2.0);
        }
        public static bool IntersectWithEx(this Entity firstEntity, Entity secondEntity)
        {
            Point3dCollection pts = new Point3dCollection();
            Plane zeroPlane = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
            firstEntity.IntersectWith(secondEntity, Intersect.OnBothOperands, zeroPlane, pts, IntPtr.Zero, IntPtr.Zero);
            zeroPlane.Dispose();
            return pts.Count > 0 ? true : false;
        }
    }
}
