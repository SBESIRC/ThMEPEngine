using System;
using Linq2Acad;
using System.IO;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using DotNetARX;
using ThCADExtension;
using ThMEPWSS.Service;

namespace ThWSS.Bussiness
{
    public static class InsertSprayService
    {
        /// <summary>
        /// 插入喷淋图块
        /// </summary>
        /// <param name="insertPts"></param>
        public static void InsertSprayBlock(List<Point3d> insertPts, SprayType type)
        {
            var sprayType = ThWSSUIService.Instance.Parameter.layoutType == ThMEPWSS.Model.LayoutType.UpSpray ? ThWSSCommon.SprayUpBlockName : ThWSSCommon.SprayDownBlockName;
            using (var db = AcadDatabase.Active())
            {
                LayerTools.AddLayer(db.Database, ThWSSCommon.SprayLayerName);
                db.Database.UnFrozenLayer(ThWSSCommon.SprayLayerName);
                db.Database.UnLockLayer(ThWSSCommon.SprayLayerName);
                db.Database.UnOffLayer(ThWSSCommon.SprayLayerName);
                var filePath = Path.Combine(ThCADCommon.SupportPath(), ThWSSCommon.SprayDwgName);
                db.Database.ImportBlocksFromDwg(filePath);
                foreach (var insertPoint in insertPts)
                {
                    var blockId = db.ModelSpace.ObjectId.InsertBlockReference(
                        ThWSSCommon.SprayLayerName,
                        sprayType,
                        insertPoint,
                        new Scale3d(1, 1, 1),
                        0);
                    //blockId.SetDynBlockValue(ThWSSCommon.BLOCK_MAP_ATTRIBUTES_BLOCK_VISIBILITY, SprayVisibilityPropValue(type));
                }
            }
        }

        /// <summary>
        /// 喷淋块属性值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string SprayVisibilityPropValue(SprayType type)
        {
            switch (type)
            {
                case SprayType.SPRAYUP:
                    return "上喷";
                case SprayType.SPRAYDOWN:
                    return "下喷";
                default:
                    throw new NotSupportedException();
            }
        }
    }

    // 喷淋放置的一些参数
    public enum SprayType
    {
        SPRAYUP = 0,
        SPRAYDOWN = 1,
    }
}
