using System;
using System.IO;
using DotNetARX;
using Linq2Acad;
using ThMEPElectrical.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;

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
                    db.Database.InsertModel(col.layoutPoint + col.layoutDirection * scaleNum * 1.5, -col.layoutDirection, new Dictionary<string, string>(){
                        { "F","W" },
                    });
                }
            }
        }

        public static List<BlockReference> InsertSprayBlock(Dictionary<List<Line>, Dictionary<Point3d, Vector3d>> insertPtInfo, ThMEPOriginTransformer originTransformer)
        {
            List<BlockReference> broadcasts = new List<BlockReference>();
            using (var db = AcadDatabase.Active())
            {
                db.Database.ImportModel();
                foreach (var ptDic in insertPtInfo)
                {
                    foreach (var ptInfo in ptDic.Value)
                    {
                        var intsertPt = ptInfo.Key;
                        originTransformer.Reset(ref intsertPt);
                        var id = db.Database.InsertModel(intsertPt + ptInfo.Value * scaleNum * 1.5, -ptInfo.Value, new Dictionary<string, string>(){
                            { "F","W" },
                        });
                        broadcasts.Add(db.Element<BlockReference>(id));
                    }
                }
            }

            return broadcasts;
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
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalBroadcastDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(ThMEPCommon.BroadcastBlockName), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPCommon.BroadcastLayerName), false);
            }
        }
    }
}
