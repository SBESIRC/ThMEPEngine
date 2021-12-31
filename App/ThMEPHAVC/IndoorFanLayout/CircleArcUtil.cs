using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using ThCADExtension;

namespace ThMEPHVAC.IndoorFanLayout
{
    public static class CircleArcUtil
    {
        public static Dictionary<Point3d, double> PointOderByArcAngle(List<Point3d> targetPoints,Arc arc) 
        {;
            var xVector= arc.Ecs.CoordinateSystem3d.Xaxis;
            return PointOderByArcAngle(targetPoints, arc.Center, arc.Normal, xVector);
        }
        public static Dictionary<Point3d, double> PointOderByArcAngle(List<Point3d> targetPoints, Point3d arcCenter,Vector3d arcNormal,Vector3d xVector)
        {
            var pointAngles = new Dictionary<Point3d, double>();
            foreach (var point in targetPoints)
            {
                var angle = xVector.GetAngleTo((point - arcCenter).GetNormal(), arcNormal);
                pointAngles.Add(point, angle);
            }
            return pointAngles;
        }
        public static int ArcIntersectLineSegment(this Arc arc,Line lineSegment, out List<Point3d> intersectPoints, double precision = 1) 
        {
            intersectPoints = new List<Point3d>();
            var circle = new Circle(arc.Center, arc.Normal, arc.Radius);
            var circleInter = circle.CircleIntersectLineSegment(lineSegment, out List<Point3d> tempInterPoints, precision);
            if (null == tempInterPoints || tempInterPoints.Count < 1)
                return circleInter;
            foreach (var point in tempInterPoints) 
            {
                if (PointRelationArc(point, arc, precision) == 1)
                    intersectPoints.Add(point);
            }
            return circleInter;
        }
        public static Arc ArcIntersectArc(Arc arc1,Arc arc2) 
        {
            //两个同心，同半径的圆弧，求共弧部分
            if (arc1.Center.DistanceTo(arc2.Center) > 1)
                return null;
            if (Math.Abs(arc1.Radius - arc2.Radius) > 1)
                return null;
            var arc1SAngle = arc1.StartAngle;
            var arc1EAngle = arc1.EndAngle;
            var arc2SAngle = arc2.StartAngle;
            var arc2EAngle = arc2.EndAngle;
            if (arc1SAngle > arc2EAngle || arc1EAngle<arc2SAngle)
                return null;
            double startAngle = arc1SAngle >= arc2SAngle?arc1SAngle:arc2SAngle;
            double endAngle = arc1EAngle >= arc2EAngle ? arc2EAngle : arc1EAngle;
            return new Arc(arc1.Center, arc1.Normal, arc1.Radius, startAngle, endAngle);
        }
        public static Point3d PointToArc(Point3d point,Arc arc) 
        {
            var vector = (point - arc.Center).GetNormal();
            return arc.Center + vector.MultiplyBy(arc.Radius);
        }
        public static Point3d PointToCircle(Point3d point, Circle circle) 
        {
            var vector = (point - circle.Center).GetNormal();
            return circle.Center + vector.MultiplyBy(circle.Radius);
        }
        /// <summary>
        /// 点和圆弧的关系
        /// </summary>
        /// <param name="point"></param>
        /// <param name="arc"></param>
        /// <param name="precision"></param>
        /// <returns>
        /// -1异面，0 外部，1圆弧上，2圆弧内部
        /// </returns>
        public static int PointRelationArc(Point3d point, Arc arc, double precision = 1,double anglePrecision=Math.PI/180) 
        {
            var center = arc.Center;
            var normal = arc.Normal;
            var xVector = arc.Ecs.CoordinateSystem3d.Xaxis;
            var circleRadius = arc.Radius;
            if (!PointInFace(point, center, normal, precision))
                return -1;
            var prjPoint = point.PointToFace(center, normal);
            var dis = center.DistanceTo(prjPoint);
            if (Math.Abs(dis - circleRadius) >  precision)
                return 0;
            var pointVector = (point - arc.Center).GetNormal();
            var angle = xVector.GetAngleTo(pointVector, normal);
            var sPoint = arc.StartPoint;
            var sAngle = arc.StartAngle;
            var eAngle = arc.EndAngle;
            if (angle > sAngle && angle < eAngle) 
            {
                return circleRadius - dis < precision ? 1 : 2;
            }
            else if (Math.Abs(angle - sAngle) < anglePrecision ||Math.Abs(angle - eAngle)<anglePrecision) 
            {
                return circleRadius - dis < precision ? 1 : 2;
            }
            return 0;
        }
        /// <summary>
        /// 线段和圆求交点
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="lineSegment"></param>
        /// <param name="intersectPoints">out 相交点（个数0，1，2）</param>
        /// <returns>
        /// 线段所在直线和圆关系（具体的相交点要根据返回的相交点个数判断）
        /// -1异面，0外部，1相切，2相割,3内部
        /// </returns>
        public static int CircleIntersectLineSegment(this Circle circle,Line lineSegment, out List<Point3d> intersectPoints, double precision = 1) 
        {
            intersectPoints = new List<Point3d>();
            var lineDir = (lineSegment.EndPoint - lineSegment.StartPoint).GetNormal();
            var relation = CircleIntersectLine(circle, lineSegment.StartPoint, lineDir, out List<Point3d> interPoints, precision);
            if (interPoints != null || interPoints.Count > 0) 
            {
                foreach (var point in interPoints) 
                {
                    if (null == point)
                        continue;
                    if (point.PointInLineSegment(lineSegment, precision, precision))
                        intersectPoints.Add(point);
                }
            }
            return relation;
        }
        /// <summary>
        /// 点和圆的关系
        /// </summary>
        /// <param name="point"></param>
        /// <param name="circle"></param>
        /// <param name="precision"></param>
        /// <returns>
        /// -1异面，0 外部，1圆上，2圆内部
        /// </returns>
        public static int PointRelationCircle(Point3d point, Circle circle, double precision = 1) 
        {
            var center = circle.Center;
            var normal = circle.Normal;
            var circleRadius = circle.Radius;
            if (!PointInFace(point, center, normal, precision))
                return -1;
            var prjPoint = point.PointToFace(center, normal);
            var dis = center.DistanceTo(prjPoint);
            if (dis > (circleRadius - precision))
                return 0;
            if (circleRadius - dis < precision)
                return 1;
            return 2;
        }
        /// <summary>
        /// 直线和圆求交点（同一平面的，内部不在进行是否在同一平面的判断）
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="lineOrigin"></param>
        /// <param name="lineDirection"></param>
        /// <param name="intersectPoints"></param>
        /// <param name="precision"></param>
        /// <returns>
        /// -1异面, 0相离（无交点），1相切（一个交点），2相割（2个交点）
        /// </returns>
        public static int CircleIntersectLine(Circle circle, Point3d lineOrigin, Vector3d lineDirection, out List<Point3d> intersectPoints, double precision = 1) 
        {
            intersectPoints = new List<Point3d>();
            return CircleIntersectLine(circle.Center, circle.Normal, circle.Radius, lineOrigin, lineDirection, out intersectPoints, precision);
        }
        /// <summary>
        /// 直线和圆求交点（同一平面的）
        /// </summary>
        /// <param name="circleCenter">圆心</param>
        /// <param name="circleNormal">圆所在面的法相</param>
        /// <param name="circleRadius">圆半径</param>
        /// <param name="lineOrigin">直线上一个点</param>
        /// <param name="lineDirection">直线方向</param>
        /// <param name="intersectPoints">out List 相交点</param>
        /// <returns>
        /// -1异面, 0相离（无交点），1相切（一个交点），2相割（2个交点）
        /// </returns>
        public static int CircleIntersectLine(Point3d circleCenter,Vector3d circleNormal,double circleRadius,Point3d lineOrigin,Vector3d lineDirection,out List<Point3d> intersectPoints,double precision= 1) 
        {
            int relation = -1;
            //同一平面的直线和圆求交点
            intersectPoints = new List<Point3d>();
            //这里使用了取巧的方法判断线和面是否异面，没有写线是否在面内的算法，这里先使用这种线上两个点和面的关系进行判断
            var ep = lineOrigin + lineDirection.MultiplyBy(Math.Max(100+ precision, 3 * precision));
            var spInFace = PointInFace(lineOrigin, circleCenter, circleNormal, precision);
            var epInFace = PointInFace(ep, circleCenter, circleNormal, precision);
            if (!spInFace || !epInFace)
                return relation;//异面不进行处理
            var prjPoint = circleCenter.PointToLine(lineOrigin, lineDirection);
            var dis = prjPoint.DistanceTo(circleCenter);
            if (dis > (circleRadius - precision))
                relation = 0;
            else if ((circleRadius - dis) < precision)
            {
                //相切
                relation = 1;
                intersectPoints.Add(prjPoint);
            }
            else
            {
                //相割
                relation = 2;
                var moveDis = Math.Sqrt(circleRadius * circleRadius - dis * dis);
                var point1 = prjPoint + lineDirection.MultiplyBy(moveDis);
                var point2 = prjPoint - lineDirection.MultiplyBy(moveDis);
                intersectPoints.Add(point1);
                intersectPoints.Add(point2);
            }
            return relation;
        }
        /// <summary>
        /// 判断点是否再面上
        /// </summary>
        /// <param name="point"></param>
        /// <param name="faceOrigin"></param>
        /// <param name="faceNormal"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static bool PointInFace(Point3d point, Point3d faceOrigin, Vector3d faceNormal,double precision=1) 
        {
            var prjPoint = point.PointToFace(faceOrigin, faceNormal);
            return point.DistanceTo(prjPoint) < precision;
        }
    }
}
