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
        private List<ThIfcBuildingElement> UnDefinedBeams = new List<ThIfcBuildingElement>();
        public ThVerticalComponentBeamLinkExtension(List<ThIfcBuildingElement> undefinedBeams)
        {
            BeamLinks = new List<ThBeamLink>();
            UnDefinedBeams = undefinedBeams;
        }
        public void CreatePrimaryBeamLink()
        {
            for (int i = 0; i < UnDefinedBeams.Count; i++)
            {
                ThIfcBeam currentBeam = UnDefinedBeams[i] as ThIfcBeam;
                if (currentBeam.ComponentType == BeamComponentType.PrimaryBeam)
                {
                    continue;
                }
                ThBeamLink thBeamLink = new ThBeamLink();
                List<ThIfcBeam> linkElements = new List<ThIfcBeam>() { currentBeam };
                Point3d prePt = PreFindBeamLink(currentBeam.StartPoint, linkElements);
                Point3d backPt = BackFindBeamLink(currentBeam.EndPoint, linkElements);
                thBeamLink.Start = QueryPortLinkElements(linkElements[0], prePt);
                thBeamLink.End = QueryPortLinkElements(linkElements[linkElements.Count - 1], backPt);
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
            //端点连接竖向构件则返回
            if (QueryPortLinkElements(beamLink[0],portPt).Count>0)
            {
                return portPt;
            }
            //端点连接的梁
            List<ThIfcBeam> linkElements = QueryPortLinkBeams(beamLink[0],portPt);
            //过滤不存在于beamLink中的梁
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
                    return PreFindBeamLink(findBeam.StartPoint, beamLink);
                }
                else
                {
                    return PreFindBeamLink(findBeam.EndPoint, beamLink);
                }
            }
            return portPt; 
        }
        private Point3d BackFindBeamLink(Point3d portPt, List<ThIfcBeam> beamLink)
        {
            //端点连接竖向构件则返回
            if (QueryPortLinkElements(beamLink[beamLink.Count - 1],portPt).Count > 0)
            {
                return portPt;
            }
            //端点连接的梁
            List<ThIfcBeam> linkElements = QueryPortLinkBeams(beamLink[beamLink.Count-1], portPt);
            //过滤不存在于beamLink中的梁
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
        protected List<ThIfcBeam> QueryPortLinkBeams(ThIfcBeam thIfcBeam, Point3d portPt)
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
            return links.Where(m => UnDefinedBeams.Where(n => m.Uuid == n.Uuid && m.ComponentType!=BeamComponentType.PrimaryBeam).Any()).ToList();
        }
    }
}
