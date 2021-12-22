using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace ThMEPArchitecture.ParkingStallArrangement.Method
{
    public static class PtTools
    {
        public static Point3d GetMiddlePt(Point3d pt1, Point3d pt2)
        {
            return new Point3d((pt1.X + pt2.X)/2, (pt1.Y + pt2.Y) / 2, 0);
        }
        public static Point3d GetMiddlePt(this Line line)
        {
            return GetMiddlePt(line.StartPoint, line.EndPoint);
        }
        public static int GetPtDir(this Point3d pt1, Point3d pt2, double tor = 0.035)
        {
            var line = new Line(pt1, pt2);
            var sinAngle = Math.Abs(Math.Sin(line.Angle));
            var cosAngle = Math.Abs(Math.Cos(line.Angle));
            if (sinAngle < tor)//水平线
            {
                return 0;
            }
            if (cosAngle < tor)//竖直线
            {
                return 1;
            }
            return -1;//不是水平竖直线
        }
        public static Point3d OffSetX(this Point3d pt, double val)
        {
            return new Point3d(pt.X + val, pt.Y, 0);
        }
        public static Point3d OffSetY(this Point3d pt, double val)
        {
            return new Point3d(pt.X, pt.Y + val, 0);
        }
        public static Point3d OffSetXY(this Point3d pt, double valX, double valY)
        {
            return new Point3d(pt.X + valX, pt.Y + valY, 0);
        }
        public static Line OffSetX(this Line line, double val)
        {
            return new Line(line.StartPoint.OffSetX(val), line.EndPoint.OffSetX(val));
        }
        public static Line OffSetY(this Line line, double val)
        {
            return new Line(line.StartPoint.OffSetY(val), line.EndPoint.OffSetY(val));
        }
    }
}
