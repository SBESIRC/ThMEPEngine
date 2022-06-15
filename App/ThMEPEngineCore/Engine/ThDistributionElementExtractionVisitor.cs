using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThDistributionElementExtractionVisitor
    {
        public HashSet<string> LayerFilter { get; set; }
        public List<ThRawIfcDistributionElementData> Results { get; set; }

        public ThDistributionElementExtractionVisitor()
        {
            LayerFilter = new HashSet<string>();
            Results = new List<ThRawIfcDistributionElementData>();
        }

        public abstract void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix);
        public abstract void DoXClip(List<ThRawIfcDistributionElementData> elements, BlockReference blockReference, Matrix3d matrix);
        public virtual bool IsDistributionElement(Entity e)
        {
            return false;
        }
        public virtual bool CheckLayerValid(Entity e)
        {
            return LayerFilter.Contains(e.Layer);
        }
        public virtual bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
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
        public virtual bool IsBuildElementBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid;
        }
        public virtual bool IsBuildElementInnerBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid;
        }
    }
}
