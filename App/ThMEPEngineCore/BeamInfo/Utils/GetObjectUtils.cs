using System;
using System.Diagnostics;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BeamInfo.Utils
{
    public static class GetObjectUtils
    {
        /// <summary>
        /// 根据polygon取得范围内的元素（boundingbox内的）
        /// </summary>
        /// <param name="ed"></param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static PromptSelectionResult GetObjectWithBounding(Editor ed, Point3d pt1, Point3d pt2, SelectionFilter filter = null)
        {
            Point3dCollection polygon = new Point3dCollection();
            double minX = Math.Min(pt1.X, pt2.X);
            double minY = Math.Min(pt1.Y, pt2.Y);
            double minZ = Math.Min(pt1.Z, pt2.Z);

            double maxX = Math.Max(pt1.X, pt2.X);
            double maxY = Math.Max(pt1.Y, pt2.Y);
            double maxZ = Math.Max(pt1.Z, pt2.Z);
            polygon.Add(new Point3d(minX, minY, minZ));
            polygon.Add(new Point3d(maxX, minY, minZ));
            polygon.Add(new Point3d(maxX, maxY, minZ));
            polygon.Add(new Point3d(minX, maxY, minZ));
            
            PromptSelectionResult result;
            if (filter == null)
            {
                result = ed.SelectCrossingPolygon(polygon);
            }
            else
            {
                result = ed.SelectCrossingPolygon(polygon, filter);
            }

            return result;
        }

        /// <summary>
        /// 根据polygon区的范围内的元素（自定义矩形内的）
        /// </summary>
        /// <param name="ed"></param>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static PromptSelectionResult GetObjectWithBounding(Editor ed, Point3d pt1, Point3d pt2, Vector3d dir, SelectionFilter filter = null)
        {
            Vector3d pDir = (pt1 - pt2).GetNormal();
            double dis = pt1.DistanceTo(pt2);
            if (pDir.DotProduct(dir) < 0)
            {
                dir = -dir;
            }
            double angle = pDir.GetAngleTo(dir);
            double offset = Math.Cos(angle) * dis;
            Point3d moveP1 = pt1 - offset * dir;
            Point3d moveP2 = pt2 + offset * dir;
            Point3dCollection polygon = new Point3dCollection() { pt1, moveP1, pt2, moveP2 };
            PromptSelectionResult result;
            if (filter == null)
            {
                result = ed.SelectCrossingPolygon(polygon);
            }
            else
            {
                result = ed.SelectCrossingPolygon(polygon, filter);
            }

            return result;
        }

        /// <summary>
        /// 判断点是否再Polyline内
        /// 0.点在polyline上    1.点在polyline内    -1.点在polyline外
        /// </summary>
        /// <returns></returns>
        public static int CheckPointInPolyline(Polyline polyline, Point3d pt, double tol)
        {
            Debug.Assert(polyline != null);
            Point3d closestP = polyline.GetClosestPointTo(pt, false);
            if (Math.Abs(closestP.DistanceTo(pt)) < tol)
            {
                return 0;
            }

            Ray ray = new Ray();
            ray.BasePoint = pt;
            ray.UnitDir = -(closestP - pt).GetNormal();
            Point3dCollection points = new Point3dCollection();
            polyline.IntersectWith(ray, Intersect.OnBothOperands, points, IntPtr.Zero, IntPtr.Zero);
            FilterEqualPoints(points, tol);

            if (points.Count % 2 == 0)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// 过滤掉容差范围内相同的点
        /// </summary>
        /// <param name="points"></param>
        /// <param name="tol"></param>
        public static void FilterEqualPoints(Point3dCollection points, double tol)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (points[i].DistanceTo(points[j]) < tol)
                    {
                        points.Remove(points[j]);
                    }
                }
            }
        }

        /// <summary>
        /// 扩大或者缩小Polyline.目前只处理全是直线的多线段
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Polyline ExpansionPolyline(Polyline polyline, double offset)
        {
            Polyline newPolyline = new Polyline() { Closed = true };
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point2d currentP = polyline.GetPoint2dAt(i);
                int j = i - 1;
                if (j < 0)
                {
                    j = polyline.NumberOfVertices - 1;
                }
                Point2d preP = polyline.GetPoint2dAt(j);
                j = i + 1;
                if (j > polyline.NumberOfVertices - 1)
                {
                    j = 0;
                }
                Point2d nextP = polyline.GetPoint2dAt(j);
                Vector2d preDir = (currentP - preP).GetNormal();
                Vector2d nextDir = (nextP - currentP).GetNormal();

                Point2d newP = currentP + offset * preDir - nextDir * offset;
                int res = CheckPointInPolyline(polyline, new Point3d(newP.X, newP.Y, 0), 0.001);
                if (offset > 0)
                {
                    if (res != -1)
                    {
                        newP = currentP - offset * preDir + nextDir * offset;
                    }
                }
                else
                {
                    if (res == -1)
                    {
                        newP = currentP - offset * preDir + nextDir * offset;
                    }
                }
                newPolyline.AddVertexAt(i, newP, 0, 0, 0);
            }

            return newPolyline;
        }

        // 合并两段共圆心同半径的Arc
        public static Arc ArcMerge(Arc arc1, Arc arc2)
        {
            // 变量初始化；
            var startAngle = new double();
            var endAngle = new double();

            var flag_1 = false;
            var flag_2 = false;
            var arc1_1 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());
            var arc1_2 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());
            var arc2_1 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());
            var arc2_2 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());

            if (arc1.StartAngle > arc1.EndAngle)
            {
                arc1_1 = new Arc(arc1.Center, arc1.Radius, 0.0, arc1.EndAngle);
                arc1_2 = new Arc(arc1.Center, arc1.Radius, arc1.StartAngle, 8 * Math.Atan(1));
                flag_1 = true;
            }
            if (arc2.StartAngle > arc2.EndAngle)
            {
                arc2_1 = new Arc(arc2.Center, arc2.Radius, 0.0, arc2.EndAngle);
                arc2_2 = new Arc(arc2.Center, arc2.Radius, arc2.StartAngle, 8 * Math.Atan(1));
                flag_2 = true;
            }
            // 两段弧均截断
            if (flag_1 && flag_2)
            {
                startAngle = Math.Min(arc1_2.StartAngle, arc2_2.StartAngle);
                endAngle = Math.Max(arc1_1.EndAngle, arc2_1.EndAngle);
            }
            // 仅arc1截断
            else if (flag_1 && !(flag_2))
            {
                if (arc2.StartAngle <= arc1_1.EndAngle)
                {
                    if (arc2.EndAngle <= arc1_2.StartAngle)
                    {
                        startAngle = arc1_2.StartAngle;
                        endAngle = Math.Max(arc1_1.EndAngle, arc2.EndAngle);
                    }
                    // arc1与arc2存在两段间断的重叠范围（如：c与镜像c）
                    else
                    {
                        startAngle = 0.0;
                        endAngle = 8 * Math.Atan(1);
                    }
                }
                else
                {
                    startAngle = Math.Min(arc1_2.StartAngle, arc2.StartAngle);
                    endAngle = arc1_1.EndAngle;
                }
            }
            // 仅arc2截断
            else if (!(flag_1) && flag_2)
            {
                if (arc1.StartAngle <= arc2_1.EndAngle)
                {
                    if (arc1.EndAngle <= arc2_2.StartAngle)
                    {
                        startAngle = arc2_2.StartAngle;
                        endAngle = Math.Max(arc2_1.EndAngle, arc1.EndAngle);
                    }
                    // arc1与arc2存在两段不相连的重叠区域（如：c与镜像c）
                    else
                    {
                        startAngle = 0.0;
                        endAngle = 8 * Math.Atan(1);
                    }
                }
                else
                {
                    startAngle = Math.Min(arc2_2.StartAngle, arc1.StartAngle);
                    endAngle = arc2_1.EndAngle;
                }
            }
            // 两段弧均不截断
            else
            {
                startAngle = Math.Min(arc1.StartAngle, arc2.StartAngle);
                endAngle = Math.Max(arc1.EndAngle, arc2.EndAngle);
            }
            return new Arc(arc1.Center, arc1.Radius, startAngle, endAngle);
        }

        // 计算两段弧的重合角度
        public static Tuple<bool, double, double, double> OverlapAngle(Arc arc1, Arc arc2)
        {
            // 变量初始化；
            var startAngle = new double();
            var endAngle = new double();
            var AngleRange = new double();
            var isOverlap = false;

            var flag_1 = false;
            var flag_2 = false;
            var arc1_1 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());
            var arc1_2 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());
            var arc2_1 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());
            var arc2_2 = new Arc(new Point3d(0.0, 0.0, 0.0), new double(), new double(), new double());

            if (arc1.StartAngle > arc1.EndAngle)
            {
                arc1_1 = new Arc(arc1.Center, arc1.Radius, 0.0, arc1.EndAngle);
                arc1_2 = new Arc(arc1.Center, arc1.Radius, arc1.StartAngle, 8 * Math.Atan(1));
                flag_1 = true;
            }
            if (arc2.StartAngle > arc2.EndAngle)
            {
                arc2_1 = new Arc(arc2.Center, arc2.Radius, 0.0, arc2.EndAngle);
                arc2_2 = new Arc(arc2.Center, arc2.Radius, arc2.StartAngle, 8 * Math.Atan(1));
                flag_2 = true;
            }
            // 两段弧均截断
            if (flag_1 && flag_2)
            {
                isOverlap = true;
                startAngle = Math.Max(arc1_2.StartAngle, arc2_2.StartAngle);
                endAngle = Math.Min(arc1_1.EndAngle, arc2_1.EndAngle);
                AngleRange = endAngle + 8 * Math.Atan(1) - startAngle;
            }
            // 仅arc1截断
            else if (flag_1 && !(flag_2))
            {
                isOverlap = !(arc2.StartAngle > arc1_1.EndAngle && arc2.EndAngle < arc1_2.StartAngle);
                if (isOverlap)
                {
                    if (arc2.StartAngle <= arc1_1.EndAngle)
                    {
                        if (arc2.EndAngle <= arc1_2.StartAngle)
                        {
                            startAngle = arc2.StartAngle;
                            endAngle = Math.Min(arc1_1.EndAngle, arc2.EndAngle);
                            AngleRange = endAngle - startAngle;
                        }
                        // arc1与arc2存在两段间断的重叠范围（如：c与镜像c）
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        startAngle = Math.Max(arc1_2.StartAngle, arc2.StartAngle);
                        endAngle = arc2.EndAngle;
                        AngleRange = endAngle - startAngle;
                    }
                }

            }
            // 仅arc2截断
            else if (!(flag_1) && flag_2)
            {
                isOverlap = !(arc1.StartAngle > arc2_1.EndAngle && arc1.EndAngle < arc2_2.StartAngle);
                if (isOverlap)
                {
                    if (arc1.StartAngle <= arc2_1.EndAngle)
                    {
                        if (arc1.EndAngle <= arc2_2.StartAngle)
                        {
                            startAngle = arc1.StartAngle;
                            endAngle = Math.Min(arc2_1.EndAngle, arc1.EndAngle);
                            AngleRange = endAngle - startAngle;
                        }
                        // arc1与arc2存在两段不相连的重叠区域（如：c与镜像c）
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        startAngle = Math.Max(arc2_2.StartAngle, arc1.StartAngle);
                        endAngle = arc1.EndAngle;
                        AngleRange = endAngle - startAngle;
                    }
                }
            }
            // 两段弧均不截断
            else
            {
                isOverlap = !(arc1.StartAngle > arc2.EndAngle || arc2.StartAngle > arc1.EndAngle);
                if (isOverlap)
                {
                    startAngle = Math.Max(arc1.StartAngle, arc2.StartAngle);
                    endAngle = Math.Min(arc1.EndAngle, arc2.EndAngle);
                    AngleRange = endAngle - startAngle;
                }
            }
            return Tuple.Create(isOverlap, startAngle, endAngle, AngleRange);
        }
        public static double BulgeFromCurve(Curve cv, bool clockwise)
        {
            double bulge = 0.0;
            Arc a = cv as Arc;
            if (a != null)
            {
                double newStart;
                // The start angle is usually greater than the end,
                // as arcs are all counter-clockwise.
                // (If it isn't it's because the arc crosses the
                // 0-degree line, and we can subtract 2PI from the
                // start angle.)
                if (a.StartAngle > a.EndAngle)
                    newStart = a.StartAngle - 8 * Math.Atan(1);
                else
                    newStart = a.StartAngle;

                // Bulge is defined as the tan of
                // one fourth of the included angle
                bulge = Math.Tan((a.EndAngle - newStart) / 4);
                // If the curve is clockwise, we negate the bulge
                if (clockwise)
                    bulge = -bulge;

            }
            return bulge;
        }

        public static Vector3d Direction(Line line)
        {
            return line.StartPoint.GetVectorTo(line.EndPoint).GetNormal();
        }
    }
}

