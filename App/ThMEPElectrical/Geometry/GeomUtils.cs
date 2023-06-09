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
using ThCADCore.NTS;
using ThMEPElectrical.PostProcess;
using ThCADExtension;
using Dreambuild.AutoCAD;
using NFox.Cad;

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

        public static bool PtInLoop(Polyline polyline, Point3d pt)
        {
            if (polyline.Closed == false)
                return false;
            return polyline.IndexedContains(pt);
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

        public static Line CalculateMidLine(Line first, Polyline sec)
        {
            var pts = CurveIntersectCurve(first, sec);
            if (pts.Count < 2)
                return first;

            var startPt = first.StartPoint;
            pts.Sort((p1, p2) => { return p1.DistanceTo(startPt).CompareTo(p2.DistanceTo(startPt)); });

            return new Line(pts.First(), pts.Last());
        }

        public static List<Point3d> CurveIntersectCurve(Curve curveFir, Curve curveSec)
        {
            var ptCol = new Point3dCollection();
            curveFir.IntersectWith(curveSec, Intersect.OnBothOperands, ptCol, (IntPtr)0, (IntPtr)0);

            return ptCol.ToPointList();
        }

        public static double Rad2Angle(double rad)
        {
            return rad / Math.PI * 180;
        }

        /// <summary>
        /// 获取框选范围的点的集合
        /// </summary>
        /// <param name="ptFir"></param>
        /// <param name="ptSec"></param>
        /// <returns></returns>
        public static Point3dCollection CalculateRectangleFromPoints(Point3d ptFir, Point3d ptSec)
        {
            var point3dCollection = new Point3dCollection();
            double xMin;
            double xMax;

            double yMin;
            double yMax;
            if (ptFir.X < ptSec.X)
            {
                xMin = ptFir.X;
                xMax = ptSec.X;
            }
            else
            {
                xMin = ptSec.X;
                xMax = ptFir.X;
            }

            if (ptFir.Y < ptSec.Y)
            {
                yMin = ptFir.Y;
                yMax = ptSec.Y;
            }
            else
            {
                yMin = ptSec.Y;
                yMax = ptFir.Y;
            }

            var leftBottmPt = new Point3d(xMin, yMin, 0);
            var rightBottomPt = new Point3d(xMax, yMin, 0);

            var leftTopPt = new Point3d(xMin, yMax, 0);

            var rightTopPt = new Point3d(xMax, yMax, 0);

            point3dCollection.Add(leftBottmPt);
            point3dCollection.Add(rightBottomPt);
            point3dCollection.Add(rightTopPt);
            point3dCollection.Add(leftTopPt);


            return point3dCollection;
        }

        public static Polyline BufferPoly(Polyline polyline)
        {
            var polys = new List<Polyline>();
            foreach (Polyline offsetPoly in polyline.Buffer(ThMEPCommon.ShrinkSmallDistance))
                polys.Add(offsetPoly);

            if (polys.Count == 0)
                return null;
            polys.Sort((p1, p2) =>
            {
                return p1.Area.CompareTo(p2.Area);
            });
            return polys.Last();
        }

        public static List<Polyline> BufferPolys(List<Polyline> srcPolys, double shrinkDis)
        {
            var resPolys = new List<Polyline>();
            foreach (var singlePoly in srcPolys)
            {
                foreach (Polyline offsetPoly in singlePoly.Buffer(shrinkDis))
                {
                    resPolys.Add(offsetPoly);
                }
            }

            return resPolys;
        }

        public static List<Polyline> CalculateCanBufferPolys(List<Polyline> srcPolys, double shrinkDis)
        {
            var resPolys = new List<Polyline>();
            foreach (var singlePoly in srcPolys)
            {
                if (IsCanBuffer(singlePoly, shrinkDis))
                    resPolys.Add(singlePoly);
            }

            return resPolys;
        }

        public static bool IsCanBuffer(Polyline poly, double shrinkDis)
        {
            if (poly.Buffer(shrinkDis).Count > 0)
                return true;

            return false;
        }

        public static DBObjectCollection Curves2DBCollection(List<Curve> curves)
        {
            return curves.OfType<DBObject>().ToCollection();
        }

        public static List<Curve> EraseSameObjects(List<Curve> srcCurves)
        {
            var objs = Curves2DBCollection(srcCurves);
            return ThCADCoreNTSGeometryFilter.GeometryEquality(objs).OfType<Curve>().ToList();
        }

        public static Polyline OptimizePolyline(Polyline polyline)
        {
            var pts = polyline.Vertices();
            var resPts = new Point3dCollection();
            for (int i = 0; i < pts.Count; i++)
            {
                resPts.Add(new Point3d(pts[i].X, pts[i].Y, 0));
            }

            var resPoly = new Polyline();
            resPoly.CreatePolyline(resPts);
            return resPoly;
        }

        public static Curve ExtendCurve(Curve srcCurve, double entityExtendDis)
        {
            if (srcCurve is Polyline poly)
            {
                var ptS = poly.StartPoint;
                var ptE = poly.EndPoint;
                if (ptS.DistanceTo(ptE) < ThMEPCommon.PolyClosedDistance)
                {
                    var clonePoly = poly.Clone() as Polyline;
                    clonePoly.Closed = true;
                    return clonePoly;
                }
                else
                {
                    var resPolyline = OptimizePolyline(poly);
                    var pts = resPolyline.Vertices();
                    var resPts = new Point3dCollection();
                    var vecFir = resPolyline.GetFirstDerivative(ptS).GetNormal();
                    var extendPtS = ptS - vecFir * entityExtendDis;

                    var vecEnd = resPolyline.GetFirstDerivative(ptE).GetNormal();
                    var extendPtE = ptE + vecEnd * entityExtendDis;
                    resPts.Add(extendPtS);
                    foreach (Point3d srcPt in pts)
                        resPts.Add(srcPt);
                    resPts.Add(extendPtE);
                    var extendPoly = new Polyline();
                    extendPoly.CreatePolyline(resPts);
                    return extendPoly;
                }
            }
            else
            {
                // 直线
                var line = srcCurve as Line;
                var ptS = line.StartPoint;
                var ptE = line.EndPoint;
                var vec = (ptE - ptS).GetNormal();
                return new Line(ptS - vec * entityExtendDis, ptE + vec * entityExtendDis);
            }
        }

        public static double CutRadRange(double rad)
        {
            if (IsAlmostNearZero(rad - 1))
                return 1;
            else if (IsAlmostNearZero(rad + 1))
                return -1;
            return rad;
        }

        public static double CalAngle(Vector3d from, Vector3d to)
        {
            double val = from.X * to.X + from.Y * to.Y;
            double tmp = Math.Sqrt(Math.Pow(from.X, 2) + Math.Pow(from.Y, 2)) * Math.Sqrt(Math.Pow(to.X, 2) + Math.Pow(to.Y, 2));
            double angleRad = Math.Acos(CutRadRange(val / tmp));
            if (IsAlmostNearZero((angleRad - Math.PI)))
                return angleRad;
            if (CrossProduct(from, to) < 0)
                return -angleRad;
            return angleRad;
        }

        public static double CrossProduct(Vector3d from, Vector3d to)
        {
            return (from.X * to.Y - from.Y * to.X);
        }

        /// <summary>
        /// 计算轮廓的矩形布置信息
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static Polyline CalculateRectPoly(List<Point3d> srcPts)
        {
            var pts = new Point3dCollection();

            var xLst = srcPts.Select(e => e.X).ToList();
            var yLst = srcPts.Select(e => e.Y).ToList();

            var xMin = xLst.Min();
            var yMin = yLst.Min();

            var xMax = xLst.Max();
            var yMax = yLst.Max();

            var leftBottmPt = new Point3d(xMin, yMin, 0);
            var rightBottomPt = new Point3d(xMax, yMin, 0);
            var leftTopPt = new Point3d(xMin, yMax, 0);
            var rightTopPt = new Point3d(xMax, yMax, 0);

            pts.Add(leftBottmPt);
            pts.Add(rightBottomPt);
            pts.Add(rightTopPt);
            pts.Add(leftTopPt);
            return pts.ToPolyline();
        }

        public static bool IsValidSinglePlace(double hLength, double wLength, double rLength)
        {
            if (Math.Pow(hLength, 2) + Math.Pow(wLength, 2) < Math.Pow(rLength, 2) * 4)
                return true;

            return false;
        }

        public static bool IsValidSinglePlace(Polyline poly, Point3d pt, double rLength)
        {
            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                if (poly.GetPoint3dAt(i).DistanceTo(pt) > rLength)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsIntersect(Polyline firstPly, Polyline secPly)
        {
            if (GeomUtils.IsIntersectValid(firstPly, secPly))
            {
                var ptLst = new Point3dCollection();
                firstPly.IntersectWith(secPly, Intersect.OnBothOperands, ptLst, (IntPtr)0, (IntPtr)0);
                if (ptLst.Count != 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsIntersectValid(Polyline firstPly, Polyline secPly)
        {
            // first
            var firstExtend3d = firstPly.Bounds.Value;
            var firMinPt = firstExtend3d.MinPoint;
            var firMaxPt = firstExtend3d.MaxPoint;
            double firLeftX = firMinPt.X;
            double firLeftY = firMinPt.Y;
            double firRightX = firMaxPt.X;
            double firRightY = firMaxPt.Y;

            //second
            var secExtend3d = secPly.Bounds.Value;
            var secMinPt = secExtend3d.MinPoint;
            var secMaxPt = secExtend3d.MaxPoint;
            double secLeftX = secMinPt.X;
            double secLeftY = secMinPt.Y;
            double secRightX = secMaxPt.X;
            double secRightY = secMaxPt.Y;

            firLeftX -= 0.1;
            firLeftY -= 0.1;
            firRightX += 0.1;
            firRightY += 0.1;

            secLeftX -= 0.1;
            secLeftY -= 0.1;
            secRightX += 0.1;
            secRightY += 0.1;

            if (Math.Min(firRightX, secRightX) >= Math.Max(firLeftX, secLeftX)
                && Math.Min(firRightY, secRightY) >= Math.Max(firLeftY, secLeftY))
                return true;

            return false;
        }

        public static DetectionPolygon MPolygon2PolygonInfo(MPolygon polygon)
        {
            Polyline shell = null;
            List<Polyline> holes = new List<Polyline>();
            for (int i = 0; i < polygon.NumMPolygonLoops; i++)
            {
                LoopDirection direction = polygon.GetLoopDirection(i);
                MPolygonLoop mPolygonLoop = polygon.GetMPolygonLoopAt(i);
                Polyline polyline = new Polyline()
                {
                    Closed = true
                };

                for (int j = 0; j < mPolygonLoop.Count; j++)
                {
                    var bulgeVertex = mPolygonLoop[j];
                    polyline.AddVertexAt(j, bulgeVertex.Vertex, bulgeVertex.Bulge, 0, 0);
                }
                if (LoopDirection.Exterior == direction)
                {
                    shell = polyline;
                }
                else if (LoopDirection.Interior == direction)
                {
                    holes.Add(polyline);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            if (shell == null)
                throw new NotSupportedException("shell is null");

            return new DetectionPolygon(shell, holes);
        }

        public static PolygonInfo MPolygon2Polygon(MPolygon mPolygon)
        {
            var polygon = MPolygon2PolygonInfo(mPolygon);
            return new PolygonInfo(polygon.Shell, polygon.Holes);
        }

        /// <summary>
        /// 计算几何中心
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static List<Point3d> CalculateCentroidFromPoly(Polyline poly)
        {
            var ptLst = new List<Point3d>();
            var postPoly = BufferPoly(poly);
            if (postPoly == null)
                return ptLst;

            postPoly = postPoly.RemoveNearSamePoints().Points2PointCollection().ToPolyline();
            var centriod = postPoly.GetCentroidPoint();
            if (GeomUtils.PtInLoop(postPoly, centriod))
            {
                ptLst.Add(centriod);
            }
            if (ptLst.Count < 1)
            {
                ptLst.Add(postPoly.MinimumBoundingCircle().Center);
            }

            // 距离500的边界调整
            var polys = new List<Polyline>();
            foreach (Polyline offsetPoly in postPoly.Buffer(ThMEPCommon.ShrinkDistance))
                polys.Add(offsetPoly);

            //var drawPts = new List<Point3d>();
            //ptLst.ForEach(e => drawPts.Add(new Point3d(e.X, e.Y, 0)));
            //var circles = GeometryTrans.Points2Circles(drawPts, 100, Vector3d.ZAxis);
            //var circleCurves = GeometryTrans.Circles2Curves(circles);

            //DrawUtils.DrawProfile(circleCurves, "drawPts");
            //DrawUtils.DrawProfile(new List<Curve>() { postPoly }, "postPoly");

            //DrawUtils.DrawProfile(polys.Polylines2Curves(), "singlePlace");
            if (polys.Count > 0)
            {
                var mainRegion = new MainSecondBeamRegion(polys, ptLst);
                return MainSecondBeamPointAdjustor.MakeMainBeamPointAdjustor(mainRegion, MSPlaceAdjustorType.SINGLEPLACE);
            }

            // 无效布置点
            ptLst.Clear();
            return ptLst;
        }
    }
}
