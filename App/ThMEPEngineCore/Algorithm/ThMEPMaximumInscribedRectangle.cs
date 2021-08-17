using System;
using System.Linq;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Algorithm
{
    public class ThMEPMaximumInscribedRectangle
    {
        // Algorithm constants
        double aspectRatioStep = 0.5; // step size for the aspect ratio
        double angleStep = 5; // step size for angles (in degrees); has linear impact on running time
        double tolerance = 0.02;
        double nTries = 20;
        double minHeight = 0;
        double minWidth = 0;
        Tolerance mergeTol = new Tolerance(1, 0.01);

        public Polyline GetRectangle(Polyline poly)
        {
            if (poly.Area == 0)
            {
                return null;
            }
            // simplify polygon
            poly = poly.DPSimplify(tolerance);

            // get the width of the bounding box of the simplified polygon
            var polyBoundingBox = GetBoundingBox(poly);
            double minX = polyBoundingBox.First().X;
            double minY = polyBoundingBox.First().Y;
            double boxWidth = polyBoundingBox.Last().X - minX;
            double boxHeight = polyBoundingBox.Last().Y - minY;

            // discretize the binary search for optimal width to a resolution of this times the polygon width
            double widthStep = Math.Min(boxWidth, boxHeight) / 50;

            // populate possible center points with random points inside the polygon
            List<Point3d> origins = new List<Point3d>();
            if (origins.Count <= 0)
            {
                // get the centroid of the polygon
                Point3d centroid = poly.GetMaximumInscribedCircleCenter();
                origins.Add(centroid);

                // get few more points inside the polygon
                while (nTries > 0)
                {
                    Random rand = new Random(Guid.NewGuid().GetHashCode());
                    double rndX = rand.NextDouble() * boxWidth + minX;
                    double rndY = rand.NextDouble() * boxHeight + minY;
                    Point3d rndPoint = new Point3d(rndX, rndY, 0);
                    if (poly.Contains(rndPoint))
                    {
                        origins.Add(rndPoint);
                    }
                    nTries--;
                }
            }
            double maxArea = 0;
            Polyline maxRect = null;

            List<double> angles = Range(-90, 90 + angleStep, angleStep);
            List<double> aspectRatios = new List<double>() { 1, 15 };
            for (int i = 0; i < angles.Count; i++)
            {
                double angle = angles[i];
                double angleRad = -angle * Math.PI / 180;
                for (int j = 0; j < origins.Count; j++)
                {
                    var origOrigin = origins[j];
                    // generate improved origins
                    var pointInfo1 = PolygonRayCast(poly, origOrigin, angleRad);
                    var pointInfo2 = PolygonRayCast(poly, origOrigin, angleRad + Math.PI / 2);
                    List<Point3d> pts = new List<Point3d>();
                    if (pointInfo1.Item1 != null && pointInfo1.Item2 != null)   // average along with width axis
                        pts.Add(new Point3d((pointInfo1.Item1.Value.X + pointInfo1.Item2.Value.X) / 2, (pointInfo1.Item1.Value.Y + pointInfo1.Item2.Value.Y) / 2, 0));
                    if (pointInfo2.Item1 != null && pointInfo2.Item2 != null)   // average along with height axis
                        pts.Add(new Point3d((pointInfo2.Item1.Value.X + pointInfo2.Item2.Value.X) / 2, (pointInfo2.Item1.Value.Y + pointInfo2.Item2.Value.Y) / 2, 0));

                    for (int z = 0; z < pts.Count; z++)
                    {
                        var origin = pts[z];

                        var pt1 = PolygonRayCast(poly, origin, angleRad);
                        if (pt1.Item1 == null || pt1.Item2 == null) continue;
                        var minSqDistW = Math.Min(PointDistanceSquared(origin, pt1.Item1.Value), PointDistanceSquared(origin, pt1.Item2.Value));
                        var maxWidth = 2 * Math.Sqrt(minSqDistW);

                        var pt2 = PolygonRayCast(poly, origin, angleRad + Math.PI / 2);
                        if (pt2.Item1 == null || pt2.Item2 == null) continue;
                        var minSqDistH = Math.Min(PointDistanceSquared(origin, pt2.Item1.Value), PointDistanceSquared(origin, pt2.Item2.Value));
                        var maxHeight = 2 * Math.Sqrt(minSqDistH);

                        if (maxWidth * maxHeight < maxArea) continue;

                        var minAspectRatio = new List<double> { aspectRatios[0], minWidth / maxHeight, maxArea / (maxHeight * maxHeight) }.Max();
                        var maxAspectRatio = new List<double> { aspectRatios[1], maxWidth / minHeight, maxWidth * maxWidth / maxArea }.Min();
                        var aRatios = Range(minAspectRatio, maxAspectRatio + aspectRatioStep, aspectRatioStep);

                        for (int m = 0; m < aRatios.Count; m++)
                        {
                            var aRatio = aRatios[m];

                            // do a binary search to find the max width that works
                            var left = Math.Max(minWidth, Math.Sqrt(maxArea * aRatio));
                            var right = Math.Min(maxWidth, maxHeight * aRatio);
                            if (right * maxHeight < maxArea) continue;

                            while (right - left >= widthStep)
                            {
                                var width = (left + right) / 2;
                                var height = width / aRatio;
                                var cPt = origin;
                                var rectPoly = new Polyline() { Closed = true };
                                rectPoly.AddVertexAt(0, new Point2d(cPt.X - width / 2, cPt.Y - height / 2), 0, 0, 0);
                                rectPoly.AddVertexAt(1, new Point2d(cPt.X + width / 2, cPt.Y - height / 2), 0, 0, 0);
                                rectPoly.AddVertexAt(2, new Point2d(cPt.X + width / 2, cPt.Y + height / 2), 0, 0, 0);
                                rectPoly.AddVertexAt(3, new Point2d(cPt.X - width / 2, cPt.Y + height / 2), 0, 0, 0);

                                rectPoly = PolylylineRaotate(rectPoly, angleRad, origin);
                                if (poly.Contains(rectPoly))
                                {
                                    // we know that the area is already greater than the maxArea found so far
                                    maxArea = width * height;
                                    //rectPoly.push(rectPoly[0]);
                                    maxRect = rectPoly;
                                    left = width; // increase the width in the binary search
                                }
                                else
                                {
                                    right = width; // decrease the width in the binary search 
                                }
                            }
                        }
                    }
                }
            }

            maxRect = OptimizationRetangle(maxRect, poly);
            return maxRect;
        }

        /// <summary>
        /// 计算距离
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private double PointDistanceSquared(Point3d p1, Point3d p2)
        {
            var dx = p2[0] - p1[0];
            var dy = p2[1] - p1[1];

            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 旋转polyline
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private Polyline PolylylineRaotate(Polyline polyline, double alpha, Point3d origin)
        {
            Polyline raotatePoly = new Polyline() { Closed = polyline.Closed };
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                raotatePoly.AddVertexAt(i, PointRaotate(polyline.GetPoint3dAt(i), alpha, origin).ToPoint2D(), 0, 0, 0);
            }

            return raotatePoly;
        }

        /// <summary>
        /// 旋转点
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="alpha"></param>
        /// <param name="originPt"></param>
        /// <returns></returns>
        private Point3d PointRaotate(Point3d pt, double alpha, Point3d originPt)
        {
            var cosAlpha = Math.Cos(alpha);
            var sinAlpha = Math.Sin(alpha);
            var xshifted = pt[0] - originPt[0];
            var yshifted = pt[1] - originPt[1];

            return new Point3d(cosAlpha * xshifted - sinAlpha * yshifted + originPt[0], sinAlpha * xshifted + cosAlpha * yshifted + originPt[1], 0);
        }

        /// <summary>
        /// 更新原始点
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="origin"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        private Tuple<Point3d?, Point3d?> PolygonRayCast(Polyline poly, Point3d origin, double alpha = 0)
        {
            var eps = 1e-9;
            origin = new Point3d(origin.X + eps * Math.Cos(alpha), origin.Y + eps * Math.Sin(alpha), 0);
            var shiftedOrigin = new Point3d(origin.X + Math.Cos(alpha), origin.Y + Math.Sin(alpha), 0);

            var idx = 0;
            if (Math.Abs(shiftedOrigin[0] - origin.X) < eps) idx = 1;
            var i = -1;
            var n = poly.NumberOfVertices;
            var b = poly.GetPoint3dAt(n - 1);
            var minSqDistLeft = double.MaxValue;
            var minSqDistRight = double.MaxValue;
            Point3d? closestPointLeft = null;
            Point3d? closestPointRight = null;
            while (++i < n)
            {
                var a = b;
                b = poly.GetPoint3dAt(i);
                var coordinate = LineIntersection(origin, shiftedOrigin, a, b);
                if (coordinate != null)
                {
                    var p = new Point3d(coordinate.Value.X, coordinate.Value.Y, 0);
                    if (SegmentBoxContains(a, b, p))
                    {
                        double sqDist = origin.DistanceTo(p);
                        if (p[idx] < origin[idx])
                        {
                            if (sqDist < minSqDistLeft)
                            {
                                minSqDistLeft = sqDist;
                                closestPointLeft = p;
                            }
                        }
                        else if (p[idx] > origin[idx])
                        {
                            if (sqDist < minSqDistRight)
                            {
                                minSqDistRight = sqDist;
                                closestPointRight = p;
                            }
                        }
                    }
                }
            }

            return Tuple.Create(closestPointLeft, closestPointRight);
        }

        /// <summary>
        /// box是否包含当前点
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool SegmentBoxContains(Point3d a, Point3d b, Point3d p)
        {
            var eps = 0.01;

            return !(p.X < Math.Min(a[0], b[0]) - eps || p.X > Math.Max(a[0], b[0]) + eps ||
                     p.Y < Math.Min(a[1], b[1]) - eps || p.Y > Math.Max(a[1], b[1]) + eps);
        }

        /// <summary>
        /// 线相交
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="q1"></param>
        /// <param name="p2"></param>
        /// <param name="q2"></param>
        /// <returns></returns>
        private Point3d? LineIntersection(Point3d p1, Point3d q1, Point3d p2, Point3d q2)
        {
            // allow for some margins due to numerical errors
            var eps = 1e-9;

            // find the intersection point between the two infinite lines
            var dx1 = p1[0] - q1[0];
            var dx2 = p2[0] - q2[0];
            var dy1 = p1[1] - q1[1];
            var dy2 = p2[1] - q2[1];

            var denom = dx1 * dy2 - dy1 * dx2;

            if (Math.Abs(denom) < eps) return null;

            var cross1 = p1[0] * q1[1] - p1[1] * q1[0];
            var cross2 = p2[0] * q2[1] - p2[1] * q2[0];

            var px = (cross1 * dx2 - cross2 * dx1) / denom;
            var py = (cross1 * dy2 - cross2 * dy1) / denom;

            return new Point3d(px, py, 0);
        }

        /// <summary>
        /// 在范围内均分数
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        private List<double> Range(double start, double end, double step)
        {
            List<double> res = new List<double>() { start };
            for (int i = 1; i < (end - start) / step; i++)
            {
                res.Add(start + i * step);
            }

            return res;
        }

        /// <summary>
        /// 获得polyline的boundingbox
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        private List<Point3d> GetBoundingBox(Polyline poly)
        {
            List<Point3d> pts = new List<Point3d>();
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                pts.Add(poly.GetPoint3dAt(i));
            }

            double minX = pts.OrderBy(x => x.X).First().X;
            double maxX = pts.OrderByDescending(x => x.X).First().X;
            double minY = pts.OrderBy(x => x.Y).First().Y;
            double maxY = pts.OrderByDescending(x => x.Y).First().Y;

            return new List<Point3d>() { new Point3d(minX, minY, 0), new Point3d(maxX, maxY, 0) };
        }

        /// <summary>
        /// 优化修正最大内接矩形
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="frame"></param>
        private Polyline OptimizationRetangle(Polyline rect, Polyline frame)
        {
            List<Line> lines = new List<Line>();
            List<Point3d> allPts = new List<Point3d>();
            for (int i = 0; i < frame.NumberOfVertices; i++)
            {
                lines.Add(new Line(frame.GetPoint3dAt(i), frame.GetPoint3dAt((i + 1) % frame.NumberOfVertices)));
                allPts.Add(frame.GetPoint3dAt(i));
            }

            List<Line> rectLines = new List<Line>();
            for (int i = 0; i < rect.NumberOfVertices; i++)
            {
                rectLines.Add(new Line(rect.GetPoint3dAt(i), rect.GetPoint3dAt((i + 1) % rect.NumberOfVertices)));
            }

            int index = 0;
            bool isClockwise = ThCADCoreNTSDbExtension.IsCCW(rect);
            while (index < rectLines.Count())
            {
                Line line = rectLines[index];
                var closetLine = GetClosetLines(lines, line);
                var s = (line.EndPoint - line.StartPoint).GetNormal();
                var dir = Vector3d.ZAxis.CrossProduct((line.EndPoint - line.StartPoint).GetNormal());
                double distance = line.Distance(closetLine);
                dir = isClockwise ? -dir : dir;
                Line newLine = new Line(line.StartPoint + distance * dir, line.EndPoint + distance * dir);

                for (int i = 0; i < rectLines.Count; i++)
                {
                    var otherLine = rectLines[i];
                    if (!(otherLine.EndPoint - otherLine.StartPoint).GetNormal().IsParallelTo(dir, mergeTol))
                    {
                        continue;
                    }
                    var moveDir = Vector3d.ZAxis.CrossProduct((otherLine.EndPoint - otherLine.StartPoint).GetNormal());
                    moveDir = isClockwise ? -moveDir : moveDir;
                    var startPt = otherLine.StartPoint.DistanceTo(newLine.StartPoint) < otherLine.EndPoint.DistanceTo(newLine.StartPoint) ?
                         otherLine.StartPoint + distance * dir : otherLine.StartPoint;
                    var endPt = otherLine.EndPoint.DistanceTo(newLine.StartPoint) < otherLine.StartPoint.DistanceTo(newLine.StartPoint) ?
                         otherLine.EndPoint + distance * dir : otherLine.EndPoint;
                    rectLines[i] = new Line(startPt, endPt);
                }

                rectLines[index] = newLine;
                index++;
            }

            return CreatePolyline(rectLines);
        }

        /// <summary>
        /// 找到需要的距离最近的线
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="lines"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private Line GetClosetLines(List<Line> lines, Line line)
        {
            var overlapLines = GetOverlapLines(lines, line);
            var closetLine = overlapLines.OrderBy(x => x.Distance(line)).FirstOrDefault();
            return closetLine;
        }

        /// <summary>
        ///找到与当前线有overlap的线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private List<Line> GetOverlapLines(List<Line> lines, Line line)
        {
            Vector3d xDir = (line.EndPoint - line.StartPoint).GetNormal();
            Vector3d zDir = Vector3d.ZAxis;
            Vector3d yDir = zDir.CrossProduct(xDir);
            Matrix3d matrix = new Matrix3d(new double[] {
                xDir.X, yDir.X, zDir.X, 0,
                xDir.Y, yDir.Y, zDir.Y, 0,
                xDir.Z, yDir.Z, zDir.Z, 0,
                0.0, 0.0, 0.0, 1.0
            });

            Line transLine = line.Clone() as Line;
            transLine.TransformBy(matrix.Inverse());

            return lines.Where(x =>
            {
                var compareLine = x.Clone() as Line;
                compareLine.TransformBy(matrix.Inverse());
                double minX = (compareLine.StartPoint.X < compareLine.EndPoint.X ? compareLine.StartPoint.X : compareLine.EndPoint.X) + 0.1;
                double maxX = (compareLine.StartPoint.X > compareLine.EndPoint.X ? compareLine.StartPoint.X : compareLine.EndPoint.X) - 0.1;
                return !(maxX < transLine.StartPoint.X || minX > transLine.EndPoint.X);
            }).ToList();
        }

        /// <summary>
        /// 创建polyline
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private Polyline CreatePolyline(List<Line> lines)
        {
            Polyline polyline = new Polyline() { Closed = true };
            for (int i = 0; i < lines.Count; i++)
            {
                polyline.AddVertexAt(i, lines[i].StartPoint.ToPoint2D(), 0, 0, 0);
            }

            return polyline;
        }
    }
}
