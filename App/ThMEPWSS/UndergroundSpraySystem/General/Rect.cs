using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;
using ThMEPWSS.UndergroundSpraySystem.Model;

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

        public static List<Point3d> GetRectPt(this Point3d centerPt, SprayIn sprayIn, DBObjectCollection lines, double tolerance = 100)
        {
            var pts = new List<Point3d>();
            var unUsedLines = new List<Line>();
            foreach (var db in lines)
            {
                bool used = false;
                var line = db as Line;
                var spt = line.StartPoint;
                var ept = line.EndPoint;
                var spt0 = new Point3d(spt.X, spt.Y, 0);
                var ept0 = new Point3d(ept.X, ept.Y, 0);
                if (spt0.DistanceTo(centerPt) < tolerance)
                {
                    pts.Add(spt0);
                    used = true;
                }
                if (ept0.DistanceTo(centerPt) < tolerance)
                {
                    pts.Add(ept0);
                    used = true;
                }
                if(!used)
                {
                    unUsedLines.Add(line);//保存没处理的线
                }
            }
            foreach(var line in unUsedLines)
            {
                var sptex = new Point3dEx(line.StartPoint);
                var eptex = new Point3dEx(line.EndPoint);
                sprayIn.PtDic[sptex].Remove(eptex);
                sprayIn.PtDic[eptex].Remove(sptex);
                var pts1 = new List<Point3d>();
                pts1.Add(line.StartPoint);
                pts1.Add(line.EndPoint);
                var pt = line.GetClosestPointTo(centerPt, false);
                if(pt.DistanceTo(centerPt) < 1)//两点分离
                {
                    LineTools.AddPtDic(sprayIn, pts1, centerPt);
                }
                else
                {
                    LineTools.AddPtDic(sprayIn, new List<Point3d>() { pt }, centerPt);
                }
            }
            return pts;
        }
    }
}
