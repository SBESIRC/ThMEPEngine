using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPLighting.ParkingStall.Business.Block
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ParkStallLightDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }
    }
}
