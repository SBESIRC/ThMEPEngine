using System;
using System.IO;
using DotNetARX;
using Linq2Acad;
using ThMEPElectrical.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPElectrical.Broadcast
{
    public static class InsertBroadcastService
    {
        private static double scaleNum = 100;

        public static void InsertSprayBlock(List<ColumnModel> insertPts)
        {
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel();
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

        public static void ImportModel(this Database database)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThMEPCommon.BroadcastBlockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPCommon.BroadcastLayerName), false);
            }
        }

        private static string BlockDwgPath()
        {
            return System.IO.Path.Combine(ThCADCommon.SupportPath(), ThMEPCommon.BroadcastDwgName);
        }
    }
}
