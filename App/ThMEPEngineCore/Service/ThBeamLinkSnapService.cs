using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThBeamLinkSnapService
    {
        public ThBeamLink BeamLink { get; private set; }
        private const double extraExtendDistance = 5.0;
        public ThBeamLinkSnapService(ThBeamLink thBeamLink)
        {
            BeamLink = thBeamLink;
        }
        public static ThBeamLinkSnapService Snap(ThBeamLink thBeamLink)
        {
            ThBeamLinkSnapService thBeamLinkSnapService = new ThBeamLinkSnapService(thBeamLink);
            thBeamLinkSnapService.Snap();
            return thBeamLinkSnapService;
        }
        private void Snap()
        {
            for (int i = 0; i < BeamLink.Beams.Count; i++)
            {
                double startExtendDis = 0.0;
                double endExtendDis = 0.0;
                var currentBeam = BeamLink.Beams[i];
                if (i == 0)
                {
                    startExtendDis = SnapTo(currentBeam, currentBeam.StartPoint,
                        BeamLink.Start.Count > 0 ? BeamLink.Start[0] : null);
                    if (BeamLink.Beams.Count == 1)
                    {
                        endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint,
                        BeamLink.End.Count > 0 ? BeamLink.End[0] : null);
                    }
                    else
                    {
                        endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint, BeamLink.Beams[i + 1]);
                    }
                }
                else if (i == BeamLink.Beams.Count - 1)
                {
                    endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint,
                        BeamLink.End.Count > 0 ? BeamLink.End[0] : null);
                    startExtendDis = SnapTo(currentBeam, currentBeam.StartPoint, BeamLink.Beams[i - 1]);
                }
                else
                {
                    startExtendDis = SnapTo(currentBeam, currentBeam.StartPoint, BeamLink.Beams[i - 1]);
                    endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint, BeamLink.Beams[i + 1]);
                }
                if (startExtendDis > 0 || endExtendDis > 0)
                {
                    currentBeam.Outline = currentBeam.ExtendBoth(startExtendDis, endExtendDis);
                }
            }
        }
        private double SnapTo(ThIfcBeam currentBeam, Point3d portPt, ThIfcBuildingElement neighbor)
        {
            if (currentBeam is ThIfcLineBeam thIfcLineBeam)
            {
                return SnapTo(thIfcLineBeam, portPt, neighbor);
            }
            else if (currentBeam is ThIfcArcBeam thIfcArcBeam)
            {
                return SnapTo(thIfcArcBeam, portPt, neighbor);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private bool Intersects(ThIfcLineBeam lineBeam, ThIfcBuildingElement neighbor)
        {
            var rectangle = lineBeam.Outline as Polyline;
            return rectangle.RectIntersects(neighbor.Outline as Curve);
        }

        private double SnapTo(ThIfcLineBeam lineBeam, Point3d portPt, ThIfcBuildingElement neighbor)
        {
            double extendDis = 0.0;
            if (neighbor != null && !Intersects(lineBeam, neighbor))
            {
                Point3dCollection intersectPts = new Point3dCollection();
                Line centerLine = new Line(lineBeam.StartPoint, lineBeam.EndPoint);
                centerLine.IntersectWith(neighbor.Outline, Intersect.ExtendThis, intersectPts, IntPtr.Zero, IntPtr.Zero);
                if (intersectPts.Count > 0)
                {
                    var closestPt = intersectPts.Cast<Point3d>()
                        .OrderBy(o => portPt.DistanceTo(o)).First();
                    extendDis = portPt.DistanceTo(closestPt);
                    extendDis += extraExtendDistance;
                }
                else
                {
                    extendDis = 0.0;
                }
            }
            return extendDis;
        }
        private double SnapTo(ThIfcArcBeam arcBeam, Point3d portPt, ThIfcBuildingElement neighbor)
        {
            return 0.0;
        }
    }
}
