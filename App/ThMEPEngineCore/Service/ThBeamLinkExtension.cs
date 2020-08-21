using System;
using System.Linq;
using System.Collections.Generic;
using ThCADCore.NTS;

using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.BeamInfo.Business;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;


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
            beamLink.Start = QueryPortLinkElements(Beam, Beam.StartPoint);
            beamLink.End = QueryPortLinkElements(Beam, Beam.EndPoint);
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
        public bool JudgeHalfPrimaryBeam(ThBeamLink thBeamLink)
        {
            if (JudgePrimaryBeam(thBeamLink))
            {
                return false;
            }
            var startLinkComponent = thBeamLink.Start.Where(o => o.GetType() == typeof(ThIfcColumn) || o.GetType() == typeof(ThIfcWall));
            if(startLinkComponent.Any())
            {
                var endLinkPrimaryBeam = thBeamLink.End.Where(o => o is ThIfcBeam thIfcBeam && thIfcBeam.ComponentType==BeamComponentType.PrimaryBeam);
                if(endLinkPrimaryBeam.Any())
                {
                    return true;
                }
            }
            var endLinkComponent = thBeamLink.End.Where(o => o is ThIfcColumn || o is ThIfcWall);
            if (endLinkComponent.Any())
            {
                var startLinkPrimaryBeam = thBeamLink.Start.Where(o => o is ThIfcBeam thIfcBeam && thIfcBeam.ComponentType == BeamComponentType.PrimaryBeam);
                if (startLinkPrimaryBeam.Any())
                {
                    return true;
                }
            }
            return false;
        }
        public bool JudgeOverhangingPrimaryBeam(ThBeamLink thBeamLink)
        {
            if (JudgePrimaryBeam(thBeamLink))
            {
                return false;
            }
            var startLinkComponent = thBeamLink.Start.Where(o => o.GetType() == typeof(ThIfcColumn) || o.GetType() == typeof(ThIfcWall));
            if (startLinkComponent.Any())
            {
                if (thBeamLink.End.Count==0)
                {
                    return true;
                }
            }
            var endLinkComponent = thBeamLink.End.Where(o => o is ThIfcColumn || o is ThIfcWall);
            if (endLinkComponent.Any())
            {
                if (thBeamLink.Start.Count==0)
                {
                    return true;
                }
            }
            return false;
        }
        public bool JudgeSecondaryPrimaryBeam(ThBeamLink thBeamLink)
        {
            if (JudgePrimaryBeam(thBeamLink))
            {
                return false;
            }
            var startLinkComponent = thBeamLink.Start.Where(o => o is ThIfcColumn || o is ThIfcWall);
            var endLinkComponent = thBeamLink.End.Where(o => o is ThIfcColumn || o is ThIfcWall);
            if (startLinkComponent.Any() || endLinkComponent.Any())
            {
                return false;
            }
            var startLinkBeam = thBeamLink.Start.Where(o => o is ThIfcBeam);
            var endLinkBeam = thBeamLink.End.Where(o => o is ThIfcBeam);
            return startLinkBeam.Any() && endLinkBeam.Any();
        }

        protected List<ThIfcBuildingElement> QueryPortLinkElements(ThIfcBeam thIfcBeam, Point3d portPt)
        {
            List<ThIfcBuildingElement> links = new List<ThIfcBuildingElement>();
            DBObjectCollection linkObjs = new DBObjectCollection();
            if (thIfcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                Polyline portEnvelop = GetLineBeamPortEnvelop(thIfcLineBeam, portPt);

                // 先判断是否搭接在柱上
                linkObjs = ThSpatialIndexManager.Instance.ColumnSpatialIndex.SelectFence(portEnvelop);
                if (linkObjs.Count > 0)
                {
                    // 确保梁的延伸和柱是“重叠(Overlap)”的
                    var overlapObjs = linkObjs.Cast<Polyline>().Where(o => o.Overlaps(portEnvelop));
                    foreach (DBObject dbObj in overlapObjs)
                    {
                        links.Add(ColumnEngine.FilterByOutline(dbObj));
                    }
                }
                if (links.Count > 0)
                {
                    return links;
                }

                // 再判断是否搭接在剪力墙上
                linkObjs = ThSpatialIndexManager.Instance.WallSpatialIndex.SelectFence(portEnvelop);
                if (linkObjs.Count > 0)
                {
                    // 确保梁的延伸和剪力墙是“重叠(Overlap)”的
                    var overlapObjs = linkObjs.Cast<Polyline>().Where(o => o.Overlaps(portEnvelop));
                    foreach (DBObject dbObj in overlapObjs)
                    {
                        links.Add(ShearWallEngine.FilterByOutline(dbObj));
                    }
                }

                return links;
            }
            else
            {
                throw new NotSupportedException();
            }
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
        protected bool TwoBeamIsParallel(ThIfcLineBeam firstBeam, ThIfcLineBeam secondBeam)
        {
            return firstBeam.Direction.IsParallelToEx(secondBeam.Direction);
        }
        protected bool TwoBeamIsCollinear(ThIfcLineBeam firstBeam, ThIfcLineBeam secondBeam)
        {

            return firstBeam.Direction.IsCodirectionalTo(secondBeam.Direction) ||
                   firstBeam.Direction.IsCodirectionalTo(secondBeam.Direction.Negate());
        }
        protected List<ThIfcBeam> QueryPortLinkBeams(ThIfcBeam thIfcBeam, Point3d portPt)
        {
            List<ThIfcBeam> links = new List<ThIfcBeam>();
            DBObjectCollection linkObjs = new DBObjectCollection();
            Polyline portEnvelop = null;
            if (thIfcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                portEnvelop = GetLineBeamPortEnvelop(thIfcLineBeam, portPt);
            }
            else if (thIfcBeam is ThIfcArcBeam thIfcArcBeam)
            {
                portEnvelop = GetArcBeamPortEnvelop(thIfcArcBeam, portPt);
            }
            linkObjs = ThSpatialIndexManager.Instance.BeamSpatialIndex.SelectFence(portEnvelop);
            if (linkObjs.Count > 0)
            {
                foreach (DBObject dbObj in linkObjs)
                {
                    links.Add(BeamEngine.FilterByOutline(dbObj) as ThIfcBeam);
                }
            }
            return links;
        }
        protected Polyline GetLineBeamPortEnvelop(ThIfcLineBeam thIfcLineBeam, Point3d portPt)
        {
            double beamWidth = GetPolylineWidth(thIfcLineBeam.Outline as Polyline, portPt);
            double distance = GenerateExpandDistance(thIfcLineBeam);
            if (portPt.DistanceTo(thIfcLineBeam.StartPoint) < portPt.DistanceTo(thIfcLineBeam.EndPoint))
            {
                return CreatePortEnvelop(thIfcLineBeam.Direction.Negate(), portPt, beamWidth, distance);
            }
            else
            {
                return CreatePortEnvelop(thIfcLineBeam.Direction, portPt, beamWidth, distance);
            }
        }
        protected Polyline GetArcBeamPortEnvelop(ThIfcArcBeam thIfcArcBeam, Point3d portPt)
        {
            double beamWidth = GetPolylineWidth(thIfcArcBeam.Outline as Polyline, portPt);
            double distance = GenerateExpandDistance(thIfcArcBeam);
            if (portPt.DistanceTo(thIfcArcBeam.StartPoint) < portPt.DistanceTo(thIfcArcBeam.EndPoint))
            {
                return CreatePortEnvelop(thIfcArcBeam.StartTangent.Negate(), portPt, beamWidth, distance);
            }
            else
            {
                return CreatePortEnvelop(thIfcArcBeam.EndTangent.Negate(), portPt, beamWidth, distance);
            }
        }
    }
}
