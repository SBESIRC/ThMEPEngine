using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.BeamInfo.Model
{
    public class LineBeam : Beam
    {
        public LineBeam(Curve newUpLine, Curve newDownLine)
        {
            Curve shortLine, longLine;
            if (newUpLine.GetLength() < newDownLine.GetLength())
            {
                shortLine = newUpLine;
                longLine = newDownLine;
            }
            else
            {
                shortLine = newDownLine;
                longLine = newUpLine;
            }
            BeamNormal = (shortLine as Line).Delta.GetNormal();
            if (longLine.GetLength() - shortLine.GetLength() > 300)
            {
                UpStartPoint = shortLine.StartPoint;
                UpEndPoint = shortLine.EndPoint;
                DownStartPoint = longLine.GetClosestPointTo(UpStartPoint, false);
                DownEndPoint = longLine.GetClosestPointTo(UpEndPoint, false);
            }
            else
            {
                DownStartPoint = longLine.StartPoint;
                DownEndPoint = longLine.EndPoint;
                UpStartPoint = shortLine.GetClosestPointTo(DownStartPoint, false);
                UpEndPoint = shortLine.GetClosestPointTo(DownEndPoint, false);
            }
            

            BeamSPointSolid = CreatePolyline(UpStartPoint, DownStartPoint, -BeamNormal, 10);
            BeamEPointSolid = CreatePolyline(UpEndPoint, DownEndPoint, BeamNormal, 10);
        }

        public override Polyline BeamBoundary
        {
            get
            {
                Polyline resPolyline = new Polyline(4)
                {
                    Closed = true,
                };
                resPolyline.AddVertexAt(0, new Point2d(UpStartPoint.X, UpStartPoint.Y), 0, 0, 0);
                resPolyline.AddVertexAt(1, new Point2d(DownStartPoint.X, DownStartPoint.Y), 0, 0, 0);
                resPolyline.AddVertexAt(2, new Point2d(DownEndPoint.X, DownEndPoint.Y), 0, 0, 0);
                resPolyline.AddVertexAt(3, new Point2d(UpEndPoint.X, UpEndPoint.Y), 0, 0, 0);

                return resPolyline;
            }
        }

        public void GetOrderPoints(out Point3d upLP, out Point3d upMP, out Point3d upRP, out Point3d downLP, out Point3d downMP, out Point3d downRP)
        {
            upLP = UpStartPoint;
            upRP = UpEndPoint;
            upMP = new Point3d((UpStartPoint.X + UpEndPoint.X) / 2, (UpStartPoint.Y + UpEndPoint.Y) / 2, 0);
            downLP = DownStartPoint;
            downRP = DownEndPoint;
            downMP = new Point3d((DownStartPoint.X + DownEndPoint.X) / 2, (DownStartPoint.Y + DownEndPoint.Y) / 2, 0);

            Vector3d moveDir = (upMP - downMP).GetNormal();
            if (Math.Abs(moveDir.Y) < 0.0001)
            {
                if (moveDir.X > 0)
                {
                    Point3d tempP = upMP;
                    upMP = downMP;
                    downMP = tempP;
                    Vector3d judgeDir = (upRP - upLP).GetNormal().CrossProduct(-moveDir);
                    if (judgeDir.Z < 0)
                    {
                        upRP = DownStartPoint;
                        upLP = DownEndPoint;
                        downRP = UpStartPoint;
                        downLP = UpEndPoint;
                    }
                    else
                    {
                        upLP = DownStartPoint;
                        upRP = DownEndPoint;
                        downLP = UpStartPoint;
                        downRP = UpEndPoint;
                    }
                }
                else
                {
                    Vector3d judgeDir = (upRP - upLP).GetNormal().CrossProduct(moveDir);
                    if (judgeDir.Z < 0)
                    {
                        upLP = UpEndPoint;
                        upRP = UpStartPoint;
                        downLP = DownEndPoint;
                        downRP = DownStartPoint;
                    }
                }
            }
            else if (moveDir.Y < 0)
            {
                Point3d tempP = upMP;
                upMP = downMP;
                downMP = tempP;
                Vector3d judgeDir = (upLP - upRP).GetNormal().CrossProduct(-moveDir);
                if (judgeDir.Z > 0)
                {
                    
                    upRP = DownStartPoint;
                    upLP = DownEndPoint;
                    downRP = UpStartPoint;
                    downLP = UpEndPoint;
                }
                else
                {
                    upLP = DownStartPoint;
                    upRP = DownEndPoint;
                    downLP = UpStartPoint;
                    downRP = UpEndPoint;
                }
            }
            else
            {
                Vector3d judgeDir = (upRP - upLP).GetNormal().CrossProduct(moveDir);
                if (judgeDir.Z < 0)
                {
                    upLP = UpEndPoint;
                    upRP = UpStartPoint;
                    downLP = DownEndPoint;
                    downRP = DownStartPoint;
                }
            }
        }
    }
}
