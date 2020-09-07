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
        public static Point3d GetMidPt(this Point3d pt1, Point3d pt2)
        {
            return pt1 + pt1.GetVectorTo(pt2) * 0.5;
        }
        public static Point3d GetProjectPtOnLine(this Point3d outerPt, Point3d startPt,Point3d endPt)
        {
            Vector3d firstVec = startPt.GetVectorTo(endPt);
            Vector3d secondVec = startPt.GetVectorTo(outerPt);
            double angle = firstVec.GetAngleTo(secondVec);
            double distance = Math.Cos(angle) * secondVec.Length;
            return startPt + firstVec.GetNormal().MultiplyBy(distance);
        }
        public static bool IsParallelUncollinear(this Line firstEnt, Line secondEnt)
        {
            Vector3d firstVec = firstEnt.StartPoint.GetVectorTo(firstEnt.EndPoint);
            Vector3d secondVec = secondEnt.StartPoint.GetVectorTo(secondEnt.EndPoint);
            if (firstVec.IsParallelToEx(secondVec))
            {
                Vector3d otherVec = firstEnt.StartPoint.GetVectorTo(secondEnt.StartPoint);
                double angle = firstVec.GetAngleTo(otherVec);
                angle = angle / Math.PI * 180.0;
                angle %= 180.0;
                return !(Math.Abs(angle - 0.0) <= 1.0 || Math.Abs(angle - 180.0) <= 1.0);
            }
            return false;
        }
    }
}
