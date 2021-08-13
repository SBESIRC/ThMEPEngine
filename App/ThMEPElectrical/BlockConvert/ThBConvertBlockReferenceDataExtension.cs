using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Engine;
using Autodesk.AutoCAD.Geometry;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertBlockReferenceDataExtension
    {
        public static Point3d GetCentroidPoint(this ThBlockReferenceData data)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(data.Database))
            {
                var entities = new DBObjectCollection();
                var blkref = acadDatabase.Element<BlockReference>(data.ObjId);
                blkref.ExplodeWithVisible(entities);
                var name = data.EffectiveName;
                if (name.Contains("风机") ||
                         name.Contains("组合式空调器") ||
                         name.Contains("暖通其他设备标注") ||
                         name.Contains("风冷热泵") ||
                         name.Contains("冷水机组") ||
                         name.Contains("冷却塔"))
                {
                    entities = entities.Cast<Entity>()
                        .Where(e => e.Layer == "0" || e.Layer.Contains("H-EQUP"))
                        .Where(e => !e.Layer.Contains("DIMS"))
                        .Where(e => e is Curve || e is BlockReference)
                        .ToCollection();
                }
                else if (name.Contains("防火阀"))
                {
                    entities = entities.Cast<Entity>()
                        .Where(e => e.Layer != "DEFPOINTS")
                        .Where(e => e is Circle || e is BlockReference)
                        .ToCollection();
                }
                else
                {
                    entities = entities.Cast<Entity>()
                        .Where(e => e.Layer != "DEFPOINTS")
                        .Where(e => e is Curve || e is BlockReference)
                        .ToCollection();
                }
                if (entities.Count == 0)
                {
                    return Point3d.Origin;
                }
                return entities.GeometricExtents().CenterPoint();
            }
        }
    }
}
