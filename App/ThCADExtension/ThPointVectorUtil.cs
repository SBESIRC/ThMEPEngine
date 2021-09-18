using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace ThCADExtension
{
    public static class ThPointVectorUtil
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
            return point.PointToLine(lineSp, lineDirection);
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
            return prjPoint.DistanceTo(point) <= outTolerance;
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
        public static bool PointInLineSegment(this Point3d point, Point3d startPoint, Point3d endPoint, double extendTolerance = 1, double outTolerance = 1)
        {
            extendTolerance = extendTolerance < 0 ? 0 : extendTolerance;
            outTolerance = outTolerance < 0 ? 0 : extendTolerance;
            var lineDir = (endPoint - startPoint).GetNormal();
            var prjPoint = point.PointToLine(startPoint, lineDir);
            if (prjPoint.DistanceTo(point) > outTolerance)
                return false;
            if (prjPoint.DistanceTo(startPoint) <= extendTolerance || prjPoint.DistanceTo(endPoint) <= extendTolerance)
                return true;
            var sDir = (point - startPoint).GetNormal();
            var eDir = (point - endPoint).GetNormal();
            var dot = sDir.DotProduct(eDir);
            return dot < 0;
        }
        /// <summary>
        /// 一组点根据线进行排序
        /// </summary>
        /// <param name="orderPoints"></param>
        /// <param name="line"></param>
        /// <param name="isDescending"></param>
        /// <returns></returns>
        public static List<Point3d> PointsOrderByLine(List<Point3d> orderPoints, Line line, bool isDescending = false)
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
        public static List<Point3d> PointsOrderByDirection(List<Point3d> orderPoints, Vector3d directon, bool isDescending, Point3d origin = new Point3d())
        {
            Dictionary<Point3d, double> resPoints = PointsOrderByDirection(orderPoints, directon, origin);
            if (isDescending)
            {
                return resPoints.OrderByDescending(c => c.Value).Select(c => c.Key).ToList();
            }
            return resPoints.OrderBy(c => c.Value).Select(c => c.Key).ToList();
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
        public static Point3d PointToFace(this Point3d point,Point3d faceOrigin,Vector3d faceNormal) 
        {
            var vector = point - faceOrigin;
            return point - faceNormal.MultiplyBy(faceNormal.DotProduct(vector));
        }
    }
}
