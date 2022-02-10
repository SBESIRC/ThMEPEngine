using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System.Linq;

namespace ThMEPWSS.UndergroundFireHydrantSystem
{
    public static class General
    {
        public static Point3d GetMidPt(Point3d pt1, Point3d pt2)
        {
            double x = (pt1.X + pt2.X) / 2;
            double y = (pt1.Y + pt2.Y) / 2;
            return new Point3d(x, y, 0);
        }

        public static Point3d GetMidPt(DBText dBText)//获取文字的中心点
        {
            var pt1 = dBText.GeometricExtents.MaxPoint;
            var pt2 = dBText.GeometricExtents.MinPoint;
            return GetMidPt(pt1, pt2);
        }

        /// <summary>
        /// 获取点为中心的包围框
        /// </summary>
        /// <param name="centerPt"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static Polyline GetRect(this Point3d centerPt, double tolerance = 100)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(centerPt.X - tolerance, centerPt.Y - tolerance)); // low left
            pts.Add(new Point2d(centerPt.X - tolerance, centerPt.Y + tolerance)); // high left
            pts.Add(new Point2d(centerPt.X + tolerance, centerPt.Y + tolerance)); // high right
            pts.Add(new Point2d(centerPt.X + tolerance, centerPt.Y - tolerance)); // low right
            pts.Add(new Point2d(centerPt.X - tolerance, centerPt.Y - tolerance)); // low left

            pl.CreatePolyline(pts);

            return pl;
        }

        
        public static double GetLinesDist(this Line l1, Line l2)
        {
            var dist1 = l1.StartPoint.DistanceTo(l2.StartPoint);
            var dist2 = l1.StartPoint.DistanceTo(l2.EndPoint);
            var dist3 = l1.EndPoint.DistanceTo(l2.StartPoint);
            var dist4 = l1.EndPoint.DistanceTo(l2.EndPoint);
            return Math.Min(Math.Min(dist1, dist2), Math.Min(dist3, dist4));
        }

        public static double GetLineDist2(this Line l1, Line l2)
        {
            double dist1 = l1.GetClosestPointTo(l2.StartPoint, false).DistanceTo(l2.StartPoint);
            double dist2 = l1.GetClosestPointTo(l2.EndPoint, false).DistanceTo(l2.EndPoint);

            return Math.Min(dist1, dist2);
        }

        public static bool GetNearestPt(this Point3dEx pt, ref Point3dEx tpt, List<Point3dEx> pts, double tolerance = 250)
        {
            var rangePts = new List<Point3dEx>();
            foreach (var p in pts)
            {
                if (p._pt.DistanceTo(pt._pt) < tolerance)
                {
                    rangePts.Add(p);
                    //tpt = p;
                    //return true;
                }
            }
            if(rangePts.Count == 0) return false;
            var sortedPts = rangePts.OrderBy(p => p._pt.DistanceTo(pt._pt)).ToList();
            tpt = sortedPts.First();
            return true;
        }
    }
}
