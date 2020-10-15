using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPEngineCore.Service
{
    public abstract class ThDbExtension
    {
        public Database HostDb { get; set; }
        public List<string> LayerFilter { get; set; }
        protected ThDbExtension(Database db)
        {
            HostDb = db;
            LayerFilter = new List<string>();
        }
        public abstract void BuildElementTexts();
        public abstract void BuildElementCurves();
        protected bool CheckLayerValid(Entity curve)
        {
            return LayerFilter.Where(o => string.Compare(curve.Layer, o, true) == 0).Any();
        }
        protected bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
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
        protected bool IsBuildElementBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid;
        }
        protected virtual bool IsBuildElement(Entity entity)
        {
            return entity.ObjectId.IsValid;
        }
    }
}
