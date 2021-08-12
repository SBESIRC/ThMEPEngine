using Linq2Acad;
using DotNetARX;
using System.IO;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.CAD
{
    public static class BlockInsertDBExtension
    {
        public static void InsertModel(this Database database, string layer, string name, Point3d position, Scale3d scale, double angle)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, name, position, scale, angle);
            }
        }

        public static void ImportModel(this Database database, string name, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalSensorDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }

        public static void ImportLinetype(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalBroadcastDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportLayer(this Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalBroadcastDwgPath(), DwgOpenMode.ReadOnly, false)) 
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportLinetype(this Database database, string url, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(url, DwgOpenMode.ReadOnly, false))
            {
                currentDb.Linetypes.Import(blockDb.Linetypes.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        public static void ImportLayer(this Database database, string url, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(url, DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(name), replaceIfDuplicate);
            }
        }
    }
}
