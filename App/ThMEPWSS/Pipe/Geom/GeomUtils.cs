using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;

namespace ThMEPWSS.Pipe.Geom
{
    public class GeomUtils
    {
        public static bool PtInLoop(Polyline polyline, Point3d pt)
        {
            if (polyline.Closed == false)
                return false;
            return polyline.IndexedContains(pt);
        }

        public static bool Point3dIsEqualPoint3d(Point3d ptFirst, Point3d ptSecond, double tolerance = 1e-6)
        {
            return Point2dIsEqualPoint2d(ptFirst.ToPoint2D(), ptSecond.ToPoint2D(), tolerance);
        }


        public static bool Point2dIsEqualPoint2d(Point2d ptFirst, Point2d ptSecond, double tolerance = 1e-6)
        {
            return IsAlmostNearZero(ptFirst.X - ptSecond.X, tolerance)
                && IsAlmostNearZero(ptFirst.Y - ptSecond.Y, tolerance);
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
