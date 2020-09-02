using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Assistant;

namespace ThMEPElectrical.Geometry
{
    /// <summary>
    /// 计算ABB多段线
    /// </summary>
    public class ABBRectangle
    {
        private Polyline m_srcPoly;

        private Polyline m_boundPoly;

        /// <summary>
        /// ABB多段线
        /// </summary>
        private Polyline ABBPolyline
        {
            get { return m_boundPoly; }
        }

        public static Polyline MakeABBPolyline(Polyline poly)
        {
            var abbRectangle = new ABBRectangle(poly);
            abbRectangle.Do();
            return abbRectangle.ABBPolyline;
        }

        private ABBRectangle(Polyline poly)
        {
            m_srcPoly = poly;
        }

        private void Do()
        {
            var pts = m_srcPoly.Polyline2Point2d();
            var pt3ds = pts.Pt2stoPt3ds();
            var circles = GeometryTrans.Points2Circles(pt3ds, 100, Vector3d.ZAxis);
            DrawUtils.DrawProfile(GeometryTrans.Circles2Curves(circles), "kkkk");
            CalculateBounds(pts);
        }

        /// <summary>
        /// 计算ABB框
        /// </summary>
        /// <param name="ptLst"></param>
        private void CalculateBounds(List<Point2d> ptLst)
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

            var circles = GeometryTrans.Points2Circles(pts, 100, Vector3d.ZAxis);
            DrawUtils.DrawProfile(GeometryTrans.Circles2Curves(circles), "pl2pt");
            m_boundPoly = GeometryTrans.Points2Poly(pts);
        }
    }
}
