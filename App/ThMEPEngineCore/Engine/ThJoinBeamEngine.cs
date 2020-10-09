using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;
using System;

namespace ThMEPEngineCore.Engine
{
    public class ThJoinBeamEngine
    {
        private ThBeamRecognitionEngine BeamEngine { get; set; }
        private ThSpatialIndexManager SpatialIndexManager { get; set; }

        public List<ThIfcBuildingElement> BeamElements { get; set; }

        private ThCADCoreNTSSpatialIndex SpatialIndex
        {
            get
            {
                return SpatialIndexManager.BeamSpatialIndex;
            }
        }

        public ThJoinBeamEngine(
            ThBeamRecognitionEngine thBeamRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            BeamEngine = thBeamRecognitionEngine;
            SpatialIndexManager = thSpatialIndexManager;
            BeamElements = thBeamRecognitionEngine.Elements;
        }

        public void Join()
        {
            throw new NotImplementedException();
        }
    }
}
