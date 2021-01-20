using System;
using System.Linq;
using Linq2Acad;
using DotNetARX;
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
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY))
            {
                dynamicProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY, model);
            }
            else
            {
                return;
            }
        }

        public static void SetValveWidth(this ObjectId obj, double width, string widthproperty)
        {
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(widthproperty))
            {
                dynamicProperties.SetValue(widthproperty, width);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static void SetValveHeight(this ObjectId obj, double height, string lengthproperty)
        {
            var dynamicProperties = obj.GetDynProperties();
            if (dynamicProperties.Contains(lengthproperty))
            {
                dynamicProperties.SetValue(lengthproperty, height);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Point3d GetValveBasePoint(this ObjectId obj)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockRef = acadDatabase.Element<BlockReference>(obj);
                return new Point3d(blockRef.Position.X, blockRef.Position.Y, 0);
            }
        }

    }
}
