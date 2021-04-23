using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Service
{
    public static class ThInsertStoreyFrameService
    {
        public static void Insert(List<Point3d> positions)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                string name = "楼层框定";
                var attNameValues = new Dictionary<string, string>() { { "楼层编号", string.Empty } };            
                acadDatabase.Database.ImportBlock(name, ThWPipeCommon.AD_FLOOR_AREA);
                positions.ForEach(o =>
                {
                    acadDatabase.Database.InsertBlock(ThWPipeCommon.AD_FLOOR_AREA, name, o, new Scale3d(1), 0, attNameValues);
                });
            }
        }

        private static void InsertBlock(this Database database, string layer, string name, Point3d position, Scale3d scale, double angle, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                acadDatabase.ModelSpace.ObjectId.InsertBlockReference(layer, name, position, scale, angle, attNameValues);
            }
        }

        /// <summary>
        /// 导入图块
        /// </summary>
        /// <param name="database"></param>
        /// <param name="name"></param>
        /// <param name="layer"></param>
        private static void ImportBlock(this Database database, string name, string layer)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.StoreyFrameDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                acadDatabase.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);               
            }
        }
    }
}
