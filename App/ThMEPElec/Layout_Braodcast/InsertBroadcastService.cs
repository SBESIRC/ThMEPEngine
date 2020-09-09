using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.Model;
using TianHua.AutoCAD.Utility.ExtensionTools;

namespace ThMEPElectrical.Layout_Braodcast
{
    public static class InsertBroadcastService
    {
        private static double scaleNum = 100;

        public static void InsertSprayBlock(List<ColumnModel> insertPts)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel(ThMEPCommon.BroadcastDwgName);
                foreach (var col in insertPts)
                {
                    db.Database.InsertModel(col.layoutPoint + col.layoutDirection * scaleNum * 1.5, col.layoutDirection, new Dictionary<string, string>(){
                        { "F","W" },
                    });
                }
            }
        }

        public static ObjectId InsertModel(this Database database, Point3d pt, Vector3d layoutDir, Dictionary<string, string> attNameValues)
        {
            double rotateAngle = Vector3d.YAxis.GetAngleTo(layoutDir);
            //控制旋转角度
            if (layoutDir.DotProduct(-Vector3d.XAxis) < 0)
            {
                rotateAngle = -rotateAngle;
            }

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    ThMEPCommon.BroadcastLayerName,
                    ThMEPCommon.BroadcastBlockName,
                    pt,
                    new Scale3d(scaleNum),
                    rotateAngle,
                    attNameValues);
            }
        }

        public static void ImportModel(this Database database, string name)
        {
            var filePath = Path.Combine(ThCADCommon.SupportPath(), name);
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(filePath, DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThMEPCommon.BroadcastBlockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPCommon.BroadcastLayerName), false);
            }
        }
    }
}
