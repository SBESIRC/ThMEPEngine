using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.BeamInfo.Business;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.Service
{
    public class ThVerticalComponentBeamLinkExtension : ThBeamLinkExtension
    {
        public List<ThBeamLink> BeamLinks { get; private set; }
        public ThVerticalComponentBeamLinkExtension()
        {
            BeamLinks = new List<ThBeamLink>();
        }
        private List<ThIfcElement> UnDefinedBeams=new List<ThIfcElement>();
        public void CreateMultipleBeamLink(List<ThIfcElement> undefinedBeams)
        {
            UnDefinedBeams = undefinedBeams;
            for (int i = 0; i < undefinedBeams.Count; i++)
            {
                ThIfcBeam currentBeam = undefinedBeams[i] as ThIfcBeam;
                if (currentBeam.ComponentType == BeamComponentType.PrimaryBeam)
                {
                    continue;
                }
                ThBeamLink thBeamLink = new ThBeamLink();
                List<ThIfcBeam> linkElements = new List<ThIfcBeam>() { currentBeam };
                Point3d prePt = PreFindBeamLink(currentBeam.StartPoint, linkElements);
                Point3d backPt = BackFindBeamLink(currentBeam.EndPoint, linkElements);
                double distance = GenerateExpandDistance(currentBeam);
                thBeamLink.Start = QueryPortLinkElements(prePt, distance);
                thBeamLink.End = QueryPortLinkElements(backPt, distance);
                if (JudgePrimaryBeam(thBeamLink))
                {
                    thBeamLink.Beams = linkElements;
                    thBeamLink.Beams.ForEach(o => o.ComponentType = BeamComponentType.PrimaryBeam);
                    BeamLinks.Add(thBeamLink);
                }
            }
        }
        private Point3d PreFindBeamLink(Point3d portPt, List<ThIfcBeam> beamLink)
        {
            double distance = GenerateExpandDistance(beamLink[0]);
            if(QueryPortLinkElements(portPt, distance).Count>0)
            {
                return portPt;
            }
            List<ThIfcBeam> linkElements = QueryPortLinkBeams(beamLink[0],portPt);
            linkElements = linkElements.Where(m => !beamLink.Where(n => n.Uuid == m.Uuid).Any()).ToList();
            if (linkElements.Count==0)
            {
                return portPt;
            }
            if(beamLink[0] is ThIfcLineBeam currentLineBeam)
            {
                linkElements = linkElements.Where(o =>
                {
                    if (o is ThIfcLineBeam otherLineBeam)
                    {
                        return TwoBeamIsParallel(currentLineBeam, otherLineBeam);
                    }
                    else if (o is ThIfcArcBeam otherArcBeam)
                    {
                        return true;
                    }
                    return false;
                }).ToList();
            }
            if(linkElements.Count==1)
            {
                if(beamLink.Where(o=>o.Uuid== linkElements[0].Uuid).Any())
                {
                    return portPt;
                }
                beamLink.Insert(0, linkElements[0]);
                ThIfcBeam findBeam = linkElements[0] as ThIfcBeam;
                if(findBeam.StartPoint.DistanceTo(portPt)> findBeam.EndPoint.DistanceTo(portPt))
                {
                    return  PreFindBeamLink(findBeam.StartPoint, beamLink);
                }
                else
                {
                    return  PreFindBeamLink(findBeam.EndPoint, beamLink);
                }
            }
            return portPt; 
        }
        private Point3d BackFindBeamLink(Point3d portPt, List<ThIfcBeam> beamLink)
        {
            double distance = GenerateExpandDistance(beamLink[beamLink.Count-1]);
            if (QueryPortLinkElements(portPt, distance).Count > 0)
            {
                return portPt;
            }
            List<ThIfcBeam> linkElements = QueryPortLinkBeams(beamLink[beamLink.Count-1], portPt);
            linkElements = linkElements.Where(m => !beamLink.Where(n => n.Uuid == m.Uuid).Any()).ToList();
            if (linkElements.Count == 0)
            {
                return portPt;
            }
            if (beamLink[beamLink.Count-1] is ThIfcLineBeam currentLineBeam)
            {
                linkElements = linkElements.Where(o =>
                {
                    if (o is ThIfcLineBeam otherLineBeam)
                    {
                        return TwoBeamIsParallel(currentLineBeam, otherLineBeam);
                    }
                    else if (o is ThIfcArcBeam otherArcBeam)
                    {
                        return true;
                    }
                    return false;
                }).ToList();
            }
            if (linkElements.Count == 1)
            {
                if (beamLink.Where(o => o.Uuid == linkElements[0].Uuid).Any())
                {
                    return portPt;
                }
                beamLink.Add(linkElements[0]);
                ThIfcBeam findBeam = linkElements[0] as ThIfcBeam;
                if (findBeam.StartPoint.DistanceTo(portPt) > findBeam.EndPoint.DistanceTo(portPt))
                {
                    return BackFindBeamLink(findBeam.StartPoint, beamLink);
                }
                else
                {
                    return BackFindBeamLink(findBeam.EndPoint, beamLink);
                }
            }
            return portPt;
        }
        private List<ThIfcBeam> QueryPortLinkBeams(ThIfcBeam thIfcBeam, Point3d portPt)
        {
            List<ThIfcBeam> links = new List<ThIfcBeam>();
            DBObjectCollection linkObjs = new DBObjectCollection();
            Polyline portEnvelop = null;
            if (thIfcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                portEnvelop = GetLineBeamPortEnvelop(thIfcLineBeam, portPt);
            }
            else
            {
                //portEnvelop = CreatePortEnvelop(portPt);
            }
            linkObjs = ThSpatialIndexManager.Instance.BeamSpatialIndex.SelectFence(portEnvelop);
            if (linkObjs.Count > 0)
            {
                foreach (DBObject dbObj in linkObjs)
                {
                    links.Add(BeamEngine.FilterByOutline(dbObj) as ThIfcBeam);
                }
            }
            return links.Where(m => UnDefinedBeams.Where(n => m.Uuid == n.Uuid).Any()).ToList();
        }
        private Polyline GetLineBeamPortEnvelop(ThIfcLineBeam thIfcLineBeam,Point3d portPt)
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
        private bool TwoBeamIsParallel(ThIfcLineBeam firstBeam ,ThIfcLineBeam secondBeam)
        {
            return firstBeam.Direction.IsParallelToEx(secondBeam.Direction);
        }
        private bool TwoBeamIsCollinear(ThIfcLineBeam firstBeam, ThIfcLineBeam secondBeam)
        {

            return firstBeam.Direction.IsCodirectionalTo(secondBeam.Direction) ||
                   firstBeam.Direction.IsCodirectionalTo(secondBeam.Direction.Negate());
        }
    }
}
