﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetARX;
using ThMEPElectrical.Assistant;
using ThMEPElectrical.Model;

namespace ThMEPElectrical.Geometry
{
    public class GeomUtils
    {
        public static double CalLoopArea(List<Point2d> ptLst)
        {
            if (ptLst == null || ptLst.Count < 3)
                return 0;

            if (Point2dIsEqualPoint2d(ptLst.First(), ptLst.Last()))
                ptLst.Remove(ptLst.Last());

            double area = 0.0;
            var ptCount = ptLst.Count;
            
            for (int i = 0; i < ptCount; i++)
            {
                var curPoint = ptLst[i];
                var nextPoint = ptLst[(i + 1) % (ptCount)];

                area += 0.5 * (curPoint.X * nextPoint.Y - curPoint.Y * nextPoint.X);
            }

            return area;
        }

        public static bool Point2dIsEqualPoint2d(Point2d ptFirst, Point2d ptSecond, double tolerance = 1e-6)
        {
            return IsAlmostNearZero(ptFirst.X - ptSecond.X, tolerance)
                && IsAlmostNearZero(ptFirst.Y - ptSecond.Y, tolerance);
        }

        public static bool Point3dIsEqualPoint3d(Point3d ptFirst, Point3d ptSecond, double tolerance = 1e-6)
        {
            return Point2dIsEqualPoint2d(ptFirst.ToPoint2D(), ptSecond.ToPoint2D(), tolerance);
        }

        /// 零值判断
        public static bool IsAlmostNearZero(double val, double tolerance = 1e-9)
        {
            if (val > -tolerance && val < tolerance)
                return true;

            return false;
        }

        public static Point3d GetMidPoint(Point3d first, Point3d second)
        {
            var x = (first.X + second.X) / 2;
            var y = (first.Y + second.Y) / 2;

            return new Point3d(x, y, 0);
        }

        public static bool PtInLoop(Polyline polyline, Point2d pt)
        {
            if (polyline.Closed == false)
                return false;

            Point2d end = new Point2d(pt.X + 100000000000, pt.Y);
            LineSegment2d intersectLine = new LineSegment2d(pt, end);
            var ptLst = new List<Point2d>();

            var curves = new List<Curve>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                var bulge = polyline.GetBulgeAt(i);
                if (GeomUtils.IsAlmostNearZero(bulge))
                {
                    LineSegment3d line3d = polyline.GetLineSegmentAt(i);
                    curves.Add(new Line(line3d.StartPoint, line3d.EndPoint));
                }
                else
                {
                    var type = polyline.GetSegmentType(i);
                    if (type == SegmentType.Arc)
                    {
                        var arc3d = polyline.GetArcSegmentAt(i);
                        var normal = arc3d.Normal;
                        var axisZ = Vector3d.ZAxis;
                        var arc = new Arc();
                        if (normal.IsEqualTo(Vector3d.ZAxis.Negate()))
                            arc.CreateArcSCE(arc3d.EndPoint, arc3d.Center, arc3d.StartPoint);
                        else
                            arc.CreateArcSCE(arc3d.StartPoint, arc3d.Center, arc3d.EndPoint);
                        curves.Add(arc);
                    }
                }
            }

            Point2d[] intersectPts;
            foreach (var curve in curves)
            {
                if (curve is Line)
                {
                    var line = curve as Line;
                    var lineS = line.StartPoint;
                    var lineE = line.EndPoint;
                    var s2d = new Point2d(lineS.X, lineS.Y);
                    var e2d = new Point2d(lineE.X, lineE.Y);
                    var line2d = new LineSegment2d(s2d, e2d);
                    intersectPts = line2d.IntersectWith(intersectLine);
                }
                else
                {
                    var arc = curve as Arc;
                    var arcS = arc.StartPoint;
                    var arcE = arc.EndPoint;
                    var arcMid = arc.GetPointAtParameter(0.5 * (arc.StartParam + arc.EndParam));
                    var arc2s = new Point2d(arcS.X, arcS.Y);
                    var arc2mid = new Point2d(arcMid.X, arcMid.Y);
                    var arc2e = new Point2d(arcE.X, arcE.Y);
                    CircularArc2d ar = new CircularArc2d(arc2s, arc2mid, arc2e);
                    intersectPts = ar.IntersectWith(intersectLine);
                }

                if (intersectPts != null && intersectPts.Count() == 1)
                {
                    var nPt = intersectPts.First();
                    bool bInLst = false;
                    foreach (var curpt in ptLst)
                    {
                        if (GeomUtils.Point2dIsEqualPoint2d(nPt, curpt))
                        {
                            bInLst = true;
                            break;
                        }
                    }

                    if (!bInLst)
                        ptLst.Add(nPt);
                }

            }

            if (ptLst.Count % 2 == 1)
                return true;
            else
                return false;
        }

        public static Line MoveLine(Line srcLine, Vector3d moveDir, double moveDis)
        {
            if (IsAlmostNearZero(moveDis))
                return srcLine;

            var ptS = srcLine.StartPoint;
            var ptE = srcLine.EndPoint;

            var offsetVec = moveDir * moveDis;
            var resPts = ptS + offsetVec;
            var resPtE = ptE + offsetVec;

            return new Line(resPts, resPtE);
        }

        /// <summary>
        /// 计算轮廓的矩形布置信息
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static PlaceRect CalculateProfileRectInfo(Polyline poly)
        {
            var pts = new List<Point3d>();

            var srcPts = poly.Polyline2Point2d();
            var xLst = srcPts.Select(e => e.X).ToList();
            var yLst = srcPts.Select(e => e.Y).ToList();

            var xMin = xLst.Min();
            var yMin = yLst.Min();

            var xMax = xLst.Max();
            var yMax = yLst.Max();

            var leftBottmPt = new Point3d(xMin, yMin, 0);
            var rightBottomPt = new Point3d(xMax, yMin, 0);
            var bottomLine = new Line(leftBottmPt, rightBottomPt);

            var leftTopPt = new Point3d(xMin, yMax, 0);

            var rightTopPt = new Point3d(xMax, yMax, 0);
            var leftLine = new Line(leftBottmPt, leftTopPt);

            var topLine = new Line(leftTopPt, rightTopPt);

            var rightLine = new Line(rightBottomPt, rightTopPt);
            var placeRect = new PlaceRect(bottomLine, leftLine, topLine, rightLine);
            return placeRect;
        }

        /// <summary>
        /// 计算多段线内一点
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static Point3d? GetCenterPt(Polyline poly)
        {
            var ptLst = poly.Polyline2Point2d();
            ptLst.Remove(ptLst.Last());

            if (ptLst.Count < 3)
                return null;

            double xSum = 0.0;
            double ySum = 0.0;

            ptLst.ForEach(e => { xSum += e.X; });
            ptLst.ForEach(e => { ySum += e.Y; });

            return new Point3d(xSum / ptLst.Count, ySum / ptLst.Count, 0);
        }
    }
}
