using System.Collections.Generic;

using AcHelper;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;

using ThMEPEngineCore;
using ThMEPLighting.EmgLight.Common;
using ThCADExtension;

namespace ThMEPLighting.EmgLight.Service
{
    public static class InsertLightService
    {
        public static void InsertSprayBlock(Dictionary<Polyline, (Point3d, Vector3d)> insertPtInfo, double scale, string blkName)
        {
            using (var db = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.AutoFireAlarmSystemDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                db.Blocks.Import(blockDb.Blocks.ElementOrDefault(blkName), true);
                db.Layers.Import(blockDb.Layers.ElementOrDefault(ThMEPLightingCommon.EmgLightLayerName), true);
                foreach (var ptInfo in insertPtInfo)
                {
                    var size = EmgLightCommon.blk_move_length[blkName];
                    db.Database.InsertModel(ptInfo.Value.Item1 + ptInfo.Value.Item2 * scale * size, ptInfo.Value.Item2, new Dictionary<string, string>() { }, scale, blkName);
                }
            }
        }

        private static ObjectId InsertModel(this Database database, Point3d pt, Vector3d layoutDir, Dictionary<string, string> attNameValues, double scale, string blkName)
        {
            double rotateAngle = Vector3d.YAxis.GetAngleTo(layoutDir, Vector3d.ZAxis);

            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    ThMEPLightingCommon.EmgLightLayerName,
                    blkName,
                    pt,
                    new Scale3d(scale),
                    rotateAngle,
                    attNameValues);
            }
        }

        public static void InsertRevcloud(List<Polyline> commentLine)
        {
            using (var db = AcadDatabase.Active())
            {
                var layerId = db.Database.CreateAILayer(EmgLightCommon.LayerComment, 1);
                commentLine.ForEach(o =>
                {
                    // 创建云线
                    ObjectId revcloud = ObjectId.Null;
                    var objId = db.ModelSpace.Add(o);
                    void handler(object s, ObjectEventArgs e)
                    {
                        if (e.DBObject is Polyline polyline)
                        {
                            revcloud = e.DBObject.ObjectId;
                        }
                    }
                    db.Database.ObjectAppended += handler;
#if ACAD_ABOVE_2014
                    Active.Editor.Command("_.REVCLOUD", "_arc", 500, 500, "_Object", objId, "_No");
#else
                    ResultBuffer args = new ResultBuffer(
                       new TypedValue((int)LispDataType.Text, "_.REVCLOUD"),
                       new TypedValue((int)LispDataType.Text, "_ARC"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "500"),
                       new TypedValue((int)LispDataType.Text, "_Object"),
                       new TypedValue((int)LispDataType.ObjectId, objId),
                       new TypedValue((int)LispDataType.Text, "_No"));
                    Active.Editor.AcedCmd(args);
#endif
                    db.Database.ObjectAppended -= handler;

                    // 设置运行属性
                    var revcloudObj = db.Element<Entity>(revcloud, true);
                    revcloudObj.LayerId = layerId;
                    revcloudObj.ColorIndex = (int)ColorIndex.BYLAYER;
                });
            }
        }
    }
}
