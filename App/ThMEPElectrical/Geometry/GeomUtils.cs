using Autodesk.AutoCAD.DatabaseServices;
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
                    var pts = poly.Vertices();
                    var resPts = new Point3dCollection();
                    var vecFir = poly.GetFirstDerivative(ptS).GetNormal();
                    var extendPtS = ptS - vecFir * entityExtendDis;

                    var vecEnd = poly.GetFirstDerivative(ptE).GetNormal();
                    var extendPtE = ptE + vecEnd * entityExtendDis;
                    resPts.Add(extendPtS);
                    foreach (Point3d srcPt in pts)
                        resPts.Add(srcPt);
                    resPts.Add(extendPtE);
                    return resPts.ToPolyline();
                }
            }
            else if (srcCurve is Line line)
            {
                var ptS = line.StartPoint;
                var ptE = line.EndPoint;
                var vec = (ptE - ptS).GetNormal();
                return new Line(ptS - vec * entityExtendDis, ptE + vec * entityExtendDis);
            }

            return srcCurve.Clone() as Curve;
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
            var regions = RegionTools.CreateRegion(new Curve[] { postPoly });

            foreach (var region in regions)
            {
                var pt = region.GetWCSCCentroid().Point3D();
                if (GeomUtils.PtInLoop(postPoly, pt.Point2D()))
                    ptLst.Add(pt);
            }

            if (ptLst.Count < 1)
            {
                var centerPt = GetCenterPt(postPoly);
                if (centerPt.HasValue)
                    ptLst.Add(centerPt.Value);
                else
                    return ptLst;
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
