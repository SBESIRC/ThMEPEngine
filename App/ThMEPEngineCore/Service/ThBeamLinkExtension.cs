using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Service
{
    public class ThBeamLinkExtension
    {
        public ThColumnRecognitionEngine ColumnEngine { get; set; }
        public ThBeamRecognitionEngine BeamEngine { get; set; }
        public ThShearWallRecognitionEngine ShearWallEngine { get; set; }
        public ThBeamLinkExtension()
        {
        }
        public ThBeamLink CreateSinglePrimaryBeamLink(ThIfcBeam Beam)
        {
            ThBeamLink beamLink = new ThBeamLink();
            double distance = GenerateExpandDistance(Beam);
            beamLink.Start = QueryPortLinkElements(Beam.StartPoint, distance);
            beamLink.End = QueryPortLinkElements(Beam.EndPoint, distance);
            if (JudgePrimaryBeam(beamLink))
            {
                Beam.ComponentType = BeamComponentType.PrimaryBeam;
                beamLink.Beams.Add(Beam);
            }
            return beamLink;
        }
        public bool JudgePrimaryBeam(ThBeamLink thBeamLink)
        {
            var startLink = thBeamLink.Start.Where(o=>o.GetType()==typeof(ThIfcColumn) || o.GetType() == typeof(ThIfcWall));
            var endLink = thBeamLink.End.Where(o => o.GetType() == typeof(ThIfcColumn) || o.GetType() == typeof(ThIfcWall));
            if(startLink.Any() && endLink.Any())
            {
                return true;
            }
            return false;
        }

        protected List<ThIfcElement> QueryPortLinkElements(Point3d portPt, double distance)
        {
            List<ThIfcElement> links = new List<ThIfcElement>();
            DBObjectCollection linkObjs = new DBObjectCollection();
            Polyline portEnvelop = CreatePortEnvelop(portPt, distance);
            linkObjs = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectFence(portEnvelop);
            if (linkObjs.Count > 0)
            {
                foreach (DBObject dbObj in linkObjs)
                {
                    links.Add(ColumnEngine.FilterByOutline(dbObj));
                }
            }
            else
            {
                linkObjs = ThSpatialIndexManager.Instance.WallSpatialIndex.SelectFence(portEnvelop);
                foreach (DBObject dbObj in linkObjs)
                {
                    links.Add(ShearWallEngine.FilterByOutline(dbObj));
                }
            }
            portEnvelop.Dispose();
            return links;
        }
        protected double GenerateExpandDistance(ThIfcBeam beam)
        {
            if (beam.Width > 120)
            {
                return 500.0; //enlargeTimes * beam.Width;
            }
            else
            {
                return 500.0;
            }
        }
        protected Polyline CreatePortEnvelop(Point3d portPt, double distance)
        {
            Point3d pt1 = portPt + new Vector3d(distance, distance, 0.0);
            Point3d pt2 = portPt + new Vector3d(-distance, distance, 0.0);
            Point3d pt3 = portPt + new Vector3d(-distance, -distance, 0.0);
            Point3d pt4 = portPt + new Vector3d(distance, -distance, 0.0);
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return CreatePolyline(pts);
        }
        protected Polyline CreatePortEnvelop(Vector3d dir,Point3d portPt,double width, double distance)
        {
            Vector3d perpendicularVector = dir.GetPerpendicularVector();
            Point3d pt1 = portPt + perpendicularVector.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt4 = portPt - perpendicularVector.GetNormal().MultiplyBy(width / 2.0);
            Point3d pt2 = pt1 + dir.GetNormal().MultiplyBy(distance);
            Point3d pt3 = pt4 + dir.GetNormal().MultiplyBy(distance);
            Point3dCollection pts = new Point3dCollection() { pt1, pt2, pt3, pt4 };
            return CreatePolyline(pts);
        }
        protected Polyline CreatePolyline(Point3dCollection pts)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            for(int i=0;i<pts.Count;i++)
            {
                polyline.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0.0, 0.0, 0.0);
            }
            return polyline;
        }
        protected double GetPolylineWidth(Polyline polyline,Point3d pt)
        {
            List<LineSegment3d> segments = new List<LineSegment3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                LineSegment3d lineSegment = polyline.GetLineSegmentAt(i);
                if (lineSegment != null)
                {
                    segments.Add(lineSegment);
                }
            }
            return segments.OrderBy(o => o.MidPoint.DistanceTo(pt)).FirstOrDefault().Length;
        }
    }
}
