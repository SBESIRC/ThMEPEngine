using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPLighting.ParkingStall.Assistant;

namespace ThMEPLighting.ParkingStall.Worker.LightAdjustor
{
    public class CenterBaseLineCalculator
    {
        private List<Line> m_lines;

        public Point3d CentroidPoint;

        public CenterBaseLineCalculator(List<Line> lines)
        {
            m_lines = lines;
        }

        public static Point3d MakeBaseLineCalculator(List<Line> lines)
        {
            var baseLineCalculator = new CenterBaseLineCalculator(lines);
            baseLineCalculator.Do();
            return baseLineCalculator.CentroidPoint;
        }

        public void Do()
        {
            var coorTransform = new CoordinateTransform(m_lines);
            coorTransform.Do();
            var transLines = coorTransform.TransLines;

            var pts = CalculatePoints(transLines);
            var poly = CalculateProfile(pts);

            var transPoly = poly.GetTransformedCopy(coorTransform.m_matrix3D.Inverse()) as Polyline;

            //DrawUtils.DrawProfileDebug(new List<Curve>() { transPoly }, "CenterBaseLine");
            CentroidPoint = transPoly.GetCentroidPoint();
        }

        private List<Point3d> CalculatePoints(List<Line> lines)
        {
            var pts = new List<Point3d>();
            lines.ForEach(line =>
            {
                pts.Add(line.StartPoint);
                pts.Add(line.EndPoint);
            });

            return pts;
        }

        private Polyline CalculateProfile(List<Point3d> ptLst)
        {
            var xLst = ptLst.Select(e => e.X).ToList();
            var yLst = ptLst.Select(e => e.Y).ToList();

            var xMin = xLst.Min();
            var yMin = yLst.Min();

            var xMax = xLst.Max() + 0.5;
            var yMax = yLst.Max() + 0.5;
            var leftBottomPt = new Point3d(xMin, yMin, 0);
            var rightTopPt = new Point3d(xMax, yMax, 0);
            var rightBottomPt = new Point3d(xMax, yMin, 0);
            var leftTopPt = new Point3d(xMin, yMax, 0);

            var pts = new List<Point3d>();
            pts.Add(leftBottomPt);
            pts.Add(rightBottomPt);
            pts.Add(rightTopPt);
            pts.Add(leftTopPt);

            return GeometryTransfer.Points2Poly(pts);
        }
    }
}
