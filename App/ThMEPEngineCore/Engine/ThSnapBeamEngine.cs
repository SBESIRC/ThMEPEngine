using System;
using System.Linq;
using System.Collections.Generic;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.BeamInfo.Business;

namespace ThMEPEngineCore.Engine
{
    public class ThSnapBeamEngine : ThBuildingElementPreprocessEngine
    {
        private ThBeamConnectRecogitionEngine BeamConnectRecogitionEngine { get; set; }
        private ThBeamLinkExtension ThBeamLinkEx;
        public ThSnapBeamEngine(ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            BeamConnectRecogitionEngine = thBeamConnectRecogitionEngine;
            ThBeamLinkEx = new ThBeamLinkExtension
            {
                ConnectionEngine = BeamConnectRecogitionEngine
            };
        }

        public void Snap()
        {
            BeamConnectRecogitionEngine.BeamEngine.Elements.ForEach(o => Snap(o as ThIfcBeam));
        }
        private void Snap(ThIfcBeam thifcBeam)
        {
            if(thifcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                Snap(thIfcLineBeam);
            }
            else if(thifcBeam is ThIfcArcBeam thIfcArcBeam)
            {
                Snap(thIfcArcBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void Snap(ThIfcLineBeam thIfcLineBeam)
        {            
            ThBeamLink thBeamLink = new ThBeamLink();
            thBeamLink.Beams.Add(thIfcLineBeam);
            thBeamLink.Start = GetPortLinkObjs(thIfcLineBeam, thIfcLineBeam.StartPoint);
            thBeamLink.End= GetPortLinkObjs(thIfcLineBeam, thIfcLineBeam.EndPoint);
            ThBeamLinkSnapService.Snap(thBeamLink);
        }
        private void Snap(ThIfcArcBeam thIfcArcBeam)
        {
            throw new NotSupportedException();
        }
        private List<ThIfcBuildingElement> GetPortLinkObjs(ThIfcBeam thIfcBeam,Point3d portPt)
        {
            List<ThIfcBuildingElement> results = new List<ThIfcBuildingElement>();
            var linkBeams = GetPortLinkBeams(thIfcBeam, portPt);
            if (linkBeams.Count == 0)
            {
                var linkComponents = ThBeamLinkEx.QueryPortLinkElements(thIfcBeam,
                portPt, ThMEPEngineCoreCommon.BeamComponentConnectionTolerance);
                linkComponents.ForEach(o => results.Add(o));
            }
            return results;
        }
        private List<ThIfcBeam> GetPortLinkBeams(ThIfcBeam thIfcBeam, Point3d portPt)
        {
            if(thIfcBeam is ThIfcLineBeam lineBeam)
            {
                return GetPortLinkBeams(lineBeam, portPt);
            }
            else if(thIfcBeam is ThIfcArcBeam arcBeam)
            {
                return GetPortLinkBeams(arcBeam, portPt);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private List<ThIfcBeam> GetPortLinkBeams(ThIfcLineBeam thIfcLineBeam, Point3d portPt)
        {
            var linkBeams = ThBeamLinkEx.QueryPortLinkBeams(thIfcLineBeam,
                   portPt, 0.5, ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance);
            return linkBeams.Where(o=>
            {
                if(o is ThIfcLineBeam lineBeam) 
                {
                    return ThStructureBeamUtils.IsLooseCollinear(thIfcLineBeam, lineBeam);
                }
                else
                {
                    return ThStructureBeamUtils.IsLooseCollinear(thIfcLineBeam, portPt, o as ThIfcArcBeam);
                }
            }).ToList();
        }

        private List<ThIfcBeam> GetPortLinkBeams(ThIfcArcBeam thIfcArcBeam, Point3d portPt)
        {
            throw new NotSupportedException();
        }   
    }
}
