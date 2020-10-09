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
        private ThBeamConnectRecogitionEngine BeamConnectRecogitionEngine { get; set; }
        public List<ThIfcBuildingElement> BeamElements { get; set; }

        public ThMergeBeamEngine(ThBeamConnectRecogitionEngine thBeamConnectRecogitionEngine)
        {
            BeamConnectRecogitionEngine = thBeamConnectRecogitionEngine;
            BeamElements = thBeamConnectRecogitionEngine.BeamEngine.Elements;
        }

        public void Merge()
        {
            //
        }
    }
}
