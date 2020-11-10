using Linq2Acad;
using DotNetARX;
using System.IO;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.Block
{
    public static class BlockInsertDBExtension
    {
        public static string BlockDwgPath(this Database database)
        {
            return Path.Combine(ThCADCommon.SupportPath(), ThMEPCommon.SENSORDWGNAME);
        }

        public static void InsertModel(this Database database, List<Point3d> insertPts, string name, Scale3d scale, double angle = 0)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var pt in insertPts)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(ThMEPCommon.SENSORLAYERNMAE,name,pt,scale, angle);
                }
            }
        }

        public static void ImportModel(this Database database, string name)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(database.BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPCommon.SENSORLAYERNMAE), false);
            }
        }
    }
}
