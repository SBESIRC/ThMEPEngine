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
    public class ThHalfPrimaryBeamLinkExtension : ThBeamLinkExtension
    {
        private List<ThIfcElement> UnDefinedBeams { get; set; }
        private List<ThBeamLink> PrimaryBeamLinks { get; set; }
        public List<ThBeamLink> HalfPrimaryBeamLinks { get; private set; }
        public ThHalfPrimaryBeamLinkExtension(List<ThIfcElement> undefinedBeams, List<ThBeamLink> primaryBeamLinks)
        {
            UnDefinedBeams = undefinedBeams;
            PrimaryBeamLinks = primaryBeamLinks;
            HalfPrimaryBeamLinks = new List<ThBeamLink>();
        }        
        public void CreateHalfPrimaryBeamLink()
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
                thBeamLink.End = QueryPortLinkElements(linkElements[linkElements.Count-1], backPt);
                if (thBeamLink.Start.Count == 0 && thBeamLink.End.Count > 0)
                {
                    // 末端连接竖向构件                    
                    thBeamLink.Start = QueryPortLinkPrimaryBeams(linkElements[0], prePt);
                }
                else if (thBeamLink.Start.Count > 0 && thBeamLink.End.Count == 0)
                {
                    // 起始端连接竖向构件
                    thBeamLink.End = QueryPortLinkPrimaryBeams(linkElements[linkElements.Count - 1], backPt);
                }
                else
                {
                    continue;
                }
                if (JudgeHalfPrimaryBeam(thBeamLink))
                {
                    thBeamLink.Beams = linkElements;
                    thBeamLink.Beams.ForEach(o => o.ComponentType = BeamComponentType.HalfPrimaryBeam);
                    HalfPrimaryBeamLinks.Add(thBeamLink);
                }
            }
        }
        private List<ThIfcElement> QueryPortLinkPrimaryBeams(ThIfcBeam thIfcBeam,Point3d portPt)
        {
            List<ThIfcBeam> thIfcBeams= QueryPortLinkBeams(thIfcBeam, portPt);
            thIfcBeams= thIfcBeams.Where(o => o.ComponentType == BeamComponentType.PrimaryBeam).ToList();
            if (thIfcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                thIfcBeams.Where(o =>
                {
                    if (o is ThIfcLineBeam otherLineBeam)
                    {
                        return !TwoBeamIsParallel(thIfcLineBeam, otherLineBeam);
                    }
                    else
                    {
                        return true;
                    }
                });
            }
            return thIfcBeams.Cast<ThIfcElement>().ToList();
        }
        private Point3d PreFindBeamLink(Point3d portPt, List<ThIfcBeam> beamLink)
        {
            //端点连接竖向构件则返回
            if(QueryPortLinkElements(beamLink[0],portPt).Count>0)
            {
                return portPt;
            }           
            List<ThIfcBeam> linkElements = FilterPortLinkBeams(beamLink[0], portPt);
            //过滤不在beamLink中的梁
            linkElements = linkElements.Where(m => !beamLink.Where(n => n.Uuid == m.Uuid).Any()).ToList();
            if (linkElements.Count == 0)
            {
                return portPt;
            }
            if (beamLink[0] is ThIfcLineBeam currentLineBeam)
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
            List<ThIfcBeam> linkElements = FilterPortLinkBeams(beamLink[beamLink.Count - 1], portPt);
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
        private List<ThIfcBeam> FilterPortLinkBeams(ThIfcBeam currentBeam, Point3d portPt)
        {
            //查找端点处连接的梁
            List<ThIfcBeam> linkElements = QueryPortLinkBeams(currentBeam, portPt);
            //端点处连接的梁中是否含有主梁
            List<ThIfcBeam> primaryBeams = linkElements.Where(m => PrimaryBeamLinks.Where(n => n.Beams.Where(k => k.Uuid == m.Uuid).Any()).Any()).ToList();
            //TODO 后续根据需要是否要对主梁进行方向筛选
            if (currentBeam is ThIfcLineBeam thIfcLineBeam)
            {
                primaryBeams=primaryBeams.Where(o =>
                {
                    if (o is ThIfcLineBeam otherLineBeam)
                    {
                        return !TwoBeamIsParallel(thIfcLineBeam, otherLineBeam);
                    }
                    else
                    {
                        return true;
                    }
                }).ToList();
            }
            if (primaryBeams.Count > 0)
            {
                return new List<ThIfcBeam>();
            }
            //从端点连接的梁中过滤只存在于UnDefinedBeams集合里的梁
            linkElements = linkElements.Where(m => UnDefinedBeams.Where(n => m.Uuid == n.Uuid).Any()).ToList();
            linkElements = linkElements.Where(o => o is ThIfcBeam thIfcBeam && thIfcBeam.ComponentType == BeamComponentType.Undefined).ToList();
            return linkElements;
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
            return links;
        }
    }
}
