using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPLighting.ParkingStall.Geometry
{
    public class GeomUtils
    {
        /// 零值判断
        public static bool IsAlmostNearZero(double val, double tolerance = 1e-9)
        {
            if (val > -tolerance && val < tolerance)
                return true;
            return false;
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

        public static bool PtInLoop(Polyline polyline, Point3d pt)
        {
            if (polyline.Closed == false)
                return false;
            return polyline.IndexedContains(pt);
        }

        public static bool IsPointOnLine(Point3d pt, Line line, double tole = 1e-8)
        {
            var startPt = line.StartPoint;
            var endPt = line.EndPoint;

            if (IsAlmostNearZero(line.Angle - Math.PI * 0.5) || IsAlmostNearZero(line.Angle - Math.PI * 1.5))
            {
                if (IsAlmostNearZero(pt.X - startPt.X, tole))
                {
                    var y1 = Math.Abs(pt.Y - startPt.Y);
                    var y2 = Math.Abs(pt.Y - endPt.Y);
                    if (IsAlmostNearZero((y1 + y2 - line.Length), tole))
                        return true;
                }
            }
            else if (IsAlmostNearZero(line.Angle) || IsAlmostNearZero(line.Angle - Math.PI))
            {
                if (IsAlmostNearZero(pt.Y - startPt.Y, tole))
                {
                    var X1 = Math.Abs(pt.X - startPt.X);
                    var X2 = Math.Abs(pt.X - endPt.X);
                    if (IsAlmostNearZero((X1 + X2 - line.Length), tole))
                        return true;
                }
            }
            else
            {
                // 非垂直
                var maxx = Math.Max(startPt.X, endPt.X);
                var minX = Math.Min(startPt.X, endPt.X);

                var maxY = Math.Max(startPt.Y, endPt.Y);
                var minY = Math.Min(startPt.Y, endPt.Y);

                var equal = Math.Abs((pt.X - startPt.X) * (endPt.Y - startPt.Y) - (endPt.X - startPt.X) * (pt.Y - startPt.Y));

                if ((IsAlmostNearZero(equal, tole)) && (pt.X >= minX && pt.X <= maxx) && (pt.Y >= minY && pt.Y <= maxY))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<Curve> Polyline2Curves(Polyline polyline, bool copyLayer = true)
        {
            if (polyline == null)
                return null;

            var curves = new List<Curve>();
            if (polyline.Closed)
            {
                for (int i = 0; i < polyline.NumberOfVertices; i++)
                {
                    var bulge = polyline.GetBulgeAt(i);
                    if (IsAlmostNearZero(bulge))
                    {
                        LineSegment3d line3d = polyline.GetLineSegmentAt(i);
                        var line = new Line(line3d.StartPoint, line3d.EndPoint);
                        if (copyLayer)
                            line.Layer = polyline.Layer;
                        curves.Add(line);
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
                            if (copyLayer)
                                arc.Layer = polyline.Layer;
                            curves.Add(arc);
                        }
                    }
                }
            }
            else
            {
                for (int j = 0; j < polyline.NumberOfVertices - 1; j++)
                {
                    try
                    {
                        var bulge = polyline.GetBulgeAt(j);
                        if (IsAlmostNearZero(bulge))
                        {
                            LineSegment3d line3d = polyline.GetLineSegmentAt(j);
                            var line = new Line(line3d.StartPoint, line3d.EndPoint);
                            if (copyLayer)
                                line.Layer = polyline.Layer;
                            curves.Add(line);
                        }
                        else
                        {
                            var type = polyline.GetSegmentType(j);
                            if (type == SegmentType.Arc)
                            {
                                var arc3d = polyline.GetArcSegmentAt(j);
                                var normal = arc3d.Normal;
                                var axisZ = Vector3d.ZAxis;
                                var arc = new Arc();
                                if (normal.IsEqualTo(Vector3d.ZAxis.Negate()))
                                    arc.CreateArcSCE(arc3d.EndPoint, arc3d.Center, arc3d.StartPoint);
                                else
                                    arc.CreateArcSCE(arc3d.StartPoint, arc3d.Center, arc3d.EndPoint);
                                if (copyLayer)
                                    arc.Layer = polyline.Layer;
                                curves.Add(arc);
                            }
                        }
                    }
                    catch
                    { }
                }
            }

            return curves;
        }
    }
}
