using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System;
using System.Linq;
using ThCADCore.NTS;
using ThMEPEngineCore;
using Dreambuild.AutoCAD;

namespace TianHua.Electrical.PDS.Service
{
    public class ThPDSInsertRevcloudService
    {
        public static void InsertRevcloud(Database active, DBObjectCollection objs, Tuple<string, short> tuple)
        {
            objs.OfType<Polyline>().ForEach(obb =>
            {
                InsertRevcloud(active, obb, tuple.Item1, tuple.Item2);
            });

        }

        private static void InsertRevcloud(Database active, Polyline obb, string layer, short colorIndex)
        {
            // 创建云线
            using (var db = AcadDatabase.Use(active))
            {
                var layerId = db.Database.CreateAILayer(layer, colorIndex);
                ObjectId revcloud = ObjectId.Null;
                var buffer = obb.Buffer(300);
                var objId = db.ModelSpace.Add(buffer[0] as Entity);
                void handler(object s, ObjectEventArgs e)
                {
                    if (e.DBObject is Polyline polyline)
                    {
                        revcloud = e.DBObject.ObjectId;
                    }
                }
                db.Database.ObjectAppended += handler;
#if ACAD_ABOVE_2014
                Active.Editor.Command("_.REVCLOUD", "_arc", 300, 300, "_Object", objId, "_No");
#else
                    ResultBuffer args = new ResultBuffer(
                       new TypedValue((int)LispDataType.Text, "_.REVCLOUD"),
                       new TypedValue((int)LispDataType.Text, "_ARC"),
                       new TypedValue((int)LispDataType.Text, "300"),
                       new TypedValue((int)LispDataType.Text, "300"),
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
            }
        }
    }
}
