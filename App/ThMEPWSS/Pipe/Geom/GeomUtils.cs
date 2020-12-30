using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

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

        public static List<Point3d> CalculatePoints(List<Polyline> polylines)
        {
            var pts = new List<Point3d>();
            foreach (var polyline in polylines)
            {
                foreach (Point3d pt in polyline.Vertices())
                {
                    pts.Add(pt);
                }
            }

            return pts;
        }

        public static Polyline CalculateProfile(List<Point3d> ptLst)
        {
            var xLst = ptLst.Select(e => e.X).ToList();
            var yLst = ptLst.Select(e => e.Y).ToList();

            var xMin = xLst.Min();
            var yMin = yLst.Min();

            var xMax = xLst.Max();
            var yMax = yLst.Max();
            var leftBottomPt = new Point3d(xMin, yMin, 0);
            var rightTopPt = new Point3d(xMax, yMax, 0);
            var rightBottomPt = new Point3d(xMax, yMin, 0);
            var leftTopPt = new Point3d(xMin, yMax, 0);

            var pts = new List<Point3d>();
            pts.Add(leftBottomPt);
            pts.Add(rightBottomPt);
            pts.Add(rightTopPt);
            pts.Add(leftTopPt);

            return Points2Poly(pts);
        }

        public static Polyline Points2Poly(List<Point3d> pts)
        {
            if (pts == null || pts.Count < 3)
                return null;

            var ptFirst = pts.First();
            var ptLast = pts.Last();

            if (GeomUtils.Point3dIsEqualPoint3d(ptFirst, ptLast))
                pts.Remove(ptLast);

            var poly = new Polyline()
            {
                Closed = true
            };

            for (int i = 0; i < pts.Count; i++)
            {
                poly.AddVertexAt(i, pts[i].ToPoint2D(), 0, 0, 0);
            }
            return poly;
        }
    }
}
