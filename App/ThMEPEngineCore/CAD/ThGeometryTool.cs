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
        public static bool IsCollinearEx(Point3d firstSp,Point3d firstEp,
            Point3d secondSp, Point3d secondEp, double tolerance = 1.0)
        {
            Vector3d firstVec = firstSp.GetVectorTo(firstEp);
            Vector3d secondVec = secondSp.GetVectorTo(secondEp);
            if (firstVec.IsParallelToEx(secondVec, tolerance))
            {
                Vector3d otherVec;
                if (firstSp.DistanceTo(secondEp)>0.0)
                {
                    otherVec = firstSp.GetVectorTo(secondEp);
                }
                else
                {
                    otherVec = firstSp.GetVectorTo(secondSp);
                }
                double angle = firstVec.GetAngleTo(otherVec);
                angle = angle / Math.PI * 180.0;
                angle %= 180.0;
                return (Math.Abs(angle) <= tolerance || Math.Abs(angle - 180.0) <= tolerance);
            }
            return false;
        }
        /// <summary>
        /// 判断点是否再Polyline内
        /// 0.点在polyline上    1.点在polyline内    -1.点在polyline外
        /// </summary>
        /// <returns></returns>
        public static int PointInPolylineEx(this Polyline polyline, Point3d pt, double tol)
        {
            int positionIndex = -1;
            Point3d closestP = polyline.GetClosestPointTo(pt, false);
            if (Math.Abs(closestP.DistanceTo(pt)) < tol)
            {
                return 0;
            }
            Point3d minPt = polyline.GeometricExtents.MinPoint;
            Point3d maxPt = polyline.GeometricExtents.MaxPoint;
            if (pt.X < minPt.X || pt.X > maxPt.X || pt.Y < minPt.Y || pt.Y > maxPt.Y)
            {
                return -1;
            }
            List<LineSegment3d> linesegments = new List<LineSegment3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                if (polyline.GetSegmentType(i) == SegmentType.Line)
                {
                    linesegments.Add(polyline.GetLineSegmentAt(i));
                }
            }
            bool doMark = true;
            double increAng = 5.0 / 180.0 * Math.PI;
            Ray ray = new Ray();
            ray.BasePoint = pt;
            ray.UnitDir = Vector3d.XAxis;
            int count = (int)Math.Round(Math.PI * 2 / increAng);
            int index = 0;
            while (doMark)
            {
                Point3d otherPt = ray.BasePoint + ray.UnitDir.MultiplyBy(100.0);
                if (linesegments.Where(o => ThGeometryTool.IsCollinearEx(
                     ray.BasePoint, otherPt, o.StartPoint, o.EndPoint, 1.0)).Any())
                {
                    Matrix3d mt = Matrix3d.Rotation(increAng, Vector3d.ZAxis, pt);
                    ray.TransformBy(mt);
                    if (index++ == count)
                    {
                        break;
                    }
                }
                else
                {
                    Point3dCollection points = new Point3dCollection();
                    polyline.IntersectWith(ray, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
                    BeamInfo.Utils.GetObjectUtils.FilterEqualPoints(points, 1.0);
                    List<Point3d> intersectPts = new List<Point3d>();
                    foreach(Point3d ptItem in points)
                    {
                        Point3d nearPt = polyline.GetClosestPointTo(ptItem, false);
                        if (nearPt.DistanceTo(ptItem) < tol)
                        {
                            intersectPts.Add(ptItem);
                        }
                    }
                    if (intersectPts.Count > 2)
                    {
                        Matrix3d mt = Matrix3d.Rotation(increAng, Vector3d.ZAxis, pt);
                        ray.TransformBy(mt);
                        if (index++ == count)
                        {
                            break;
                        }
                    }
                    else
                    {
                        positionIndex = intersectPts.Count % 2 == 0 ? -1 : 1;
                        break;
                    }
                }
            }
            ray.Dispose();
            return positionIndex;
        }
    }
}
