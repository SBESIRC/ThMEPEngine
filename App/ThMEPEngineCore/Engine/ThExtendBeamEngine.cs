using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service;

namespace ThMEPEngineCore.Engine
{
    public class ThExtendBeamEngine : ThBuildingElementPreprocessEngine
    {
        private List<ThIfcBuildingElement> BeamElements { get; set; }

        private ThBeamLinkExtension LinkExtension { get; set; }

        private ThBeamConnectRecogitionEngine BeamConnectRecogitionEngine { get; set; }

        private ThCADCoreNTSSpatialIndex SpatialIndex
        {
            get
            {
                return BeamConnectRecogitionEngine.SpatialIndexManager.BeamSpatialIndex;
            }
        }

        private ThBeamRecognitionEngine BeamEngine
        {
            get
            {
                return BeamConnectRecogitionEngine.BeamEngine;
            }
        }
        private IEnumerable<ThIfcLineBeam> LineBeams
        {
            get
            {
                return BeamEngine.Elements.Where(o => o is ThIfcLineBeam).Cast<ThIfcLineBeam>();
            }
        }

        public ThExtendBeamEngine(ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            BeamConnectRecogitionEngine = thBeamConnectRecogitionEngine;
            LinkExtension = new ThBeamLinkExtension()
            {
                ConnectionEngine = BeamConnectRecogitionEngine,
            };
        }

        public void Extend()
        {
            BeamElements = new List<ThIfcBuildingElement>();
            foreach (var thIfcBeam in LineBeams)
            {
                bool bExtendStart = LinkExtension.QueryPortLinkBeams(
                    thIfcBeam,
                    thIfcBeam.StartPoint,
                    1.0,
                    ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance)
                    .Cast<ThIfcLineBeam>()
                    .Where(o => IsParallel(thIfcBeam, o))
                    .Where(o => !IsOverlap(thIfcBeam, o))
                    .Where(o => IsCollinear(thIfcBeam, o))
                    .Any();

                bool bExtendEnd = LinkExtension.QueryPortLinkBeams(
                    thIfcBeam,
                    thIfcBeam.EndPoint,
                    1.0,
                    ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance)
                    .Cast<ThIfcLineBeam>()
                    .Where(o => IsParallel(thIfcBeam, o))
                    .Where(o => !IsOverlap(thIfcBeam, o))
                    .Where(o => IsCollinear(thIfcBeam, o))
                    .Any();

                if (bExtendStart || bExtendEnd)
                {
                    double endDistance = bExtendEnd ? ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance : 0.0;
                    double startDistance = bExtendStart ? ThMEPEngineCoreCommon.BeamIntervalMinimumTolerance : 0.0;
                    BeamElements.Add(ThIfcLineBeam.Create(thIfcBeam.ExtendBoth(startDistance, endDistance)));
                }
                else
                {
                    BeamElements.Add(thIfcBeam);
                }
            }
            BeamEngine.Elements = BeamElements;
        }
    }
}
