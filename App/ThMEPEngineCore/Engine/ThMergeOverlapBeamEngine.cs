using System;
using ThCADCore.NTS;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Service;
using System.Collections.Generic;

namespace ThMEPEngineCore.Engine
{
    public class ThMergeOverlapBeamEngine
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

        public ThMergeOverlapBeamEngine(
            ThBeamRecognitionEngine thBeamRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            BeamEngine = thBeamRecognitionEngine;
            SpatialIndexManager = thSpatialIndexManager;
            BeamElements = thBeamRecognitionEngine.Elements;
        }

        public void MergeOverlap()
        {
            throw new NotImplementedException();
        }
    }
}
