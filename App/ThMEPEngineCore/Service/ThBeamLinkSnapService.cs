using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Extension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

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
                    startExtendDis = SnapTo(currentBeam, currentBeam.StartPoint, BeamLink.Start);
                    if (BeamLink.Beams.Count == 1)
                    {
                        endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint,BeamLink.End);
                    }
                    else
                    {
                        endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint, 
                            new List<ThIfcBuildingElement> { BeamLink.Beams[i + 1] });
                    }
                }
                else if (i == BeamLink.Beams.Count - 1)
                {
                    endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint,BeamLink.End);
                    startExtendDis = SnapTo(currentBeam, currentBeam.StartPoint,
                        new List<ThIfcBuildingElement> { BeamLink.Beams[i - 1] });
                }
                else
                {
                    startExtendDis = SnapTo(currentBeam, currentBeam.StartPoint,
                        new List<ThIfcBuildingElement> { BeamLink.Beams[i - 1] });
                    endExtendDis = SnapTo(currentBeam, currentBeam.EndPoint,
                        new List<ThIfcBuildingElement> { BeamLink.Beams[i + 1] });
                }
                if (startExtendDis > 0 || endExtendDis > 0)
                {
                    currentBeam.ExtendBoth(startExtendDis, endExtendDis);
                }
            }
        }
        private double SnapTo(ThIfcBeam currentBeam, Point3d portPt, List<ThIfcBuildingElement> neighbors)
        {
            if (currentBeam is ThIfcLineBeam thIfcLineBeam)
            {
                return SnapTo(thIfcLineBeam, portPt, neighbors);
            }
            else if (currentBeam is ThIfcArcBeam thIfcArcBeam)
            {
                return SnapTo(thIfcArcBeam, portPt, neighbors);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private bool Intersects(ThIfcLineBeam lineBeam, ThIfcBuildingElement neighbor)
        {
            var rectangle = lineBeam.Outline as Polyline;
            return rectangle.Intersects(neighbor.Outline as Curve);
        }

        private double SnapTo(ThIfcLineBeam lineBeam, Point3d portPt, List<ThIfcBuildingElement> neighbors)
        {
            double extendDis = 0.0;
            var neighbor = FindClosestNeighbor(lineBeam, portPt, neighbors);
            if (neighbor != null)
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
        private double SnapTo(ThIfcArcBeam arcBeam, Point3d portPt, List<ThIfcBuildingElement> neighbors)
        {
            return 0.0;
        }
        private ThIfcBuildingElement FindClosestNeighbor(ThIfcBeam thIfcBeam, Point3d portPt, List<ThIfcBuildingElement> neighbors)
        {
            if (thIfcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                return FindClosestNeighbor(thIfcLineBeam, portPt, neighbors);
            }
            else if(thIfcBeam is ThIfcArcBeam thIfcArcBeam)
            {
                return FindClosestNeighbor(thIfcArcBeam, portPt, neighbors);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private ThIfcBuildingElement FindClosestNeighbor(ThIfcLineBeam thIfcLineBeam, Point3d portPt, List<ThIfcBuildingElement> neighbors)
        {
           if(neighbors.Where(o => Intersects(thIfcLineBeam, o)).Any())
            {
                return null;
            }
           else
            {
                Line centerLine = new Line(thIfcLineBeam.StartPoint,thIfcLineBeam.EndPoint);
                List<Tuple<ThIfcBuildingElement, Point3d>> intersects = new List<Tuple<ThIfcBuildingElement, Point3d>>();
                neighbors.ForEach(o =>
                {
                    Point3dCollection intersectPts = new Point3dCollection();
                    centerLine.IntersectWith(o.Outline, Intersect.ExtendThis, intersectPts, IntPtr.Zero, IntPtr.Zero);
                    if (intersectPts.Count > 0)
                    {
                        var closestPt = intersectPts.Cast<Point3d>()
                            .OrderBy(m => portPt.DistanceTo(m)).First();
                        intersects.Add(Tuple.Create(o, closestPt));
                    }
                });
                return intersects.Count > 0 ? intersects.OrderBy(o => portPt.DistanceTo(o.Item2)).First().Item1 : null;
            }
        }
        private ThIfcBuildingElement FindClosestNeighbor(ThIfcArcBeam thIfcArcBeam, Point3d portPt, List<ThIfcBuildingElement> neighbors)
        {
            return null;
        }
    }
}
