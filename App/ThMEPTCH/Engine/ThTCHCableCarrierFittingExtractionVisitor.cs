using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPTCH.CAD;

namespace ThMEPTCH.Engine
{
    public class ThTCHCableCarrierFittingExtractionVisitor : ThFlowFittingExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcFlowFittingData> elements, Entity dbObj, Matrix3d matrix)
        {
            elements.AddRange(HandleTCHCableCarrierFitting(dbObj, matrix));
        }

        public override void DoExtract(List<ThRawIfcFlowFittingData> elements, Entity dbObj)
        {
            elements.AddRange(HandleTCHCableCarrierFitting(dbObj, Matrix3d.Identity));
        }

        public override void DoXClip(List<ThRawIfcFlowFittingData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            //
        }

        public override bool IsFlowFitting(Entity e)
        {
            return e.IsTCHCableCarrierFitting();
        }

        public override bool CheckLayerValid(Entity entity)
        {
            return true;
        }

        private List<ThRawIfcFlowFittingData> HandleTCHCableCarrierFitting(Entity dbObj, Matrix3d matrix)
        {
            return new List<ThRawIfcFlowFittingData>()
            {
                dbObj.Database.LoadTCHCableCarrierFitting(dbObj.ObjectId, matrix),
            };
        }
    }
}
