using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using System.IO;

namespace ThMEPElectrical.Block
{
    public static class BlockInsertDBExtension
    {
        public const string SENSOR_LAYER = "E-FAS-DEVC";
        public const string BLOCK_FILE = "烟感温感图块.dwg";
        public const string SMOKE_SENSOR_BLOCK_NAME = "E-BFAS110";
        public const string TEMPERATURE_SENSOR_BLOCK_NAME = "E-BFAS120";

        public static string BlockDwgPath(this Database database)
        {
            var fileName = database.Filename;
            var fileInfo = new FileInfo(fileName);
            var fileDirectoryName = fileInfo.DirectoryName;

            return Path.Combine(fileDirectoryName, BLOCK_FILE);
        }

        public static void InsertModel(this Database database, List<Point3d> insertPts, string name, Scale3d scale)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (var pt in insertPts)
                {
                    acadDatabase.ModelSpace.ObjectId.InsertBlockReference(SENSOR_LAYER, name, pt, scale, 0.0);
                }
            }
        }

        public static void ImportModel(this Database database, string name)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(database.BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(SENSOR_LAYER), false);
            }
        }
    }
}
