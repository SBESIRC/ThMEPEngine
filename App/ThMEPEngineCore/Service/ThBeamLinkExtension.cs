using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Service
{
    public class ThBeamLinkExtension
    {
        private double enlargeTimes = 1.0;
        private double expandDis = 0.0;
        public ThIfcBeam Beam { get; private set; }
        public ThBeamLink BeamLink { get; private set; }
        public ThBeamLinkExtension(ThIfcBeam beam)
        {
            Beam = beam;
            if (beam.Width > 120)
            {
                expandDis = enlargeTimes * beam.Width;
            }
            else
            {
                expandDis = 200.0;
            }
        }
        public void CreateSinglePrimaryBeamLink()
        {
            BeamLink = new ThBeamLink
            {
                Start = QueryPortLinkElements(Beam.StartPoint),
                End = QueryPortLinkElements(Beam.EndPoint)
            };
            if (JudgePrimaryBeam())
            {
                BeamLink.Beams.Add(Beam);
            }
        }
        public bool JudgePrimaryBeam()
        {
            var startLink =BeamLink.Start.Where(o=>o.GetType()==typeof(ThIfcColumn) || o.GetType() == typeof(ThIfcWall));
            var endLink = BeamLink.End.Where(o => o.GetType() == typeof(ThIfcColumn) || o.GetType() == typeof(ThIfcWall));
            if(startLink.Any() && endLink.Any())
            {
                return true;
            }
            return false;
        }
        private List<ThIfcElement> QueryPortLinkElements(Point3d portPt)
        {
            List<ThIfcElement> links = new List<ThIfcElement>();
            DBObjectCollection linkObjs = new DBObjectCollection();
            Polyline portEnvelop = CreatePortEnvelop(portPt);
            linkObjs = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectFence(portEnvelop);
            if(linkObjs.Count > 0)
            {
                foreach(DBObject dbObj in linkObjs)
                {
                    links.Add(ThIfcColumn.CreateColumnEntity(dbObj as Curve));
                }
            }
            else
            {
                //linkObjs = ThSpatialIndexManager.Instance.WallSpatialIndex.SelectFence(portEnvelop);
                //foreach (DBObject dbObj in linkObjs)
                //{
                //    links.Add(ThIfcWall.CreateWallEntity(dbObj as Curve));
                //}
            }
            return links;
        }
        private Polyline CreatePortEnvelop(Point3d portPt)
        {
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            Point3d pt1 = portPt + new Vector3d(expandDis / 2.0, expandDis / 2.0, 0.0);
            Point3d pt2 = portPt + new Vector3d(-expandDis / 2.0, expandDis / 2.0, 0.0);
            Point3d pt3 = portPt + new Vector3d(-expandDis / 2.0, -expandDis / 2.0, 0.0);
            Point3d pt4 = portPt + new Vector3d(expandDis / 2.0, -expandDis / 2.0, 0.0);
            polyline.AddVertexAt(0, new Point2d(pt1.X, pt1.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(pt2.X, pt2.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(pt3.X, pt3.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(pt4.X, pt4.Y), 0.0, 0.0, 0.0);
            return polyline;
        }
    }
}
