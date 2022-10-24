using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Algorithm;
using ThMEPTCH.CAD;
using System.Linq;
using ThCADExtension;

namespace ThMEPTCH.Engine
{
    public class ThTCHCableCarrierSegmentExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        public override bool IsFlowSegmentBlock(BlockTableRecord blockTableRecord)
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
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !xclip.Contains(o.Geometry as Curve));
            }
        }

        public override bool IsFlowSegment(Entity e)
        {
            return e.IsTCHCableCarrierSegment();
        }

        private List<ThRawIfcFlowSegmentData> HandleTCHCableCarrierSegment(Entity dbObj, Matrix3d matrix)
        {
            var results = new List<ThRawIfcFlowSegmentData>();
            if (IsFlowSegment(dbObj) && CheckLayerValid(dbObj))
            {
                if (dbObj is Curve curve)
                {  
                    results.Add(new ThRawIfcFlowSegmentData()
                    {
                        Geometry = curve.GetTCHCableCarrierSegmentOutLine(matrix)
                    });
                }
            }
            return results;
        }
    }
}
