using System;
using System.Linq;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public class ThJoinBeamEngine : ThBeamPreprocessEngine
    {
        private ThBeamConnectRecogitionEngine BeamConnectRecognitionEngine { get; set; }
        private ThBeamLinkExtension ThBeamLinkEx;
        private List<ThIfcBeam> RemoveBeams { get; set; }
        private List<ThIfcBeam> AddBeams { get; set; }
        public ThJoinBeamEngine(ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            BeamConnectRecognitionEngine = thBeamConnectRecogitionEngine;
            ThBeamLinkEx = new ThBeamLinkExtension
            {
                ConnectionEngine = BeamConnectRecognitionEngine
            };
            RemoveBeams = new List<ThIfcBeam>();
            AddBeams = new List<ThIfcBeam>();
        }
        public void Join()
        {
            BeamConnectRecognitionEngine.BeamEngine.Elements.ForEach(o => Join(o as ThIfcBeam));
            RemoveBeams.ForEach(o => BeamConnectRecognitionEngine.BeamEngine.Elements.Remove(o));
            AddBeams.ForEach(o => BeamConnectRecognitionEngine.BeamEngine.Elements.Add(o));
        }
        private void Join(ThIfcBeam thifcBeam)
        {
            if (thifcBeam is ThIfcLineBeam thIfcLineBeam)
            {
                Join(thIfcLineBeam);
            }
            else if (thifcBeam is ThIfcArcBeam thIfcArcBeam)
            {
                Join(thIfcArcBeam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private void Join(ThIfcLineBeam thIfcLineBeam)
        {
            var startComponents = GetPortLinkObjs(thIfcLineBeam, thIfcLineBeam.StartPoint);
            var startBeams = new List<ThIfcLineBeam>();
            if (startComponents.Count == 0)
            {
                startBeams = FilterCollinearBeams(thIfcLineBeam, thIfcLineBeam.StartPoint);
                startBeams = startBeams
                    .Where(o => IsOutsideOfBeam(thIfcLineBeam, o))
                    .Where(o => SameWidth(o, thIfcLineBeam) && SameHeight(o, thIfcLineBeam))
                    .OrderBy(o => thIfcLineBeam.StartPoint.DistanceTo(o.EndPoint)).ToList();
            }
            var endComponents = GetPortLinkObjs(thIfcLineBeam, thIfcLineBeam.EndPoint);
            var endBeams = new List<ThIfcLineBeam>();
            if (endComponents.Count == 0)
            {
                endBeams = FilterCollinearBeams(thIfcLineBeam, thIfcLineBeam.EndPoint);
                endBeams = endBeams
                    .Where(o => IsOutsideOfBeam(thIfcLineBeam, o))
                    .Where(o => SameWidth(o, thIfcLineBeam) && SameHeight(o, thIfcLineBeam))
                    .OrderBy(o => thIfcLineBeam.EndPoint.DistanceTo(o.StartPoint)).ToList();
            }
            if (startBeams.Count > 0 || endBeams.Count > 0)
            {
                var startBeam = startBeams.Count > 0 ? startBeams[0] : null;
                var endBeam = endBeams.Count > 0 ? endBeams[0] : null;
                var newBeam = Join(thIfcLineBeam, startBeam, endBeam);
                RemoveBeams.Add(thIfcLineBeam);
                AddBeams.Add(newBeam);
            }
        }
        private ThIfcLineBeam Join(ThIfcLineBeam current, ThIfcLineBeam startLink, ThIfcLineBeam endLink)
        {
            var pts = new List<Point3d>();
            pts.Add(current.StartPoint);
            pts.Add(current.EndPoint);
            if (startLink != null)
            {
                pts.Add(startLink.StartPoint);
                pts.Add(startLink.EndPoint);
            }
            if (endLink != null)
            {
                pts.Add(endLink.StartPoint);
                pts.Add(endLink.EndPoint);
            }
            double startExtendDis = 0.0;
            double endExtendDis = 0.0;
            var maxPts = pts.GetCollinearMaxPts();
            if(maxPts.Item1.GetVectorTo(maxPts.Item2).DotProduct(current.StartPoint.GetVectorTo(current.EndPoint))>0)
            {
                startExtendDis = maxPts.Item1.DistanceTo(current.StartPoint)+5.0;
                endExtendDis = maxPts.Item2.DistanceTo(current.EndPoint) + 5.0;
            }
            else
            {
                startExtendDis = maxPts.Item2.DistanceTo(current.StartPoint) + 5.0;
                endExtendDis = maxPts.Item1.DistanceTo(current.EndPoint) + 5.0;
            }
            return ThIfcLineBeam.Create(current, startExtendDis, endExtendDis);
        }

        private List<ThIfcLineBeam> FilterCollinearBeams(ThIfcLineBeam thIfcLineBeam, Point3d portPt)
        {
            return ThBeamLinkEx.QueryPortLinkBeams(thIfcLineBeam, portPt, 0.5,
                ThMEPEngineCoreCommon.BeamIntervalMaximumTolerance)
                .Where(o => o is ThIfcLineBeam)
                .Where(o => o.Uuid != thIfcLineBeam.Uuid)
                .Where(o => IsCollinear(thIfcLineBeam, o as ThIfcLineBeam))
                .Cast<ThIfcLineBeam>().ToList();
        }
        private bool IsOutsideOfBeam(ThIfcLineBeam baseBeam, ThIfcLineBeam second)
        {
            //两根梁是共线的
            return !ThGeometryTool.IsPointOnLine(baseBeam.StartPoint, baseBeam.EndPoint, second.StartPoint) &&
                !ThGeometryTool.IsPointOnLine(baseBeam.StartPoint, baseBeam.EndPoint, second.StartPoint);
        }
        private void Join(ThIfcArcBeam thIfcAcBeam)
        {
            throw new NotSupportedException();
        }
        private List<ThIfcBuildingElement> GetPortLinkObjs(ThIfcBeam thIfcBeam, Point3d portPt)
        {
            List<ThIfcBuildingElement> results = new List<ThIfcBuildingElement>();
            var linkBeams = ThBeamLinkEx.QueryPortLinkBeams(thIfcBeam,
                portPt, 0.5, ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance);
            if (linkBeams.Count == 0)
            {
                var linkComponents = ThBeamLinkEx.QueryPortLinkElements(thIfcBeam,
                portPt, ThMEPEngineCoreCommon.BeamComponentConnectionTolerance);
                linkComponents.ForEach(o => results.Add(o));
            }
            return results;
        }
    }
}
