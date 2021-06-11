using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Geom
{
    public class GeomUtils
    {
        public static bool PtInLoop(Polyline polyline, Point3d pt)
        {
            if (polyline != null)
            {
                if (polyline.Closed == false)
                    return false;
                return polyline.IndexedContains(pt);
            }
            return false;
        }

        public static bool Point3dIsEqualPoint3d(Point3d ptFirst, Point3d ptSecond)
        {
            return ptFirst.IsEqualTo(ptSecond, ThMEPEngineCoreCommon.DEFAULT_THMAP_TOLERANCE);
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
