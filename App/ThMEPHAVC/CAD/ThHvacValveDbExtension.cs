using DotNetARX;
using Linq2Acad;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.CAD
{
    public static class ThHvacValveDbExtension
    {
        public static ObjectId InsertValve(this Database database, string name, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layer,
                    name,
                    Point3d.Origin,
                    new Scale3d(1.0),
                    0.0);
            }
        }

        public static void ImportValve(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportLayer(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportLinetype(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportTextStyle(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacPipeDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void SetValveModel(this ObjectId obj, string model)
        {
            obj.SetDynBlockValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY, model);
        }

        public static void SetValveWidth(this ObjectId obj, double width, string widthproperty)
        {
            obj.SetDynBlockValue(widthproperty, width);
        }

        public static void SetValveHeight(this ObjectId obj, double height, string lengthproperty)
        {
            obj.SetDynBlockValue(lengthproperty, height);
        }

        public static void SetValveTextHeight(this ObjectId obj, double height, string textheightproperty)
        {
            obj.SetDynBlockValue(textheightproperty, height);
        }

        public static void SetValveTextRotate(this ObjectId obj, double angle, string textrotateproperty)
        {
            obj.SetDynBlockValue(textrotateproperty, angle);
        }
    }
}
