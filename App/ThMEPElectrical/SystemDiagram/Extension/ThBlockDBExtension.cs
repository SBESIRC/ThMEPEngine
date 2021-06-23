using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADCore.NTS;

namespace ThMEPElectrical.SystemDiagram.Extension
{
    public static class ThBlockDBExtension
    {
        public static DBPoint GetBlockReferenceOBBCenter(this BlockReference br, Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                HostApplicationServices.WorkingDatabase = database;
                // 业务需求: 去除隐藏的图元信息              
                var blockTableRecord = acadDatabase.Blocks.Element(br.BlockTableRecord);
                var entities = blockTableRecord.GetEntities().Where(e =>
                {
                    if (e is AttributeDefinition) return false;
                    var layerTableRecord = acadDatabase.Layers.Element(e.Layer);
                    return !layerTableRecord.IsFrozen & !layerTableRecord.IsOff;
                });
                var rectangle = entities.ToCollection().GeometricExtents().ToRectangle();
                var rectangleCenter = rectangle.GetCentroidPoint();
                rectangleCenter = rectangleCenter.TransformBy(br.BlockTransform);
                return new DBPoint(rectangleCenter);
            }
        }
    }
}
