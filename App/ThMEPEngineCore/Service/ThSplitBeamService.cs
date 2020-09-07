using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model.Segment;

namespace ThMEPEngineCore.Service
{
    public abstract class ThSplitBeamService
    {
        public List<ThIfcBeam> SplitBeams { get; private set; }
        protected List<ThSegment> Segments { get; set; }
        protected ThSplitBeamService(List<ThSegment> segments)
        {
            SplitBeams = new List<ThIfcBeam>();
            Segments = segments;
        }
        public abstract void Split();        
        protected Point3dCollection IntersectWithEx(Entity firstEntity, Entity secondEntity)
        {
            Point3dCollection pts = new Point3dCollection();
            Plane zeroPlane = new Plane(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis);
            firstEntity.IntersectWith(secondEntity, Intersect.OnBothOperands, zeroPlane, pts, IntPtr.Zero, IntPtr.Zero);
            zeroPlane.Dispose();
            return pts;
        }
        protected bool CheckTwoLineUnIntersect(Point3d firstSpt, Point3d firstEpt, Point3d secondSpt, Point3d secondEpt)
        {
            double firstLength = firstSpt.DistanceTo(firstEpt);
            double secondLength = secondSpt.DistanceTo(secondEpt);
            return firstSpt.DistanceTo(secondEpt) > (firstLength + secondLength);
        }
        protected ThIfcLineBeam CreateLineBeam(ThIfcLineBeam thIfcLineBeam, Point3d startPt, Point3d endPt)
        {
            Vector3d direction = startPt.GetVectorTo(endPt);
            Vector3d perpendDir = direction.GetPerpendicularVector();
            Point3d pt1 = startPt - perpendDir.GetNormal().MultiplyBy(thIfcLineBeam.ActualWidth / 2.0);
            Point3d pt2 = startPt + perpendDir.GetNormal().MultiplyBy(thIfcLineBeam.ActualWidth / 2.0);
            Point3d pt3 = pt2 + direction.GetNormal().MultiplyBy(startPt.DistanceTo(endPt));
            Point3d pt4 = pt1 + direction.GetNormal().MultiplyBy(startPt.DistanceTo(endPt));
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            ThIfcLineBeam newLineBeam = new ThIfcLineBeam()
            {
                Uuid = Guid.NewGuid().ToString(),
                StartPoint = startPt,
                EndPoint = endPt,
                Direction = direction,
                Width = thIfcLineBeam.Width,
                Height = thIfcLineBeam.Height,
                ComponentType = thIfcLineBeam.ComponentType,
                Outline = pts.CreatePolyline()
            };
            return newLineBeam;
        }        
        public static Polyline CreateExtendOutline(ThIfcLineBeam thIfcLineBeam, double tolerance)
        {
            Vector3d direction = thIfcLineBeam.StartPoint.GetVectorTo(thIfcLineBeam.EndPoint);
            Vector3d perpendDir = direction.GetPerpendicularVector();
            double width = thIfcLineBeam.ActualWidth + tolerance * 2;
            Point3d pt1 = thIfcLineBeam.StartPoint - perpendDir.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt2 = thIfcLineBeam.StartPoint + perpendDir.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt3 = pt2 + direction.GetNormal().MultiplyBy(thIfcLineBeam.Length);
            Point3d pt4 = pt1 + direction.GetNormal().MultiplyBy(thIfcLineBeam.Length);
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return pts.CreatePolyline();
        }

        public static Polyline CreateExtendOutline(ThIfcArcBeam thIfcArcBeam, double tolerance)
        {
            return thIfcArcBeam.Outline.Clone() as Polyline;
        }
        public static Polyline CreateExtendOutline(ThIfcBuildingElement thIfcBuildingElement, double tolerance=50.0)
        {
            if (thIfcBuildingElement is ThIfcLineBeam thIfcLineBeam)
            {
                return CreateExtendOutline(thIfcLineBeam, tolerance);
            }
            else if (thIfcBuildingElement is ThIfcArcBeam thIfcArcBeam)
            {
                return CreateExtendOutline(thIfcArcBeam, tolerance);
            }
            else
            {
                return thIfcBuildingElement.Outline.Clone() as Polyline;
            }
        }
    }
}
