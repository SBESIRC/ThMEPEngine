using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;
using ThMEPWSS.Uitl.ExtensionsNs;

namespace ThMEPWSS.UndergroundSpraySystem.General
{
    public static class Rect
    {
        public static Tuple<Point3d, Point3d> GetRect(this Line line, bool flag = true)
        {
            double leftX;
            double rightX;
            double leftY;
            double rightY;
            Point3d pt1, pt2;

            if (line.StartPoint.X < line.EndPoint.X)
            {
                leftX = line.StartPoint.X;
                rightX = line.EndPoint.X;
                leftY = line.StartPoint.Y;
                rightY = line.EndPoint.Y;
            }
            else
            {
                leftX = line.EndPoint.X;
                rightX = line.StartPoint.X;
                leftY = line.EndPoint.Y;
                rightY = line.StartPoint.Y;
            }

            if(flag)
            {
                pt1 = new Point3d(leftX, leftY + 350, 0);
                pt2 = new Point3d(rightX, rightY + 150, 0);
            }
            else
            {
                pt1 = new Point3d(leftX, leftY - 150, 0);
                pt2 = new Point3d(rightX, rightY - 350, 0);
            }

            var tuplePoint = new Tuple<Point3d, Point3d>(pt1, pt2);
            return tuplePoint;
        }

        public static Polyline GetRect(this Point3d pt, double tolerance = 100)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();

            pts.Add(pt.OffsetXY(-tolerance, tolerance).ToPoint2D());
            pts.Add(pt.OffsetXY(tolerance, tolerance).ToPoint2D());
            pts.Add(pt.OffsetXY(tolerance, -tolerance).ToPoint2D());
            pts.Add(pt.OffsetXY(-tolerance, -tolerance).ToPoint2D());
            pts.Add(pt.OffsetXY(-tolerance, tolerance).ToPoint2D());

            pl.CreatePolyline(pts);

            return pl;
        }
    }
}
