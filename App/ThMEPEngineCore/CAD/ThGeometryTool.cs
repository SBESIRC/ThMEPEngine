using System;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

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
        public static bool IsParallelToEx(this Vector3d vector, Vector3d other)
        {
            double angle = vector.GetAngleTo(other) / Math.PI * 180.0;
            return (angle < ThMEPEngineCoreCommon.LOOSE_PARALLEL_ANGLE) || ((180.0 - angle) < ThMEPEngineCoreCommon.LOOSE_PARALLEL_ANGLE);
        }
        public static bool IsCollinearEx(Point3d firstSp, Point3d firstEp,
            Point3d secondSp, Point3d secondEp)
        {
            Vector3d firstVec = firstSp.GetVectorTo(firstEp);
            Vector3d secondVec = secondSp.GetVectorTo(secondEp);
            if (firstVec.IsParallelToEx(secondVec))
            {
                Vector3d otherVec;
                if (firstSp.DistanceTo(secondEp) > 0.0)
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
                return (Math.Abs(angle) <= ThMEPEngineCoreCommon.LOOSE_PARALLEL_ANGLE) 
                    || Math.Abs(angle - 180.0) <= ThMEPEngineCoreCommon.LOOSE_PARALLEL_ANGLE;
            }
            return false;
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
                if (linesegments.Where(o => IsCollinearEx(
                     ray.BasePoint, otherPt, o.StartPoint, o.EndPoint)).Any())
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
        public static bool IsIntersects(this Entity firstEnt, Entity secondEnt)
        {            
            return firstEnt.IntersectWithEx(secondEnt).Count > 0 ? true : false;
        }
        public static Point3dCollection IntersectWithEx(this Entity firstEntity, Entity secondEntity)
        {
            Point3dCollection pts = new Point3dCollection();
            Plane plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            firstEntity.IntersectWith(secondEntity, Intersect.OnBothOperands, plane, pts, IntPtr.Zero, IntPtr.Zero);
            plane.Dispose();
            return pts;
        }
        public static bool IsPointOnLine(Point3d lineSp,Point3d lineEp,Point3d outerPt)
        {
            Vector3d vec = lineSp.GetVectorTo(lineEp);
            Plane plane = new Plane(lineSp, vec);
            Matrix3d wcsToUcs = Matrix3d.WorldToPlane(plane);
            Point3d newPt=outerPt.TransformBy(wcsToUcs);
            if(Math.Abs(newPt.X)<=0.0001 && Math.Abs(newPt.Y) <= 0.0001)
            {
                if(newPt.Z>=0 && newPt.Z<= lineSp.DistanceTo(lineEp))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool IsPointInLine(Point3d lineSp, Point3d lineEp, Point3d outerPt,double tolerance=0.0)
        {
            if(IsPointOnLine(lineSp, lineEp, outerPt))
            {
                return outerPt.DistanceTo(lineSp) > tolerance && outerPt.DistanceTo(lineEp) > tolerance;
            }
            return false;
        }
        public static bool IsProjectionPtInLine(Point3d lineSp, Point3d lineEp, Point3d outerPt)
        {
            Vector3d vec = lineSp.GetVectorTo(lineEp);
            Plane plane = new Plane(lineSp, vec);
            Matrix3d wcsToUcs = Matrix3d.WorldToPlane(plane);
            Point3d newPt = outerPt.TransformBy(wcsToUcs);
            return newPt.Z >= 0 && newPt.Z <= lineSp.DistanceTo(lineEp);
        }
        public static Polyline TextOBB(this DBText dBText)
        {
            Matrix3d clockwiseMat = Matrix3d.Rotation(-1.0 * dBText.Rotation, Vector3d.ZAxis, dBText.Position);
            DBText newText = dBText.GetTransformedCopy(clockwiseMat) as DBText;
            Polyline obb = newText.GeometricExtents.ToRectangle();
            Matrix3d counterClockwiseMat = Matrix3d.Rotation(dBText.Rotation, Vector3d.ZAxis, dBText.Position);
            obb.TransformBy(counterClockwiseMat);
            return obb;
        }
    }
}
