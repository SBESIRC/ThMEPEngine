using System;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
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
        public static bool IsParallelToEx(this Vector3d vector, Vector3d other)
        {
            double angle = vector.GetAngleTo(other) / Math.PI * 180.0;
            return (angle < ThMEPEngineCoreCommon.LOOSE_PARALLEL_ANGLE) || ((180.0 - angle) < ThMEPEngineCoreCommon.LOOSE_PARALLEL_ANGLE);
        }
        public static bool IsParallelToEx(this Line first, Line second)
        {
            var firstVec = first.LineDirection();
            var secondVec = second.LineDirection();
            return firstVec.IsParallelToEx(secondVec);
        }
        public static bool IsCollinearEx(Point3d firstSp, Point3d firstEp,
            Point3d secondSp, Point3d secondEp, double tolerance = 1.0)
        {
            return
                IsCollinearEx(firstSp, firstEp, secondSp, tolerance) &&
                IsCollinearEx(firstSp, firstEp, secondEp, tolerance);
        }
        public static bool IsCollinearEx(Point3d firstPt, Point3d secondPt,
            Point3d thirdPt, double tolerance = 1.0)
        {
            Vector3d firstVec = firstPt.GetVectorTo(secondPt);
            Vector3d secondVec = firstPt.GetVectorTo(thirdPt);
            return firstVec.CrossProduct(secondVec).Length <= firstVec.Length * tolerance;
        }
        public static bool IsOverlapEx(Point3d firstSp, Point3d firstEp,
            Point3d secondSp, Point3d secondEp)
        {
            if (IsCollinearEx(firstSp, firstEp, secondSp, secondEp))
            {
                List<Point3d> pts = new List<Point3d> { firstSp, firstEp, secondSp, secondEp };
                var maxPts = GetCollinearMaxPts(pts);
                return (firstSp.DistanceTo(firstEp) + secondSp.DistanceTo(secondEp)) >
                    maxPts.Item1.DistanceTo(maxPts.Item2);
            }
            return false;
        }
        public static Point3d GetMidPt(this Point3d pt1, Point3d pt2)
        {
            return pt1 + pt1.GetVectorTo(pt2) * 0.5;
        }
        public static Point3d GetExtentPoint(this Point3d pt,Vector3d direction,double length)
        {
            return pt+ direction.GetNormal().MultiplyBy(length);
        }
        public static Point3d GetProjectPtOnLine(this Point3d outerPt, Point3d startPt, Point3d endPt)
        {
            Vector3d firstVec = startPt.GetVectorTo(endPt);
            Vector3d secondVec = startPt.GetVectorTo(outerPt);
            double angle = firstVec.GetAngleTo(secondVec);
            double distance = Math.Cos(angle) * secondVec.Length;
            return startPt + firstVec.GetNormal().MultiplyBy(distance);
        }
        public static bool IsIntersects(this Entity firstEnt, Entity secondEnt)
        {
            return firstEnt.IntersectWithEx(secondEnt).Count > 0 ? true : false;
        }
        public static Point3dCollection IntersectWithEx(this Entity firstEntity, Entity secondEntity, Intersect intersectType = Intersect.OnBothOperands)
        {
            Point3dCollection pts = new Point3dCollection();
            Plane plane = new Plane(Point3d.Origin, Vector3d.ZAxis);
            firstEntity.IntersectWith(secondEntity, intersectType, plane, pts, IntPtr.Zero, IntPtr.Zero);
            plane.Dispose();
            return pts;
        }
        public static bool IsPointOnLine(Point3d lineSp, Point3d lineEp, Point3d outerPt, double tolerance = 1e-4)
        {
            var line = new Line(lineSp, lineEp);
            if (line.Length < tolerance)
            {
                // 在精度下“塌陷”成一个点
                return false;
            }
            return outerPt.DistanceTo(line.GetClosestPointTo(outerPt, false)) <= tolerance;
        }
        public static bool IsPointInLine(Point3d lineSp, Point3d lineEp, Point3d outerPt, double tolerance = 0.0)
        {
            Vector3d vec = lineSp.GetVectorTo(lineEp).GetNormal();
            Point3d sp = lineSp + vec.MultiplyBy(tolerance);
            Point3d ep = lineEp - vec.MultiplyBy(tolerance);
            return IsPointOnLine(sp, ep, outerPt);
        }
        public static bool IsProjectionPtInLine(Point3d lineSp, Point3d lineEp, Point3d outerPt)
        {
            Vector3d vec = lineSp.GetVectorTo(lineEp);
            Plane plane = new Plane(lineSp, vec);
            Matrix3d wcsToUcs = Matrix3d.WorldToPlane(plane);
            Point3d newPt = outerPt.TransformBy(wcsToUcs);
            return newPt.Z >= 0 && newPt.Z <= lineSp.DistanceTo(lineEp);
        }
        public static bool IsVertical(this Vector3d first, Vector3d second, double angTolerance = 1.0)
        {
            var ang = first.GetAngleTo(second).RadToAng();
            ang = ang % 180.0;
            return Math.Abs(ang - 90.0) <= angTolerance;
        }

        public static Polyline TextOBB(this DBText dBText)
        {
            try
            {
                return dBText.OBBFrame();
            }
            catch
            {
                // 以下原因可能导致DBText.GeometricExtents异常：
                //  1. 缺文字样式的字体
                //  2. 文字的内容为空
                // 这里返回一个空闭合多段线
                return new Polyline()
                {
                    Closed = true
                };
            }
        }
        public static Polyline TextOBB(this MText mText)
        {
            if(mText.Location.IsNull())
            {
                return new Polyline()
                {
                    Closed = true,
                };
            }
            if (string.IsNullOrEmpty(mText.Text))
            {
                return new Polyline()
                {
                    Closed = true,
                };
            }
            var mTextCopy = mText.Clone() as MText;
            var ang = Vector3d.XAxis.GetAngleTo(mTextCopy.Direction, mTextCopy.Normal);
            Matrix3d clockwiseMat = Matrix3d.Rotation(-1.0 * ang, mTextCopy.Normal, mTextCopy.Location);
            var newText = mTextCopy.GetTransformedCopy(clockwiseMat) as MText;

            var objs = new DBObjectCollection();
            newText.Explode(objs);
            var extents = new Extents3d();
            objs.Cast<DBText>().Where(o => !string.IsNullOrEmpty(o.TextString)).ForEach(o =>
            {
                try
                {
                    extents.AddExtents(o.GeometricExtents);
                }
                catch
                {
                    // 忽略导致异常的DBText
                }
            });
            var obb = extents.ToRectangle();
            Matrix3d counterClockwiseMat = Matrix3d.Rotation(ang, mText.Normal, mText.Location);
            obb.TransformBy(counterClockwiseMat);
            return obb;
        }
        public static bool IsPerpendicular(Vector3d firstVec, Vector3d secondVec, double tolerance = 1.0)
        {
            double rad = firstVec.GetAngleTo(secondVec);
            double ang = rad / Math.PI * 180.0;
            return Math.Abs(ang - 90.0) <= tolerance;
        }
        public static double ProjectionDis(this Vector3d a, Vector3d b)
        {
            double rad = a.GetAngleTo(b);
            return b.Length * Math.Cos(rad);
        }

        public static Point3dCollection IntersectPts(
            this Line first, Line second, Intersect intersectType, double distance = 1.0)
        {
            var firstNew = first.ExtendLine(distance);
            var secondNew = second.ExtendLine(distance);
            return firstNew.IntersectWithEx(secondNew, intersectType);
        }

        public static Tuple<Point3d, Point3d> GetCollinearMaxPts(this List<Line> lines)
        {
            //传入的线要共线
            var pts = new List<Point3d>();
            lines.ForEach(o =>
            {
                pts.Add(o.StartPoint);
                pts.Add(o.EndPoint);
            });
            return GetCollinearMaxPts(pts);
        }
        public static Tuple<Point3d, Point3d> GetCollinearMaxPts(this List<Point3d> pts)
        {
            if (pts.Count == 0)
            {
                return Tuple.Create(Point3d.Origin, Point3d.Origin);
            }
            else if (pts.Count == 1)
            {
                return Tuple.Create(pts[0], pts[0]);
            }
            else
            {
                Point3d first = pts[0];
                Point3d second = pts[pts.Count - 1];
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    for (int j = i + 1; j < pts.Count; j++)
                    {
                        if (pts[i].DistanceTo(pts[j]) > first.DistanceTo(second))
                        {
                            first = pts[i];
                            second = pts[j];
                        }
                    }
                }
                return Tuple.Create(first, second);
            }
        }
        public static bool IsOverlap(Point3d firstSp, Point3d firstEp,
            Point3d secondSp, Point3d secondEp, bool includedJoin = true)
        {
            //第二根线在第一根线上的投影是否重叠
            Vector3d vec = firstSp.GetVectorTo(firstEp).GetNormal();
            Plane plane = new Plane(firstSp, vec);
            Matrix3d wcsToUcs = Matrix3d.WorldToPlane(plane);
            Point3d newSecondSp = secondSp.TransformBy(wcsToUcs);
            Point3d newSecondEp = secondEp.TransformBy(wcsToUcs);
            double minZ = Math.Min(newSecondSp.Z, newSecondEp.Z);
            double maxZ = Math.Max(newSecondSp.Z, newSecondEp.Z);
            if (includedJoin)
            {
                if (maxZ < 0)
                {
                    return false;
                }
                if (minZ > firstSp.DistanceTo(firstEp))
                {
                    return false;
                }
            }
            else
            {
                if (maxZ <= 0)
                {
                    return false;
                }
                if (minZ >= firstSp.DistanceTo(firstEp))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsPointOnLine(this Point3d pt, Line line, double tolerance = 0.0001)
        {
            return pt.DistanceTo(line.GetClosestPointTo(pt, false)) <= tolerance;
        }
        /// <summary>
        /// A完全包含B（B没有任何一个点在A的边界上，都在A的里面）
        /// </summary>
        /// <param name="first">A</param>
        /// <param name="second">B</param>
        /// <returns></returns>
        public static bool IsFullContains(this Entity first, Entity second)
        {
            //first完全包含second
            //second所有的点在A的内部，且没有任何一个点在A的边界上
            var firstPolygon = first.ToNTSPolygonalGeometry();
            var secondPolygon = second.ToNTSPolygonalGeometry();
            if (firstPolygon == null || secondPolygon == null)
            {
                return false;
            }
            var relateMatrix = new ThCADCoreNTSRelate(firstPolygon, secondPolygon);
            if (relateMatrix.IsContains || relateMatrix.IsCovers)
            {
                var vertices = second.EntityVertices();
                //second上的点没有一个在first边界上         
                return vertices.Cast<Point3d>().Where(o => firstPolygon.OnBoundary(o)).Count() == 0;
            }
            else
            {
                return false;
            }
        }

        public static List<Point3d> GetPoints(this Line line)
        {
            var result = new List<Point3d>();
            result.Add(line.StartPoint);
            result.Add(line.EndPoint);
            return result;
        }
        public static List<Point3d> GetPoints(this Polyline polyline, double tesslateLength = 5)
        {
            var result = new List<Point3d>();
            polyline.ExplodeLines(tesslateLength).ForEach(o =>
            {
                result.AddRange(GetPoints(o));
            });
            return result;
        }
        public static List<Point3d> GetPoints(this Circle circle)
        {
            var result = new List<Point3d>();
            result.Add(circle.Center + Vector3d.XAxis.MultiplyBy(circle.Radius));
            result.Add(circle.Center + Vector3d.YAxis.MultiplyBy(circle.Radius));
            result.Add(circle.Center - Vector3d.XAxis.MultiplyBy(circle.Radius));
            result.Add(circle.Center - Vector3d.YAxis.MultiplyBy(circle.Radius));
            return result;
        }
        public static List<Point3d> GetPoints(this Arc arc, double tesslateLength = 5)
        {
            var result = new List<Point3d>();
            var polyline = arc.TessellateArcWithArc(tesslateLength);
            polyline.ExplodeLines(tesslateLength).ForEach(o =>
            {
                result.AddRange(GetPoints(o));
            });
            return result;
        }
        public static List<Point3d> GetPoints(this Ellipse ellipse)
        {
            var result = new List<Point3d>();
            var pt1 = ellipse.Center + ellipse.MajorAxis.GetNormal().MultiplyBy(ellipse.MajorRadius);
            var pt2 = ellipse.Center + ellipse.MinorAxis.GetNormal().MultiplyBy(ellipse.MinorRadius);
            var pt3 = ellipse.Center - ellipse.MajorAxis.GetNormal().MultiplyBy(ellipse.MajorRadius);
            var pt4 = ellipse.Center - ellipse.MinorAxis.GetNormal().MultiplyBy(ellipse.MinorRadius);
            result.Add(pt1);
            result.Add(pt2);
            result.Add(pt3);
            result.Add(pt4);
            return result;
        }

        /// <summary>
        /// CAD GetOffsetCurves 外扩内缩闭合polyline。
        /// offset为正，外扩。为负，内缩。
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Polyline GetOffsetClosePolyline(this Polyline polyline, double offset)
        {
            var dir = 1;
            var newPolyline = new Polyline();
            if (polyline.Closed == true)
            {
                if (polyline.IsCCW() == false)
                {
                    dir = -1;
                }
                 newPolyline = polyline.GetOffsetCurves(dir * offset).Cast<Polyline>().OrderByDescending(y => y.Area).FirstOrDefault();
            }

            return newPolyline;
        }
    }
}
