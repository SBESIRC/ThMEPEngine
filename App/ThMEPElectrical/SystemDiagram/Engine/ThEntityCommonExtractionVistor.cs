using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Engine;

namespace ThMEPElectrical.SystemDiagram.Engine
{
    public abstract class ThEntityCommonExtractionVistor
    {
        public List<string> LayerFilter { get; set; }
        public List<ThEntityData> Results { get; set; }

        public ThEntityCommonExtractionVistor()
        {
            Results = new List<ThEntityData>();
            LayerFilter = new List<string>();
        }

        public abstract void DoExtract(List<ThEntityData> elements, Entity dbObj, Matrix3d matrix);

        public abstract void DoExtract(List<ThEntityData> elements, Entity dbObj);

        public abstract void DoXClip(List<ThEntityData> elements, BlockReference blockReference, Matrix3d matrix);

        public virtual bool IsSpatialElement(Entity entity)
        {
            return entity.ObjectId.IsValid;
        }
        public virtual bool CheckLayerValid(Entity curve)
        {
            return LayerFilter.Where(o => string.Compare(curve.Layer, o, true) == 0).Any();
        }
        public virtual bool IsSpatialElementBlock(BlockTableRecord blockTableRecord)
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
        public virtual bool IsSpatialElementBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid;
        }
    }
}
