using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.EarthingGrid.Engine
{
    public class ThDownConductorWireExtractionEngine : ThFlowSegmentExtractionEngine
    {
        public override void Extract(Database database)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromEditor(Point3dCollection frame)
        {
            throw new NotImplementedException();
        }

        public override void ExtractFromMS(Database database)
        {
            throw new NotImplementedException();
        }
    }
}
