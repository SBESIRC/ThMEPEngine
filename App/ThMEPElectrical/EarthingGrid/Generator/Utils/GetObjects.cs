using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;
using AcHelper;
using Linq2Acad;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using NetTopologySuite.Geometries;

namespace ThMEPElectrical.EarthingGrid.Generator.Utils
{
    class GetObjects
    {
        /// <summary>
        /// 获得多个所选多边形的中点
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static HashSet<Point3d> GetCenters(AcadDatabase acdb)
        {
            var result = Active.Editor.GetSelection();
            var points = new HashSet<Point3d>();
            if (result.Status == PromptStatus.OK)
            {
                foreach (var obj in result.Value.GetObjectIds())
                {
                    points.Add(acdb.Element<Entity>(obj).GeometricExtents.CenterPoint());
                }
            }
            return points;
        }

        /// <summary>
        /// 获取线集中每条线起始点的平均值
        /// </summary>
        public static Point3d GetLinesCenter(List<Tuple<Point3d, Point3d>> lines)
        {
            double xSum = 0;
            double ySum = 0;
            foreach (var line in lines)
            {
                xSum += line.Item1.X;
                ySum += line.Item1.Y;
            }
            return new Point3d(xSum / lines.Count, ySum / lines.Count, 0);
        }


        /// <summary>
        /// 获取多个多边形
        /// </summary>
        /// <param name="acdb"></param>
        /// <returns></returns>
        public static List<Polyline> GetPolylines(AcadDatabase acdb)
        {
            var tvs = new List<TypedValue>();
            tvs.Add(new TypedValue((int)DxfCode.Start, RXClass.GetClass(typeof(Polyline)).DxfName));
            var sf = new SelectionFilter(tvs.ToArray());
            var result = Active.Editor.GetSelection(sf);
            if (result.Status != PromptStatus.OK)
            {
                return null;
            }
            return result.Value.GetObjectIds().Select(o => acdb.Element<Polyline>(o)).ToList();
        }

        /// <summary>
        /// find the nearest point by correct direction
        /// </summary>
        /// <param name="fromPt">起始点</param>
        /// <param name="aimDirection">目标方向</param>
        /// <param name="toPts">目标点所在的点集</param>
        /// <param name="findDegree">查找范围容差（容差越小越准确，也越难出结果）</param>
        /// <param name="constrain">限制最长链接距离</param>
        /// <returns>要连接的点</returns>
        public static Point3d GetRangePointByDirection(Point3d fromPt, Vector3d aimDirection, HashSet<Point3d> toPts, double findDegree = Math.PI / 12, double constrain = 90000)
        {
            Point3d toPt = fromPt;
            double minDis = double.MaxValue;
            foreach (Point3d curPt in toPts)
            {
                double curRotate = aimDirection.GetAngleTo(curPt - fromPt);
                double curDis = fromPt.DistanceTo(curPt);
                if (curRotate < findDegree && curDis < constrain && curDis > 200)
                {
                    if (curDis < minDis)
                    {
                        toPt = curPt;
                        minDis = curDis;
                    }
                }
            }
            return toPt;
        }

        /// <summary>
        /// 获得点集中距离基准点最近的那个点
        /// </summary>
        public static Point3d GetMinDisPt(Point3d basePt, List<Point3d> points)
        {
            double minDis = double.MaxValue;
            var minDisPt = basePt;
            double curDis;
            foreach (var curPt in points)
            {
                curDis = curPt.DistanceTo(basePt);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    minDisPt = curPt;
                }
            }
            return minDisPt;
        }

        /// <summary>
        /// 获取两个点的中心
        /// </summary>
        public static Point3d GetCenterPt(Point3d ptA, Point3d ptB)
        {
            return new Point3d((ptA.X + ptB.X) / 2, (ptA.Y + ptB.Y) / 2, 0);
        }

        /// <summary>
        /// Get Closest Point By Direction On a Polyline
        /// </summary>
        /// <param name="fromPt">基准点</param>
        /// <param name="vector">方向</param>
        /// <param name="range">射线距离</param>
        /// <param name="polyline">穿过的多边形</param>
        /// <returns>返回最近的相交点</returns>
        public static Point3d GetClosestPointByDirection(Point3d fromPt, Vector3d vector, double range, Polyline polyline)
        {
            Line line = new Line(fromPt, fromPt + vector * range);
            Point3dCollection intersectPts = new Point3dCollection();
            Plane plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            line.IntersectWith(polyline, Intersect.OnBothOperands, plane, intersectPts, IntPtr.Zero, IntPtr.Zero);
            plane.Dispose();
            if (intersectPts.Count == 0)
            {
                return fromPt;
            }
            else
            {
                double minDis = double.MaxValue;
                double curDis;
                Point3d toPt = fromPt;
                foreach (Point3d pt in intersectPts)
                {
                    curDis = pt.DistanceTo(fromPt);
                    if (curDis < minDis && curDis > 200)
                    {
                        minDis = curDis;
                        toPt = pt;
                    }
                }
                return toPt;
            }
        }

        public static Point3d GetPointByDirectionB(Point3d fromPt, Vector3d aimDirection, HashSet<Point3d> basePts, double tolerance = Math.PI / 12, double constrain = 9000)
        {
            Point3d ansPt = fromPt;
            double minDis = double.MaxValue;
            foreach (Point3d curPt in basePts)
            {
                double curRotate = aimDirection.GetAngleTo(curPt - fromPt);
                double curDis = fromPt.DistanceTo(curPt);
                if (curRotate < tolerance && curDis < constrain && curDis > 200)
                {
                    if (curDis < minDis)
                    {
                        ansPt = curPt;
                        minDis = curDis;
                    }
                }
            }
            return ansPt;
        }


        public static void GetCloestLineOfPolyline(Polyline polyline, Point3d basePt, ref Vector3d vector)
        {
            int n = polyline.NumberOfVertices;
            if (n < 2)
            {
                return;
            }
            double minDis = double.MaxValue;
            Line ansLine = null;
            for (int i = 0; i < n; ++i)
            {
                //Vector3d curVec = polyline.GetPoint3dAt(i) - polyline.GetPoint3dAt((i + 1) % n);
                Line curLine = new Line(polyline.GetPoint3dAt(i), polyline.GetPoint3dAt((i + 1) % n));
                double curDis = basePt.DistanceTo(curLine.GetClosestPointTo(basePt, false));
                if (curDis < minDis)
                {
                    minDis = curDis;
                    ansLine = curLine;
                }
            }
            vector = basePt.DistanceTo(ansLine.StartPoint) < basePt.DistanceTo(ansLine.EndPoint) ? ansLine.StartPoint - ansLine.EndPoint : ansLine.EndPoint - ansLine.StartPoint;
        }

        /// <summary>
        /// 获取一个长方形的评分切割线
        /// 切割较长的方向
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public static Tuple<Point3d, Point3d> GetBisectorOfRectangle(Polyline rectangle)
        {
            Point3d centerA = GetCenterPt(rectangle.GetPoint3dAt(0), rectangle.GetPoint3dAt(1));
            Point3d centerB = GetCenterPt(rectangle.GetPoint3dAt(1), rectangle.GetPoint3dAt(2));
            Point3d centerC = GetCenterPt(rectangle.GetPoint3dAt(2), rectangle.GetPoint3dAt(3));
            Point3d centerD = GetCenterPt(rectangle.GetPoint3dAt(3), rectangle.GetPoint3dAt(0));
            return centerA.DistanceTo(centerC) > centerB.DistanceTo(centerD) ?
                new Tuple<Point3d, Point3d>(centerB, centerD) : new Tuple<Point3d, Point3d>(centerA, centerC);
        }


    }
}
