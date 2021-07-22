using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
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
                entities = entities.Cast<Entity>()
                    .Where(e => e.Layer != "DEFPOINTS")
                    .Where(e => e is Curve || e is BlockReference)
                    .ToCollection();
                if (entities.Count == 0)
                {
                    return Point3d.Origin;
                }
                return entities.GeometricExtents().CenterPoint();
            }
        }
    }
}
