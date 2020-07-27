// (C) Copyright 2008-2012 by COMSAL Srl - RSM
//
// COMSAL PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// COMSAL SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.
// COMSAL SRL DOES NOT WARRANT THAT THE OPERATION OF THE
// PROGRAM WILL BE UNINTERRUPTED OR ERROR FREE.
//
// Description:
// Classe di supporto per funzioni geometriche comuni
//
// History:
// 02.10.2008 [MC] Prima stesura
//
//

using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThCADCore.Geometry
{ 
    // Support class for non-trigonometric math
    public class csCosDir
    {
        public Double cx;
        public Double cy;
        public Double cz;

        public csCosDir()
        {
            cx = cy = cz = 0.0;
        }

        public csCosDir(Point2d p1, Point2d p2)
        {
            Double dx = p2.X - p1.X;
            Double dy = p2.Y - p1.Y;
            Double dd = Math.Sqrt(dx * dx + dy * dy);

            if (dd > csMath.dEpsilon)
            {
                cx = dx / dd;
                cy = dy / dd;
            }
        }

        public csCosDir(Point3d p1, Point3d p2)
        {
            Double dx = p2.X - p1.X;
            Double dy = p2.Y - p1.Y;
            Double dz = p2.Z - p1.Z;
            Double dd = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dd > csMath.dEpsilon)
            {
                cx = dx / dd;
                cy = dy / dd;
                cz = dz / dd;
            }
        }
    }

    // Common math functions
    // https://www.keanw.com/2012/09/overriding-the-grips-of-an-autocad-polyline-to-maintain-fillet-segments-using-net.html
    public static class csMath
    {
        public const Double PI =
          3.141592653589793; // More decimal places than Math.PI
        public const Double dEpsilon =
          0.001; // General precision allowed for identity

        /// <summary>
        /// Check intersection for two segments on plane pPlane
        /// </summary>
        /// <param name="l1">First segment</param>
        /// <param name="l2">Second segment</param>
        /// <param name="pPlane">Working plane</param>
        /// <param name="pOut">Resulting point, if available</param>
        /// <returns></returns>
        public static bool CheckIntersect(
          LineSegment3d l1,
          LineSegment3d l2,
          Plane pPlane,
          ref Point3d pOut
        )
        {
            bool result = false;
            // Get 2d points on working plane
            Point2d p1 = l1.StartPoint.Convert2d(pPlane);
            Point2d p2 = l1.EndPoint.Convert2d(pPlane);
            Point2d q1 = l2.StartPoint.Convert2d(pPlane);
            Point2d q2 = l2.EndPoint.Convert2d(pPlane);
            Point2d pInt = Point2d.Origin;
            IntersectLinesState res = IntersectLines(p1, p2, q1, q2, out pInt);
            if (res == IntersectLinesState.ApparentIntersect ||
                res == IntersectLinesState.RealIntersect)
            {
                pOut = new Point3d(pPlane, pInt);
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Possible results for IntersectLines() function
        /// </summary>
        public enum IntersectLinesState
        {
            InvalidPoints = -1,    // Invalid points (coincident?)
            RealIntersect = 0,     // Real intersection found, pInt valid
            ApparentIntersect = 1, // Apparent inters. found, pInt valid
            NoIntersection = 2,    // Segments are parallel
            OverLapping = 3,       // Segments are overlapping
            Colinear = 4           // Segments are co-linear
        }

        /// <summary>
        /// Try to get intersection point of two segment.
        /// The intersection may be real or apparent.
        /// The resulting point is
        /// </summary>
        /// <param name="p1">Start point of first segment</param>
        /// <param name="p2">End point of first segment</param>
        /// <param name="q1">Start point of second segment</param>
        /// <param name="q2">End point of second segment</param>
        /// <param name="pInt">[out] Resulting intersection point</param>
        /// <returns>Result validity state</returns>
        ///
        public static IntersectLinesState IntersectLines(
            Point2d p1, Point2d p2, // First segment
            Point2d q1, Point2d q2, // Second segment
            out Point2d pInt        // Intersecting point
        )
        {
            IntersectLinesState result = IntersectLinesState.NoIntersection;
            pInt = Point2d.Origin;

            // Get sine/cosine coefficients for the two segments
            csCosDir r1 = new csCosDir(p1, p2);
            csCosDir r2 = new csCosDir(q1, q2);

            // Check coefficients, if points are coincident,
            // segments are null
            if ((r1.cx == 0.0 && r1.cy == 0.0) ||
                (r2.cx == 0.0 && r2.cy == 0.0)
            )
            {
                // Coincident points? Invalid segments data
                return IntersectLinesState.InvalidPoints;
            }

            // Intersection coefficients
            Double a1 = r1.cx, a2 = r1.cy;
            Double b1 = -r2.cx, b2 = -r2.cy;
            Double c1 = q1.X - p1.X, c2 = q1.Y - p1.Y;
            // Get denominator, if null, segments have same direction
            Double dden = a1 * b2 - a2 * b1;
            if (Math.Abs(dden) > csMath.dEpsilon)
            {
                // Valid denominator, lines are not parallels or co-linear
                // now, intersection linear parameter may be get either
                // from first or second segment. Do both to check for
                // real or apparent intersection.
                // Linear parameter for second segment
                Double tt = (c1 * b2 - c2 * b1) / dden;
                // Linear parameter for first segment
                Double vv = (c2 * a1 - c1 * a2) / dden;
                // Intersection point from first segment parameter
                pInt = new Point2d(q1.X + r2.cx * vv, q1.Y + r2.cy * vv);
                // To be 'real', intersection point must lay on both
                // segments (parameter from 0 to 1). Otherwise, we set
                // it as 'apparent'. Get normalized linear parameter
                // (at this stage denominator are intrinsicly valid,
                // no need to check for DIVBYZERO)
                tt = tt / p1.GetDistanceTo(p2);
                vv = vv / q1.GetDistanceTo(q2);
                // Check if both coefficients lay within 0 and 1
                if (
                  tt > -csMath.dEpsilon && tt < (1 + csMath.dEpsilon) &&
                  vv > -csMath.dEpsilon && vv < (1 + csMath.dEpsilon)
                )
                {
                    result = IntersectLinesState.RealIntersect;
                }
                else
                {
                    result = IntersectLinesState.ApparentIntersect;
                }
            }
            else
            {
                // Segments are parallel or co-linear (have same direction).
                // Check coefficients for a new connecting segment with
                // points taken from both original segment. If coefficients
                // are the same, segments are co-linear, otherwise are
                // parallel
                csCosDir rx;
                // Avoid coincident points
                if (p1.GetDistanceTo(q1) > csMath.dEpsilon)
                    rx = new csCosDir(p1, q1);
                else
                    rx = new csCosDir(p1, q2);

                if (Math.Abs(rx.cx - r1.cx) < csMath.dEpsilon &&
                    Math.Abs(rx.cy - r1.cy) < csMath.dEpsilon
                )
                {
                    // Same coefficient, segments lay on the same vector.
                    // Check if there is any overlapping by checking distances
                    // from the two ends of a segments and another end.
                    // Sum of distances must be equal or higher than segment
                    // length, otherwise they are partially overlapping
                    Double ll = p2.GetDistanceTo(p1);
                    bool bOver1 = q1.GetDistanceTo(p1) + q1.GetDistanceTo(p2) > ll + csMath.dEpsilon;
                    bool bOver2 = q2.GetDistanceTo(p1) + q2.GetDistanceTo(p2) > ll + csMath.dEpsilon;
                    result = bOver1 && bOver2 ? IntersectLinesState.Colinear : IntersectLinesState.OverLapping;
                }
                else
                {
                    // Parallel segments, no intersection
                    result = IntersectLinesState.NoIntersection;
                }
            }
            return result;
        }
    }
}
