using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThFlowFittingExtractionVisitor
    {
        public List<string> LayerFilter { get; set; }
        public List<ThRawIfcFlowFittingData> Results { get; set; }

        public ThFlowFittingExtractionVisitor()
        {
            LayerFilter = new List<string>();
            Results = new List<ThRawIfcFlowFittingData>();
        }

        public abstract void DoExtract(List<ThRawIfcFlowFittingData> elements, Entity dbObj, Matrix3d matrix);

        public abstract void DoExtract(List<ThRawIfcFlowFittingData> elements, Entity dbObj);

        public abstract void DoXClip(List<ThRawIfcFlowFittingData> elements, BlockReference blockReference, Matrix3d matrix);

        public virtual bool CheckLayerValid(Entity entity)
        {
            return LayerFilter.Where(o => string.Compare(entity.Layer, o, true) == 0).Any();
        }

        public virtual bool IsFlowFitting(Entity entity)
        {
            return false;
        }

        public virtual bool IsFlowFittingBlock(BlockTableRecord blockTableRecord)
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

        public virtual bool IsFlowFittingBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid;
        }
    }
}
