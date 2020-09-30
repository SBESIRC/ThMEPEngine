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
    public abstract class ThBeamSplitter
    {
        public List<ThIfcBeam> SplitBeams { get; protected set; }
        protected List<ThSegment> Segments { get; set; }
        protected ThBeamSplitter(List<ThSegment> segments)
        {
            SplitBeams = new List<ThIfcBeam>();
            Segments = segments;
        }
        public abstract void Split();       
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
                Width = thIfcLineBeam.Width,
                Height = thIfcLineBeam.Height,
                ComponentType = thIfcLineBeam.ComponentType,
                Outline = pts.CreatePolyline()
            };
            return newLineBeam;
        }        
        public static Polyline CreateExtendOutline(ThIfcBuildingElement thIfcBuildingElement, double tolerance)
        {
            if (thIfcBuildingElement is ThIfcBeam thIfcBeam)
            {
                return thIfcBeam.Extend(0.0, tolerance);
            }            
            else
            {
                return thIfcBuildingElement.Outline.Clone() as Polyline;
            }
        }
    }
}
