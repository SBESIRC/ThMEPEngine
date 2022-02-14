using System.Linq;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThFlowSegmentExtractionVisitor
    {
        public List<string> LayerFilter { get; set; }
        public List<ThRawIfcFlowSegmentData> Results { get; set; }

        public ThFlowSegmentExtractionVisitor()
        {
            LayerFilter = new List<string>();
            Results = new List<ThRawIfcFlowSegmentData>();
        }

        public abstract void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj, Matrix3d matrix);

        public abstract void DoExtract(List<ThRawIfcFlowSegmentData> elements, Entity dbObj);

        public abstract void DoXClip(List<ThRawIfcFlowSegmentData> elements, BlockReference blockReference, Matrix3d matrix);

        public virtual bool IsFlowSegment(Entity entity)
        {
            return false;
        }

        public virtual bool CheckLayerValid(Entity curve)
        {
            return LayerFilter.Where(o => string.Compare(curve.Layer, o, true) == 0).Any();
        }

        public virtual bool IsFlowSegmentBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略动态块
            if (blockTableRecord.IsDynamicBlock)
            {
                return false;
            }

            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout || blockTableRecord.IsAnonymous)
            {
                return false;
            }

            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }

            return true;
        }

        public virtual bool IsFlowSegmentBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid;
        }
    }
}
