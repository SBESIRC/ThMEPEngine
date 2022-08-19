using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Engine
{
    public abstract class ThBuildingElementExtractionVisitor
    {
        public List<string> LayerFilter { get; set; }
        public List<ThRawIfcBuildingElementData> Results { get; set; }

        public ThBuildingElementExtractionVisitor()
        {
            LayerFilter = new List<string>();
            Results = new List<ThRawIfcBuildingElementData>();
        }

        public abstract void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix);

        public abstract void DoXClip(List<ThRawIfcBuildingElementData> elements, BlockReference blockReference, Matrix3d matrix);

        public virtual bool IsBuildElement(Entity entity)
        {
            return entity.ObjectId.IsValid;
        }
        public virtual bool CheckLayerValid(Entity curve)
        {
            return LayerFilter.Where(o => string.Compare(curve.Layer, o, true) == 0).Any();
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

            // 有些块由于未知的原因成为不可炸的，这样会大大影响我们提取底层数据
            // 暂时让这种块通过
            //// 忽略不可“炸开”的块
            //if (!blockTableRecord.Explodable)
            //{
            //    return false;
            //}

            return true;
        }
        public virtual bool IsBuildElementBlockReference(BlockReference blockReference)
        {
            return false;
        }
        public virtual void DoExtract(List<ThRawIfcBuildingElementData> elements, Entity dbObj, Matrix3d matrix, int uid)
        {
            DoExtract(elements, dbObj, matrix);
        }
    }
}
