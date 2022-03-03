using System;
using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPEngineCore.Engine;

namespace TianHua.Electrical.PDS.Engine
{
    public class ThCabletraySegmentExtractionVisitor : ThFlowSegmentExtractionVisitor
    {
        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        public override void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj)
        {
            if (dbObj is Curve curve)
            {
                elements.AddRange(Handle(curve));
            }
        }

        public override void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix)
        {
            throw new NotImplementedException();
        }

        private List<ThRawIfcFlowSegmentData> Handle(Curve curve)
        {
            var clone = curve.Clone() as Curve;
            return new List<ThRawIfcFlowSegmentData>
            {
                new ThRawIfcFlowSegmentData()
                {
                    Data = clone,
                }
            };
        }
    }
}
