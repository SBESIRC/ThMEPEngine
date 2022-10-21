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
        public override bool IsFlowFittingBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间
            if (blockTableRecord.IsLayout)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

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
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        public override bool IsFlowFitting(Entity e)
        {
            return e.IsTCHCableCarrierFitting();
        }

        private List<ThRawIfcFlowFittingData> HandleTCHCableCarrierFitting(Entity dbObj, Matrix3d matrix)
        {
            var results = new List<ThRawIfcFlowFittingData>();
            if(IsFlowFitting(dbObj) && CheckLayerValid(dbObj))
            {
                if(dbObj is Curve curve)
                {
                    results.Add(new ThRawIfcFlowFittingData()
                    {
                        Geometry = curve.GetTCHCableCarrierFittingOutline(matrix)
                    });
                }
            }
            return results;
        }
    }
}
