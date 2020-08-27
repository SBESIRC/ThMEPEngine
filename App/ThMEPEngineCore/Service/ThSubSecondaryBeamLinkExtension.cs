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
    public class ThSubSecondaryBeamLinkExtension : ThSecondaryBeamLinkExtension
    {
        public ThSubSecondaryBeamLinkExtension(List<ThIfcBuildingElement> undefinedBeams, List<ThBeamLink> primaryBeamLinks,
            List<ThBeamLink> halfPrimaryBeamLinks, List<ThBeamLink> overhangingPrimaryBeamLinks, List<ThBeamLink> secondaryBeamLinks)
            :base(undefinedBeams, primaryBeamLinks, halfPrimaryBeamLinks, overhangingPrimaryBeamLinks)
        {
            SecondaryBeamLinks = secondaryBeamLinks;
        }   
        /// <summary>
        /// 次次梁
        /// </summary>
        public void CreateSubSecondaryBeamLink()
        {
            for (int i = 0; i < UnDefinedBeams.Count; i++)
            {
                ThIfcBeam currentBeam = UnDefinedBeams[i] as ThIfcBeam;
                if (currentBeam.ComponentType == BeamComponentType.SecondaryBeam)
                {
                    continue;
                }
                ThBeamLink thBeamLink = new ThBeamLink();
                List<ThIfcBeam> linkElements = new List<ThIfcBeam>() { currentBeam };
                Point3d prePt = PreFindBeamLink(currentBeam.StartPoint, linkElements);
                Point3d backPt = BackFindBeamLink(currentBeam.EndPoint, linkElements);
                thBeamLink.Start = QueryPortLinkElements(linkElements[0], prePt);
                thBeamLink.End = QueryPortLinkElements(linkElements[linkElements.Count - 1], backPt);
                if (thBeamLink.Start.Count == 0 && thBeamLink.End.Count == 0)
                {
                    thBeamLink.Start.AddRange(QueryPortLinkPrimaryBeams(PrimaryBeamLinks, linkElements[0], prePt));
                    thBeamLink.Start.AddRange(QueryPortLinkHalfPrimaryBeams(HalfPrimaryBeamLinks, linkElements[0], prePt));
                    thBeamLink.Start.AddRange(QueryPortLinkOverhangingPrimaryBeams(OverhangingPrimaryBeamLinks, linkElements[0], prePt));
                    thBeamLink.Start.AddRange(QueryPortLinkSecondaryBeams(SecondaryBeamLinks, linkElements[0], prePt));

                    thBeamLink.End.AddRange(QueryPortLinkPrimaryBeams(PrimaryBeamLinks, linkElements[linkElements.Count - 1], backPt));
                    thBeamLink.End.AddRange(QueryPortLinkHalfPrimaryBeams(HalfPrimaryBeamLinks, linkElements[linkElements.Count - 1], backPt));
                    thBeamLink.End.AddRange(QueryPortLinkOverhangingPrimaryBeams(OverhangingPrimaryBeamLinks, linkElements[linkElements.Count - 1], backPt));
                    thBeamLink.End.AddRange(QueryPortLinkSecondaryBeams(SecondaryBeamLinks, linkElements[linkElements.Count - 1], backPt));
                }
                else
                {
                    continue;
                }
                if (JudgeSubSecondaryPrimaryBeam(thBeamLink))
                {
                    thBeamLink.Beams = linkElements;
                    thBeamLink.Beams.ForEach(o => o.ComponentType = BeamComponentType.SecondaryBeam);
                    SecondaryBeamLinks.Add(thBeamLink);
                }
            }
        }
    }
}
