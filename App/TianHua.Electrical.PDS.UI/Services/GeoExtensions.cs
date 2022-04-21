using System;
using System.Windows;

namespace TianHua.Electrical.PDS.UI.WpfServices
{
    public static class GeoExtensions
    {
        public static double AngleToDegree(this double angle)
        {
            return angle * 180 / Math.PI;
        }
        public static double AngleFromDegree(this double degree)
        {
            return degree * Math.PI / 180;
        }
        public static Rect OffsetXY(this Rect r, double dx, double dy)
        {
            var r2 = r;
            r2.Offset(dx, dy);
            return r2;
        }
        public static Rect ToWpfRect(this GRect r) => new(r.LeftTop, r.RightButtom);
        public static Point OffsetXY(this Point point, double dx, double dy)
        {
            point.Offset(dx, dy);
            return point;
        }
        public static Point OffsetY(this Point point, double dy)
        {
            point.Offset(0, dy);
            return point;
        }
        public static Point OffsetX(this Point point, double dx)
        {
            point.Offset(dx, 0);
            return point;
        }
        public static double GetDistanceTo(this Point pt1, Point pt2)
        {
            var v1 = pt1.X - pt2.X;
            var v2 = pt1.Y - pt2.Y;
            return Math.Sqrt(v1 * v1 + v2 * v2);
        }
    }
}
