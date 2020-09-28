using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using ThMEPEngineCore.Model.Segment;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPEngineCore.BeamInfo.Business;
using System.Collections.Generic;

namespace ThMEPEngineCore.Engine
{
    public class ThMergeBeamEngine
    {
        private ThBeamRecognitionEngine BeamEngine { get; set; }
        private ThSpatialIndexManager SpatialIndexManager { get; set; }

        public List<ThIfcBuildingElement> BeamElements { get; set; }

        public ThMergeBeamEngine(
            ThBeamRecognitionEngine thBeamRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            BeamEngine = thBeamRecognitionEngine;
            SpatialIndexManager = thSpatialIndexManager;
            BeamElements = BeamEngine.Elements;

        }

        public void Merge()
        {
            //
        }
    }
}
