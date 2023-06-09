﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.Common
{
    public static class PointVectorUtil
    {
        /// <summary>
        /// 点投影到线
        /// </summary>
        /// <param name="point"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Point3d PointToLine(this Point3d point, Line line)
        {
            Point3d lineSp = line.StartPoint;
            Vector3d lineDirection = (line.EndPoint - line.StartPoint).GetNormal();
            return point.PointToLine(lineSp,lineDirection);
        }
        /// <summary>
        /// 点投影到线
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lineSp"></param>
        /// <param name="lineDirection"></param>
        /// <returns></returns>
        public static Point3d PointToLine(this Point3d point, Point3d lineSp, Vector3d lineDirection)
        {
            var vect = point - lineSp;
            var dot = vect.DotProduct(lineDirection);
            return lineSp + lineDirection.MultiplyBy(dot);
        }
        /// <summary>
        /// 获取点list的中心点
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Point3d PointsAverageValue(List<Point3d> points) 
        {
            Point3d avgPoint = new Point3d(0, 0, 0);
            Vector3d vector = new Vector3d();
            foreach (var point in points) 
            {
                var tempVector = point - avgPoint;
                vector += tempVector;
            }
            vector = vector / points.Count;
            avgPoint = avgPoint + vector;
            return avgPoint;
        }
        /// <summary>
        /// 判断点在直线上
        /// +---------------------------+
        ///     outTolerance|            
        ///                 +
        /// </summary>
        /// <param name="point"></param>
        /// <param name="linePoint"></param>
        /// <param name="lineDirection"></param>
        /// <param name="outTolerance"></param>
        /// <returns></returns>
        public static bool PointInLine(this Point3d point, Point3d linePoint, Vector3d lineDirection, double outTolerance = 1) 
        {
            var prjPoint = point.PointToLine(linePoint, lineDirection);
            if (prjPoint.DistanceTo(point) > outTolerance)
                return false;
            return true;
        }
        /// <summary>
        /// 点是否在线段上
        ///   extendTolerance
        /// + | --------------------------- | +
        ///       outTolerance|             extendTolerance
        ///                   +
        /// </summary>
        /// <param name="point"></param>
        /// <param name="line"></param>
        /// <param name="extendTolerance"></param>
        /// <param name="outTolerance"></param>
        /// <returns></returns>
        public static bool PointInLineSegment(this Point3d point, Line line, double extendTolerance = 1, double outTolerance = 1) 
        {
            return point.PointInLineSegment(line.StartPoint, line.EndPoint, extendTolerance, outTolerance);
        }
        /// <summary>
        /// 点是否在线段上
        ///   extendTolerance
        /// + | --------------------------- | +
        ///       outTolerance|             extendTolerance
        ///                   +
        /// </summary>
        /// <param name="point"></param>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="extendTolerance"></param>
        /// <param name="outTolerance"></param>
        /// <returns></returns>
        public static bool PointInLineSegment(this Point3d point, Point3d startPoint,Point3d endPoint,double extendTolerance=1,double outTolerance=1)
        {
            extendTolerance = extendTolerance < 0?0:extendTolerance;
            outTolerance = outTolerance<0? 0:extendTolerance;
            var lineDir = (endPoint - startPoint).GetNormal();
            var prjPoint = point.PointToLine(startPoint, lineDir);
            if (prjPoint.DistanceTo(point) > outTolerance)
                return false;
            if (prjPoint.DistanceTo(startPoint) <= extendTolerance || prjPoint.DistanceTo(endPoint) <= extendTolerance)
                return true;
            var sDir = (point - startPoint).GetNormal();
            var eDir = (point - endPoint).GetNormal();
            var dot = sDir.DotProduct( eDir);
            if (dot < 0)
                return true;
            return false;
        }
        /// <summary>
        /// 一组点根据线进行排序
        /// </summary>
        /// <param name="orderPoints"></param>
        /// <param name="line"></param>
        /// <param name="isDescending"></param>
        /// <returns></returns>
        public static List<Point3d> PointsOrderByLine(List<Point3d> orderPoints,Line line, bool isDescending = false) 
        {
            var lineSp = line.StartPoint;
            var lineEp = line.EndPoint;
            var lineDir = (lineEp - lineSp).GetNormal();
            return PointsOrderByDirection(orderPoints, lineDir, isDescending, lineSp);
        }
        /// <summary>
        /// 一组点根据方向进行排序
        /// </summary>
        /// <param name="orderPoints"></param>
        /// <param name="directon"></param>
        /// <param name="isDescending"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static List<Point3d> PointsOrderByDirection(List<Point3d> orderPoints,Vector3d directon,bool isDescending, Point3d origin = new Point3d()) 
        {
            Dictionary<Point3d, double> resPoints = PointsOrderByDirection(orderPoints, directon, origin);
            if (isDescending) 
            {
                return resPoints.OrderByDescending(c => c.Value).Select(c => c.Key).ToList();
            }
            return resPoints.OrderBy(c=>c.Value).Select(c => c.Key).ToList();
        }

        /// <summary>
        /// 一组点根据方向进行排序
        /// </summary>
        /// <param name="orderPoints"></param>
        /// <param name="directon"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static Dictionary<Point3d, double> PointsOrderByDirection(List<Point3d> orderPoints, Vector3d directon, Point3d origin = new Point3d())
        {
            Dictionary<Point3d, double> resPoints = new Dictionary<Point3d, double>();
            if (null == orderPoints || orderPoints.Count < 1)
                return resPoints;
            foreach (var point in orderPoints)
            {
                if (resPoints.Any(c => c.Key.IsEqualTo(point, new Tolerance(1, 1))))
                    continue;
                var dot = (point - origin).DotProduct(directon);
                resPoints.Add(point, dot);
            }
            return resPoints;
        }
        /// <summary>
        /// （XOY平面）直线与直线的交点（非线段交点）
        /// </summary>
        /// <param name="firstOrigin"></param>
        /// <param name="firstDirection"></param>
        /// <param name="secondOrigin"></param>
        /// <param name="secondDirection"></param>
        /// <param name="intersectionPoint"></param>
        /// <returns>
        ///    0 （共面平行）无交点
        ///    1 （共面非平行）一个交点
        ///    2 （共面且共线）共线无数交点
        /// </returns>
        public static int LineIntersectionLine(Point3d firstOrigin,Vector3d firstDirection,Point3d secondOrigin,Vector3d secondDirection,out Point3d intersectionPoint) 
        {
            //直线与直线的关系，异面（无交点，这里不考虑），共面平行不共线（无交点），共面相交（一个交点），共面平行且共线（无数交点）
            intersectionPoint = new Point3d(0,0,0);

            var P0 = firstOrigin;
            var D0 = firstDirection.GetNormal();
            var P1 = secondOrigin;
            var D1 = secondDirection.GetNormal();

            var E = P1 - P0;
            var kross = D0.X * D1.Y - D0.Y * D1.X;
            var sqrKross = kross * kross;
            var sqrLen0 = D0.X * D0.X + D0.Y * D0.Y;
            var sqrLen1 = D1.X * D1.X + D1.Y * D1.Y;
            var sqlEpsilon = 0.00001;
            //有一个交点
            if (sqrKross > sqlEpsilon * sqrLen0 * sqrLen1)
            {
                var s = (E.X * D1.Y - E.Y * D1.X) / kross;
                intersectionPoint = P0 + s * D0;
                return 1;
            }
            //如果线是平行的
            var sqrLenE = E.X * E.X + E.Y * E.Y;
            kross = E.X * D0.Y - E.Y * D0.X;
            sqrKross = kross * kross;

            var value = sqlEpsilon * sqrLen0 * sqrLenE;
            if (Math.Abs(sqrKross - value) > 1 && sqrKross > value)
                return 0;
            return 2;
        }
        
    }
}
