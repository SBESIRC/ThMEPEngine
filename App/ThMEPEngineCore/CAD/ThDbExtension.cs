using AcHelper;
using Linq2Acad;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public static class ThDbExtension
    {
        public static IEnumerable<Curve> BuildElementCurves(this Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var curves = acadDatabase.ModelSpace
                    .OfType<Curve>()
                    .Where(o => o.IsBuildElementCurve())
                    .Select(o => o.GetTransformedCopy(Matrix3d.Identity) as Curve).ToList();

                var blockReferences = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o => o.IsBuildElementBlockReference());
                foreach(var blockReference in blockReferences)
                {
                    var mcs2wcs = blockReference.BlockTransform.PreMultiplyBy(Matrix3d.Identity);
                    curves.AddRange(database.BuildElementCurves(blockReference, mcs2wcs));
                }

                return curves;
            }
        }

        public static IEnumerable<Curve> BuildElementCurves(this Database database, BlockReference blockReference, Matrix3d matrix)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var blockTableRecord = acadDatabase.Blocks.Element(blockReference.Name);
                if (blockTableRecord.IsBuildElementBlock())
                {
                    var curves = blockTableRecord
                        .OfType<Curve>()
                        .Where(o => o.IsBuildElementCurve())
                        .Select(o => o.GetTransformedCopy(matrix) as Curve).ToList();

                    var nestedblockReferences = blockTableRecord
                        .OfType<BlockReference>()
                        .Where(o => o.IsBuildElementBlockReference());
                    foreach(var nestedblockReference in nestedblockReferences)
                    {
                        var mcs2wcs = nestedblockReference.BlockTransform.PreMultiplyBy(matrix);
                        curves.AddRange(database.BuildElementCurves(nestedblockReference, mcs2wcs));
                    }

                    return curves;
                }
                return new List<Curve>();
            }
        }

        /// <summary>
        /// 判断是否是天华三维协同图元
        /// 天华三维协同将BIM信息填充在图元的“超链接”属性
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsBuildElementCurve(this Entity entity)
        {
            return entity.Hyperlinks.Count > 0;
        }

        /// <summary>
        /// 判断是否是天华三维协同创建的块引用
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static bool IsBuildElementBlockReference(this BlockReference blockReference)
        {
            return true;
        }

        /// <summary>
        /// 判断是否是天华三维协同创建的块定义
        /// </summary>
        /// <param name="blockTableRecord"></param>
        /// <returns></returns>
        public static bool IsBuildElementBlock(this BlockTableRecord blockTableRecord)
        {
            // 暂时不支持动态块，外部参照，覆盖
            if (blockTableRecord.IsDynamicBlock ||
                blockTableRecord.IsFromExternalReference ||
                blockTableRecord.IsFromOverlayReference)
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

        public static bool IsVisible(this Entity entity,AcadDatabase acadDb)
        {
            if (entity.LayerId != ObjectId.Null && entity.LayerId.IsValid)
            {
                var layerTableRecord = acadDb.Element<LayerTableRecord>(entity.LayerId);
                return !(layerTableRecord.IsOff || layerTableRecord.IsFrozen);
            }
            return entity.IsVisible();
        }
        public static bool IsVisible(this Entity entity)
        {
            return entity.Visible;
        }
    }
}