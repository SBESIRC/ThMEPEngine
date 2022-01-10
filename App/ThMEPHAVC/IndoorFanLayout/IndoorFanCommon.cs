using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPHVAC.IndoorFanLayout.DataEngine;

namespace ThMEPHVAC.IndoorFanLayout
{
    class IndoorFanCommon
    {
        public const double FanSpaceMinDistance = 800;
        public const double RoomBufferOffSet = -500.0;
        public const double ReducingLength = 150.0;
        public static Point3d PolylinCenterPoint(Polyline polyline) 
        {
            var allPoints = GetPolylinePoints(polyline);
            return ThPointVectorUtil.PointsAverageValue(allPoints);
        }
        public static bool RoomLoadTableReadLoad(Table roomTable,bool isCold, out double roomArea, out double roomLoad)
        {
            var roomLoadTable = new LoadTableRead();
            roomLoad = 0.0;
            roomArea = 0.0;
            bool haveValue = roomLoadTable.ReadRoomLoad(roomTable, out string roomAreaStr, out string roomLoadStr);
            if (!haveValue)
                return false;
            double.TryParse(roomAreaStr, out roomArea);
            var spliteLoads = roomLoadStr.Split('/').ToList();
            if (spliteLoads.Count < 2)
                return false;
            var roomCoolLoadStr = spliteLoads[0];
            var roomHotLoadStr = spliteLoads[1];
            if (isCold)
            {
                if (string.IsNullOrEmpty(roomCoolLoadStr) || roomCoolLoadStr.Contains("-"))
                    return false;
                double.TryParse(roomCoolLoadStr, out roomLoad);
            }
            else
            {
                if (string.IsNullOrEmpty(roomHotLoadStr) || roomHotLoadStr.Contains("-"))
                    return false;
                double.TryParse(roomHotLoadStr, out roomLoad);
            }
            return true;
        }
        public static string GetEffectiveBlkByName(BlockReference blockReference)
        {
            using (var db = AcadDatabase.Active())
            {
                if (blockReference.BlockTableRecord.IsNull)
                {
                    return string.Empty;
                }
                string name;
                if (blockReference.DynamicBlockTableRecord.IsValid)
                {
                    name = db.Element<BlockTableRecord>(blockReference.DynamicBlockTableRecord).Name;
                }
                else
                {
                    name = blockReference.Name;
                }
                return name;
            }
        }
        public static Polyline PointsAABBByVector(List<Point3d> points,Vector3d xVector,Vector3d normal) 
        {
            var yVector = normal.CrossProduct(xVector).GetNormal();
            var orderXPoints = ThPointVectorUtil.PointsOrderByDirection(points, xVector, false);
            var orderYPoints = ThPointVectorUtil.PointsOrderByDirection(points, yVector, false);
            var xMin = orderXPoints.First();
            var xMax = orderXPoints.Last();
            var yMin = orderYPoints.First();
            var yMax = orderYPoints.Last();
            var minPoint = ThPointVectorUtil.PointToFace(xMin, yMin, yVector);
            var maxPoint = ThPointVectorUtil.PointToFace(xMax, yMax, yVector);
            var pt1 = minPoint;
            var pt2 = ThPointVectorUtil.PointToFace(minPoint, maxPoint, yVector);
            var pt3 = maxPoint;
            var pt4 = ThPointVectorUtil.PointToFace(maxPoint, minPoint, yVector);
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt2.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt3.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(0, pt4.ToPoint2D(), 0, 0, 0);
            polyline.Closed = true;
            return polyline;
        }
        public static List<Curve> GetPolylineCurves(Polyline polyline)
        {
            var allCurves = new DBObjectCollection();
            polyline.Explode(allCurves);
            return allCurves.OfType<Curve>().ToList();
        }
        public static List<Point3d> GetPolylinePoints(Polyline polyline)
        {
            var resPoints = new List<Point3d>();
            if (null == polyline)
                return resPoints;
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var point = polyline.GetPoint3dAt(i);
                if (resPoints.Any(c => c.DistanceTo(point) < 1))
                    continue;
                resPoints.Add(point);
            }
            return resPoints;
        }
        /// <summary>
        /// 线的方向是否和方向垂直
        /// </summary>
        /// <param name="line"></param>
        /// <param name="dir"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static bool LineIsVerticalDir(Line line, Vector3d dir, double precision = Math.PI / 180.0)
        {
            return DirIsVerticalDir((line.EndPoint - line.StartPoint).GetNormal(), dir, precision);
        }
        /// <summary>
        /// 线的方向是否和方向平行
        /// </summary>
        /// <param name="line"></param>
        /// <param name="dir"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static bool LineIsParallelDir(Line line, Vector3d dir, double precision = Math.PI / 180.0)
        {
            return DirIsParallelDir((line.EndPoint - line.StartPoint).GetNormal(), dir, precision);
        }
        /// <summary>
        /// 线的方向是否和方向垂直
        /// </summary>
        /// <param name="dir1"></param>
        /// <param name="dir2"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static bool DirIsVerticalDir(Vector3d dir1, Vector3d dir2, double precision = Math.PI / 180.0)
        {
            var angle = dir1.GetAngleTo(dir2);
            angle %= Math.PI;
            return Math.Abs(Math.PI / 2.0 - angle) < precision;
        }
        public static List<Line> LineBreakByPoints(Line beBreakLine, List<Point3d> points)
        {
            var retLines = new List<Line>();
            var linePoints = new List<Point3d>() { beBreakLine.StartPoint, beBreakLine.EndPoint };
            foreach (var point in points)
            {
                if (linePoints.Any(c => c.DistanceTo(point) < 1))
                    continue;
                if (point.PointInLineSegment(beBreakLine, 0.1, 0.1))
                    linePoints.Add(point);
            }
            linePoints = linePoints.OrderBy(c => c.DistanceTo(beBreakLine.StartPoint)).ToList();
            for (int i = 0; i < linePoints.Count - 1; i++)
            {
                var sp = linePoints[i];
                var ep = linePoints[i + 1];
                retLines.Add(new Line(sp, ep));
            }
            return retLines;
        }
        /// <summary>
        /// 线的方向是否和方向平行
        /// </summary>
        /// <param name="dir1"></param>
        /// <param name="dir2"></param>
        /// <param name="precision"></param>
        /// <returns></returns>
        public static bool DirIsParallelDir(Vector3d dir1, Vector3d dir2, double precision = Math.PI / 180.0)
        {
            var angle = dir1.GetAngleTo(dir2);
            angle %= Math.PI;
            return angle < precision || angle > (Math.PI - precision);
        }
        public static int FindIntersection(Line line1, Line line2, out List<Point3d> intersectionPoints) 
        {
            intersectionPoints = new List<Point3d>();
            var lineDir = (line1.EndPoint - line1.StartPoint).GetNormal();
            var yAxis = line1.Normal.CrossProduct(lineDir);
            var transform = Matrix3d.AlignCoordinateSystem(line1.StartPoint, lineDir, yAxis, line1.Normal, Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis);
            var line1Clone = line1.GetTransformedCopy(transform) as Line;
            var line2Clone = line2.GetTransformedCopy(transform) as Line;
            var ret = FindIntersection(line1Clone.StartPoint, line1Clone.EndPoint, line2Clone.StartPoint, line2Clone.EndPoint, out List<Point3d> tempPoints);
            if (null != tempPoints && tempPoints.Count > 0)
            {
                foreach (var point in tempPoints) 
                {
                    var tempPoint = point.TransformBy(transform.Inverse());
                    intersectionPoints.Add(tempPoint);
                }
            }
            return ret;

        }
        /// <summary>
        /// 线段求共线部分(精确共线)
        /// </summary>
        /// <param name="s0"></param>
        /// <param name="e0"></param>
        /// <param name="s1"></param>
        /// <param name="e1"></param>
        /// <param name="intersectionPoints">
        /// out 返回共线点坐标
        /// </param>
        /// <returns>
        /// -1不相交，1端点相交，2有个共线部分
        /// </returns>
        static int FindIntersection(Point3d s0, Point3d e0, Point3d s1, Point3d e1, out List<Point3d> intersectionPoints)
        {
            intersectionPoints = new List<Point3d>();
            double EPS3 = 1.0e-3f;
            var P0 = s0;
            var D0 = e0 - s0;
            var P1 = s1;
            var D1 = e1 - s1;

            var E = P1 - P0;
            var kross = D0.X * D1.Y - D0.Y * D1.X;
            var sqrKross = kross * kross;
            var sqrLen0 = D0.X * D0.X + D0.Y * D0.Y;
            var sqrLen1 = D1.X * D1.X + D1.Y * D1.Y;
            var sqlEpsilon = EPS3 * EPS3;

            //有一个交点
            if (sqrKross > sqlEpsilon * sqrLen0 * sqrLen1)
            {
                var s = (E.X * D1.Y - E.Y * D1.X) / kross;
                if (Math.Abs(s) > EPS3 && Math.Abs(s - 1) > EPS3 && (s < 0 || s > 1))
                    //线段没有交点
                    return -1;
                var t = (E.X * D0.Y - E.Y * D0.X) / kross;
                if ( Math.Abs(t) > EPS3 && Math.Abs(t - 1) > EPS3 && (t < 0 || t > 1))
                    return -1;
                intersectionPoints.Add(P0 + s * D0);
                return 1;
            }
            //如果线是平行的
            var sqrLenE = E.X * E.X + E.Y * E.Y;
            kross = E.X * D0.Y - E.Y * D0.X;
            sqrKross = kross * kross;

            var value = sqlEpsilon * sqrLen0 * sqrLenE;

            if (Math.Abs(sqrKross - value) > EPS3 && sqrKross > value)
                return -1;

            //求共线的区间
            var region0 = (D0.X * E.X + D0.Y * E.Y) / sqrLen0;
            var region1 = region0 + (D0.X * D1.X + D0.Y * D1.Y) / sqrLen0;
            var smin = region0 > region1 ? region1 : region0;
            var smax = region0 > region1 ? region0 : region1;
            List<double> w = null;
            var imax = FindIntersection(0.0, 1.0, smin, smax, out w);

            for (int i = 0; i < imax; i++) 
            {
                var point = P0 + w[i] * D0;
                if (intersectionPoints.Any(c => c.DistanceTo(point) < 0.0001))
                {
                    //intersectionPoints.Add(point);
                    continue;
                }
                intersectionPoints.Add(point);
            }
                
            intersectionPoints = intersectionPoints.Distinct().ToList();
            var iCount = intersectionPoints.Count;
            System.Diagnostics.Debug.Assert(iCount == 0 || iCount == 1 || iCount == 2);

            return iCount == 0 ? -1 : iCount == 1 ? 1 :2;
        }
        /// <summary>
        /// 计算两个区间[u0,u1]和[v0,v1]的相交
        /// 其中 u0 < u1, v0 < v1
        /// </summary>
        /// <param name="u0"></param>
        /// <param name="u1"></param>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="w"></param>
        /// <returns>
        /// 0: 不相交
        /// 1: 一个交点, 即端点
        /// 2: 相交于一个区间，区间的端点保存在w[0]和w[1]中
        /// </returns>
        static int FindIntersection(double u0, double u1, double v0, double v1, out List<double> w)
        {
            w = new List<double>() { 0, 0 };

            if (u1 < v0 || u0 > v1)
                return 0;

            if (u1 > v0)
            {
                if (u0 < v1)
                {
                    if (u0 < v0) w[0] = v0; else w[0] = u0;
                    if (u1 > v1) w[1] = v1; else w[1] = u1;

                    return 2;
                }
                else
                {
                    w[0] = u0;
                    return 1;
                }
            }
            else
            {
                w[0] = u1;
                return 1;
            }
        }
    }
}
