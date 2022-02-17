using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using ThMEPArchitecture.ParkingStallArrangement.Method;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    public static class Rect
    {
        public static Polyline GetRect(Point3d pt1, Point3d pt2)
        {
            var pline = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(pt1.X, pt1.Y));
            pts.Add(new Point2d(pt1.X, pt2.Y));
            pts.Add(new Point2d(pt2.X, pt2.Y));
            pts.Add(new Point2d(pt2.X, pt1.Y));
            pts.Add(new Point2d(pt1.X, pt1.Y));
            pline.CreatePolyline(pts);

            return pline;
        }

        public static Polyline GetRect(Point3d pt1, double tolerance = 1.0)
        {
            var pline = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(pt1.X - tolerance, pt1.Y - tolerance));
            pts.Add(new Point2d(pt1.X - tolerance, pt1.Y + tolerance));
            pts.Add(new Point2d(pt1.X + tolerance, pt1.Y + tolerance));
            pts.Add(new Point2d(pt1.X + tolerance, pt1.Y - tolerance));
            pts.Add(new Point2d(pt1.X - tolerance, pt1.Y - tolerance));
            pline.CreatePolyline(pts);

            return pline;
        }

        public static Polyline GetRect(this BlockReference br)
        {
            var minPt = br.GeometricExtents.MinPoint;
            var maxPt = br.GeometricExtents.MaxPoint;
            return GetRect(minPt, maxPt);
        }

        public static Polyline GetRectExtend(this BlockReference br, double dist = 1.0)
        {
            var minPt = br.GeometricExtents.MinPoint;
            var maxPt = br.GeometricExtents.MaxPoint;
            return GetRect(minPt.OffSetXY(-dist,-dist), maxPt.OffSetXY(dist, dist));
        }
    }
}
