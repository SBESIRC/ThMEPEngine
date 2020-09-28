using System;
using NFox.Cad;
using DotNetARX;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADExtension
{
    public static class ThCurveExtension
    {
        /// <summary>
        /// 获取曲线的中点
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        public static Point3d GetMidpoint(this Curve curve)
        {
            double d1 = curve.GetDistanceAtParameter(curve.StartParam);
            double d2 = curve.GetDistanceAtParameter(curve.EndParam);
            return curve.GetPointAtDist(d1 + ((d2 - d1) / 2.0));
        }


        // Make sure the pt1 and pt2 are on the Curve before calling this method.
        //  https://spiderinnet1.typepad.com/blog/2012/10/autocad-net-isonpoint3d-curvegetclosestpointto-curvegetparameteratpoint.html
        public static double GetLength(this Curve ent, Point3d pt1, Point3d pt2)
        {
            double dist1 = ent.GetDistanceAtParameter(ent.GetParameterAtPoint(ent.GetClosestPointTo(pt1, false)));
            double dist2 = ent.GetDistanceAtParameter(ent.GetParameterAtPoint(ent.GetClosestPointTo(pt2, false)));

            return Math.Abs(dist1 - dist2);
        }

        public static double BulgeFromCurve(this Curve cv, bool clockwise)
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

        public static Curve ToCurve(this NurbCurve2d spline2d, PlanarEntity plane)
        {
#if ACAD_ABOVE_2014
            return Curve.CreateFromGeCurve(spline2d.To3D(plane));
#else
            if (spline2d.HasFitData)
            {
                NurbCurve2dFitData n2fd = spline2d.FitData;
                Point3dCollection p3ds = new Point3dCollection();
                foreach (Point2d p in n2fd.FitPoints) p3ds.Add(new Point3d(plane, p));
                Spline ent = new Spline(p3ds, new Vector3d(plane, n2fd.StartTangent), new Vector3d(plane, n2fd.EndTangent),
                    n2fd.KnotParam, n2fd.Degree, n2fd.FitTolerance.EqualPoint);
                return ent;
            }
            else
            {
                NurbCurve2dData n2fd = spline2d.DefinitionData;
                Point3dCollection p3ds = new Point3dCollection();
                DoubleCollection knots = new DoubleCollection(n2fd.Knots.Count);
                foreach (Point2d p in n2fd.ControlPoints) p3ds.Add(new Point3d(plane, p));
                foreach (double k in n2fd.Knots) knots.Add(k);
                double period = 0;
                Spline ent = new Spline(n2fd.Degree, n2fd.Rational,
                            spline2d.IsClosed(), spline2d.IsPeriodic(out period),
                            p3ds, knots, n2fd.Weights, n2fd.Knots.Tolerance, n2fd.Knots.Tolerance);
                return ent;
            }
#endif
        }

        public static Curve ToCurve(this CircularArc2d arc2d, PlanarEntity plane)
        {
#if ACAD_ABOVE_2014
            return Curve.CreateFromGeCurve(arc2d.To3D(plane));
#else
            Arc ent = new Arc();
            if (arc2d.IsClockWise)
            {
                ent.CreateArcSCE(
                    new Point3d(plane, arc2d.EndPoint),
                    new Point3d(plane, arc2d.Center),
                    new Point3d(plane, arc2d.StartPoint));
            }
            else
            {
                ent.CreateArcSCE(
                    new Point3d(plane, arc2d.StartPoint),
                    new Point3d(plane, arc2d.Center),
                    new Point3d(plane, arc2d.EndPoint));
            }
            return ent;
#endif
        }

        public static Curve ToCurve(this LineSegment2d line2d, PlanarEntity plane)
        {
#if ACAD_ABOVE_2014
            return Curve.CreateFromGeCurve(line2d.To3D(plane));
#else
            return new Line(
                new Point3d(plane, line2d.StartPoint),
                new Point3d(plane, line2d.EndPoint)
                );
#endif
        }

        public static PolylineSegmentCollection ToPolylineSegments(this NurbCurve2d nurbCurve, PlanarEntity plane)
        {
            if (nurbCurve.ToCurve(plane) is Spline spline)
            {
#if ACAD_ABOVE_2014
                if (spline.ToPolyline(false, true) is Polyline polyline)
                {
                    return new PolylineSegmentCollection(polyline);
                }
#else
                if (spline.ToPolyline() is Polyline polyline)
                {
                    return new PolylineSegmentCollection(polyline);
                }                
#endif
            }
            return null;
        }

        public static LinearEntity3d ToGeLine(this Line line)
        {
            return new Line3d(line.StartPoint, line.EndPoint);
        }

        public static Curve WashClone(this Curve curve)
        {
            return curve.ToCurve3d().ToCurve();
        }
    }
}
