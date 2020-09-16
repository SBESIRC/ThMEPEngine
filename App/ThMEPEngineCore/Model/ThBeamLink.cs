using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Model
{
    public class ThBeamLink
    {
        public List<ThIfcBuildingElement> Start { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBuildingElement> End { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBeam> Beams { get; set; } = new List<ThIfcBeam>();
        public Tuple<Polyline,Point3d,Point3d> CreateExtendBeamOutline(double extendDis)
        {
            Polyline polyline=new Polyline();
            if(Beams.Count==0 || Beams.Where(o => o is ThIfcArcBeam).Any())
            {
                return Tuple.Create(polyline, Point3d.Origin, Point3d.Origin);
            }
            double maxWidth = Beams.Select(o => o.ActualWidth).OrderByDescending(o => o).FirstOrDefault();
            Point3d firstBeamSpt = Beams[0].StartPoint;
            Point3d firstBeamEpt = Beams[0].EndPoint;
            List<Point3d> ptList = new List<Point3d>();
            Beams.ForEach(o =>
            {
                ptList.Add(o.StartPoint.GetProjectPtOnLine(firstBeamSpt, firstBeamEpt));
                ptList.Add(o.EndPoint.GetProjectPtOnLine(firstBeamSpt, firstBeamEpt));
            });
            Vector3d vec = ptList[0].GetVectorTo(ptList[ptList.Count - 1]);
            Vector3d perpendVec = vec.GetPerpendicularVector();
            Point3d newSpt = ptList[0] - vec.GetNormal().MultiplyBy(extendDis);
            Point3d newEpt = ptList[ptList.Count - 1] + vec.GetNormal().MultiplyBy(extendDis);
            Point3d pt1 = newSpt - perpendVec.GetNormal().MultiplyBy(maxWidth/2.0);
            Point3d pt2 = newSpt + perpendVec.GetNormal().MultiplyBy(maxWidth / 2.0);
            Point3d pt3 = newEpt + perpendVec.GetNormal().MultiplyBy(maxWidth / 2.0);
            Point3d pt4 = newEpt - perpendVec.GetNormal().MultiplyBy(maxWidth / 2.0);
            
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return Tuple.Create(pts.CreatePolyline(), newSpt, newEpt);
        }
        public bool StartHasVerticalComponent=> Start.Where(o => o is ThIfcWall || o is ThIfcColumn).Any();
        public bool EndHasVerticalComponent=> End.Where(o => o is ThIfcWall || o is ThIfcColumn).Any();
    }
    public class ThSingleBeamLink
    {
        public List<ThIfcBuildingElement> StartVerComponents { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBuildingElement> EndVerComponents { get; set; } = new List<ThIfcBuildingElement>();
        public List<ThIfcBeam> StartBeams { get; set; } = new List<ThIfcBeam>();
        public List<ThIfcBeam> EndBeams { get; set; } = new List<ThIfcBeam>();
        public ThIfcBeam Beam { get; set; }

        public List<ThIfcBuildingElement> GetPortVerComponents(Point3d pt)
        {
            if (pt.DistanceTo(Beam.StartPoint) <= 1.0)
            {
                return StartVerComponents;
            }
            else if (pt.DistanceTo(Beam.EndPoint) <= 1.0)
            {
                return EndVerComponents;
            }
            else
            {
                return new List<ThIfcBuildingElement>();
            }
        }
        public List<ThIfcBeam> GetPortBeams(Point3d pt)
        {
            if (pt.DistanceTo(Beam.StartPoint) <= 1.0)
            {
                return StartBeams;
            }
            else if (pt.DistanceTo(Beam.EndPoint) <= 1.0)
            {
                return EndBeams;
            }
            else
            {
                return new List<ThIfcBeam>();
            }
        }
        public bool IsUnconnected()
        {
            if(StartVerComponents.Count==0 && EndVerComponents.Count==0 &&
                StartBeams.Count==0 && EndBeams.Count==0)
            {
                return true;
            }
            return false;
        }
        public void UpdateStartLink()
        {
            StartBeams = ThFilterPortLinkBeams.Filter(Beam, Beam.StartPoint, StartBeams);
            //bool startPtInComponent = StartVerComponents.Where(o => (o.Outline as Polyline).PointInPolylineEx(Beam.StartPoint, 1.0) == 1).Any();
            //if (!startPtInComponent)
            //{
            //    bool startHasLinkedBeam = StartBeams.Where(o => o.StartPoint.DistanceTo(Beam.StartPoint) <= 10.0 ||
            // o.EndPoint.DistanceTo(Beam.StartPoint) <= 10.0).Any();
            //    if (startHasLinkedBeam)
            //    {
            //        StartVerComponents = new List<ThIfcBuildingElement>();
            //    }
            //}
        }
        public void UpdateEndLink()
        {
            EndBeams = ThFilterPortLinkBeams.Filter(Beam, Beam.EndPoint, EndBeams);
            //bool endPtInComponent = EndVerComponents.Where(o => (o.Outline as Polyline).PointInPolylineEx(Beam.EndPoint, 1.0) == 1).Any();
            //if (!endPtInComponent)
            //{
            //    bool endHasLinkedBeam = EndBeams.Where(o => o.StartPoint.DistanceTo(Beam.EndPoint) <= 10.0 ||
            //                          o.EndPoint.DistanceTo(Beam.EndPoint) <= 10.0).Any();
            //    if (endHasLinkedBeam)
            //    {
            //        EndVerComponents = new List<ThIfcBuildingElement>();
            //    }
            //}
        }
    }
}
