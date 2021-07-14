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
                    .Where(e => e is Curve);
                if (entities.Any())
                {
                    var rectangle = entities.ToCollection().GeometricExtents().ToRectangle();
                    return rectangle.GetCentroidPoint().TransformBy(data.MCS2WCS);
                }
                else
                {
                    var rectangle = data.ToOBB();
                    return rectangle.GetCentroidPoint();
                }
            }
        }

        public static Polyline ToOBB(this ThBlockReferenceData data)
        {
            using (var acadDatabase = AcadDatabase.Active())
            {
                var blockTableRecord = acadDatabase.Blocks.Element(data.EffectiveName);
                var rectangle = blockTableRecord.GeometricExtents().ToRectangle();
                rectangle.TransformBy(data.MCS2WCS);
                return rectangle;
            }
        }
    }
}
