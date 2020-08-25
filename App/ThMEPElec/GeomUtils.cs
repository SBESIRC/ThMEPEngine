using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (IsAlmostNearZero(ptFirst.X - ptSecond.X, tolerance)
                && IsAlmostNearZero(ptFirst.Y - ptSecond.Y, tolerance))
                return true;

            return false;
        }

        /// 零值判断
        public static bool IsAlmostNearZero(double val, double tolerance = 1e-9)
        {
            if (val > -tolerance && val < tolerance)
                return true;

            return false;
        }
    }
}
