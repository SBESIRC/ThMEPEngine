﻿using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

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

        public static bool GetNearestPt(this Point3dEx pt, ref Point3dEx tpt, List<Point3dEx> pts, double tolerance = 150)
        {
            foreach (var p in pts)
            {
                if (p._pt.DistanceTo(pt._pt) < tolerance)
                {
                    tpt = p;
                    return true;
                }
            }
            return false;
        }
    }
}
