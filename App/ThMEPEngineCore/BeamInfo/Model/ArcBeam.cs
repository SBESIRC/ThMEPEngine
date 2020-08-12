using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BeamInfo.Model
{
    public class ArcBeam : Beam
    {
        public ArcBeam(Curve upLine, Curve downLine)
        {
            if ((upLine as Arc).Length > (downLine as Arc).Length)
            {
                UpBeamLine = upLine;
                DownBeamLine = downLine;
            }
            else
            {
                UpBeamLine = downLine;
                DownBeamLine = upLine;
            }

            UpStartPoint = UpBeamLine.StartPoint;
            UpEndPoint = UpBeamLine.EndPoint;
            if (UpStartPoint.DistanceTo(DownBeamLine.StartPoint) < UpStartPoint.DistanceTo(DownBeamLine.EndPoint))
            {
                DownStartPoint = DownBeamLine.StartPoint;
                DownEndPoint = DownBeamLine.EndPoint;
            }
            else
            {
                DownStartPoint = DownBeamLine.EndPoint;
                DownEndPoint = DownBeamLine.StartPoint;
            }

            CenterPoint = (upLine as Arc).Center;
            Vector3d proV = Vector3d.ZAxis.CrossProduct(UpEndPoint - UpStartPoint).GetNormal();
            Point3d proP1 = upLine.GetClosestPointTo(CenterPoint, proV, false);
            Point3d proP2 = downLine.GetClosestPointTo(CenterPoint, proV, false);
            if (CenterPoint.DistanceTo(proP1) > CenterPoint.DistanceTo(proP2))
            {
                MiddlePoint = proP1;
            }
            else
            {
                MiddlePoint = proP2;
            }
            BeamNormal = (MiddlePoint - CenterPoint).GetNormal();

            BeamSPointSolid = CreatePolyline(UpStartPoint, DownStartPoint, BeamNormal, 10);
            BeamEPointSolid = CreatePolyline(UpEndPoint, DownEndPoint, BeamNormal, 10);
        }

        public override Polyline BeamBoundary
        {
            get
            {
                if (UpBeamLine == null || DownBeamLine == null)
                {
                    return null;
                }

                double upBluge = Math.Tan((UpBeamLine as Arc).TotalAngle / 4);
                double downBluge = Math.Tan((DownBeamLine as Arc).TotalAngle / 4);
                Polyline resPolyline = new Polyline(4)
                {
                    Closed = true,
                };
                resPolyline.AddVertexAt(0, new Point2d(UpBeamLine.StartPoint.X, UpBeamLine.StartPoint.Y), upBluge, 0, 0);
                resPolyline.AddVertexAt(1, new Point2d(UpBeamLine.EndPoint.X, UpBeamLine.EndPoint.Y), 0, 0, 0);
                if (UpBeamLine.EndPoint.DistanceTo(DownBeamLine.StartPoint) < UpBeamLine.EndPoint.DistanceTo(DownBeamLine.EndPoint))
                {
                    resPolyline.AddVertexAt(2, new Point2d(DownBeamLine.StartPoint.X, DownBeamLine.StartPoint.Y), downBluge, 0, 0);
                    resPolyline.AddVertexAt(3, new Point2d(DownBeamLine.EndPoint.X, DownBeamLine.EndPoint.Y), 0, 0, 0);
                }
                else
                {
                    resPolyline.AddVertexAt(2, new Point2d(DownBeamLine.EndPoint.X, DownBeamLine.EndPoint.Y), -downBluge, 0, 0);
                    resPolyline.AddVertexAt(3, new Point2d(DownBeamLine.StartPoint.X, DownBeamLine.StartPoint.Y), 0, 0, 0);
                }
                return resPolyline;
            }
        }

        public Point3d MiddlePoint { get; set; }

        public Point3d CenterPoint { get; set; }
    }
}
