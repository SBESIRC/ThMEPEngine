using System;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.EarthingGrid.Engine
{
    public class ThDownConductorWireExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            throw new NotImplementedException();
        }

        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }
    }
}
