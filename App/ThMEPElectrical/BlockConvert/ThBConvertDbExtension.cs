using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertDbExtension
    {
        /// <summary>
        ///获取图纸中的块转换映射表
        /// </summary>
        /// <param name="database"></param>
        /// <returns></returns>
        public static List<ThBConvertRule> Rules(this Database database, ConvertMode mode)
        {
            var engine = new ThBConvertRuleEngine();
            return engine.Acquire(database, mode);
        }

        /// <summary>
        /// 获取转换图块的OBB
        /// </summary>
        /// <param name="br"></param>
        /// <param name="ecs2Wcs"></param>
        /// <returns></returns>
        public static Polyline GetBlockReferenceOBB(this BlockReference br, Matrix3d ecs2Wcs)
        {
            using (var acadDatabase = AcadDatabase.Use(br.Database))
            {
                // 业务需求: 去除在“DEFPOINTS”上的图元
                var blockTableRecord = acadDatabase.Blocks.Element(br.BlockTableRecord);
                var entities = blockTableRecord.GetEntities().Where(e => e.Layer != "DEFPOINTS");
                var rectangle = entities.ToCollection().GeometricExtents().ToRectangle();
                rectangle.TransformBy(ecs2Wcs);
                return rectangle;
            }
        }
    }
}