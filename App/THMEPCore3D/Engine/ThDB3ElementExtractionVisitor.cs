using THMEPCore3D.Model;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace THMEPCore3D.Engine
{
    public abstract class ThDB3ElementExtractionVisitor
    {
        public HashSet<string> LayerFilter { get; set; }
        public List<ThDb3ElementRawData> Results { get; protected set; }

        public ThDB3ElementExtractionVisitor()
        {
            Results = new List<ThDb3ElementRawData>();
            LayerFilter = new HashSet<string>();
        }

        public abstract void DoExtract(List<ThDb3ElementRawData> elements, Entity dbObj, Matrix3d matrix);

        public abstract void DoXClip(List<ThDb3ElementRawData> elements, BlockReference blockReference, Matrix3d matrix);

        public virtual bool IsBuildElement(Entity entity)
        {
            return entity.ObjectId.IsValid;
        }
        public virtual bool CheckLayerValid(Entity curve)
        {
            return LayerFilter.Count ==0 ? true : LayerFilter.Contains(curve.Layer);
        }
        public virtual bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
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
    }
}
