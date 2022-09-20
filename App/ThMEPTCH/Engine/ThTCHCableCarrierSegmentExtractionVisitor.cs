using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPTCH.CAD;

namespace ThMEPTCH.Engine
{
    public class ThTCHCableCarrierSegmentExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(HandleTCHCableCarrierSegment(dbObj, matrix));
        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            elements.AddRange(HandleTCHCableCarrierSegment(dbObj, Matrix3d.Identity));
        }

        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //
        }

        public override bool IsFlowSegment(Entity e)
        {
            return e.IsTCHCableCarrierSegment();
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        private List<ThRawIfcFlowSegmentData> HandleTCHCableCarrierSegment(Entity dbObj, Matrix3d matrix)
        {
            return new List<ThRawIfcFlowSegmentData>()
            {
                dbObj.Database.LoadCableCarrierSegmentFromDb(dbObj.ObjectId, matrix),
            };
        }
    }
}
