using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;


namespace ThMEPElectrical.SystemDiagram.Extension
{
    public static class ThDBObbExtension
    {
        public static Point3d GetBlockReferenceOBBCenter(this Database database, BlockReference br)
        {
            return database.GetBlockReferenceOBB(br).GetCentroidPoint();
        }

        public static Point3d GetDBTextReferenceOBBCenter(this Database database, DBText text)
        {
            using (var dbSwitch = new ThDbWorkingDatabaseSwitch(database))
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var textObb = ThMEPEngineCore.CAD.ThGeometryTool.TextOBB(text);
                var rectangleCenter = textObb.GetCentroidPoint();
                return rectangleCenter;
            }
        }

        public static Polyline GetBlockReferenceOBB(this Database database, BlockReference br)
        {
            using (var dbSwitch = new ThDbWorkingDatabaseSwitch(database))
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                // 业务需求: 去除隐藏的图元信息
                var blockTableRecord = acadDatabase.Blocks.Element(br.BlockTableRecord);
                var entities = blockTableRecord.GetEntities().Where(e =>
                {
                    if (e is AttributeDefinition) return false;//业务：过滤属性定义
                    if (e is BlockReference) return false;//业务：过滤块中块
                    var layerTableRecord = acadDatabase.Layers.Element(e.Layer);
                    return !layerTableRecord.IsFrozen & !layerTableRecord.IsOff;
                });

                var rectangle = entities.ToCollection().GeometricExtents().ToRectangle();
                // 考虑到多段线不能使用非比例的缩放
                // 这里采用一个变通方法：
                // 将矩形柱转化成2d Solid，缩放后再转回多段线
                // 当实体存在时
                if (rectangle.Area > 1e-6)
                {
                    var solid = rectangle.ToSolid();
                    solid.TransformBy(br.BlockTransform);
                    return solid.ToPolyline();
                }

                // 特殊情况：块定义图元全在隐藏图层上
                // 退而求其次，用块引用的AABB来代替OBB
                // 可能是由于块的图元放在“0”图层上，块引用本身在可见图层上，但在图纸中“0”图层被隐藏了
                return br.GeometricExtents.ToRectangle();
            }
        }
    }
}
