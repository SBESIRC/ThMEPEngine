using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBlockReferenceDataExtension
    {
        public static Point3d GetCentroidPoint(this ThBlockReferenceData data)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var entities = data.VisibleEntities()
                    .Cast<ObjectId>()
                    .Select(e => acadDatabase.Element<Entity>(e))
                    .Where(e => e.Layer != "DEFPOINTS")
                    .Where(e => e is Curve)
                    .ToCollection();
                if (entities.Count != 0)
                {
                    return entities.GeometricExtents().CenterPoint().TransformBy(data.MCS2WCS);
                }
                var blockTableRecord = acadDatabase.Blocks.Element(data.EffectiveName);
                return blockTableRecord.GeometricExtents().CenterPoint().TransformBy(data.MCS2WCS);
            }
        }
    }
}
