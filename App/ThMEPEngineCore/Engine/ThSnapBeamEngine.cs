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
    public class ThSnapBeamEngine
    {
        private ThColumnRecognitionEngine ColumnEngine { get; set; }
        private ThShearWallRecognitionEngine ShearWallEngine { get; set; }
        private ThBeamRecognitionEngine BeamEngine { get; set; }
        private ThSpatialIndexManager SpatialIndexManager { get; set; }
        public List<ThIfcBuildingElement> BeamElements { get; set; }

        public ThSnapBeamEngine(
            ThBeamRecognitionEngine thBeamRecognitionEngine,
            ThColumnRecognitionEngine thColumnRecognitionEngine,
            ThShearWallRecognitionEngine thShearWallRecognitionEngine,
            ThSpatialIndexManager thSpatialIndexManager)
        {
            ColumnEngine = thColumnRecognitionEngine;
            ShearWallEngine = thShearWallRecognitionEngine;
            BeamEngine = thBeamRecognitionEngine;
            SpatialIndexManager = thSpatialIndexManager;
            BeamElements = BeamEngine.Elements;
        }

        public void Snap()
        {
            //
        }
    }
}
