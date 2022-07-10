using NFox.Cad;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using ThCADCore.NTS;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using ThCADExtension;

namespace ThMEPHVAC.FloorHeatingCoil.Data
{
    public class ThRoomSuperBoundaryProcessService
    {
        private DBObjectCollection CollectObjs { get; set; }
        private DBObjectCollection ModelSpaceEnties { get; set; }

        public ThRoomSuperBoundaryProcessService()
        {
            CollectObjs = new DBObjectCollection();
            ModelSpaceEnties = new DBObjectCollection();
        }

        public DBObjectCollection ProcessBoundary(DBObjectCollection obj, bool WithUI)
        {
            using (var docLock = Active.Document.LockDocument())
            {
                AddToSpace(obj);

                // 首先清空现有的PickFirst选择集
                Active.Editor.SetImpliedSelection(new ObjectId[0]);
                // 接着将模型添加到PickFirst选择集
                var array = ModelSpaceEnties.OfType<Entity>().Select(o => o.ObjectId).ToArray();
                Active.Editor.SetImpliedSelection(array);

                Active.Database.ObjectAppended += Database_ObjectAppended;
                if (WithUI == true)
                {
                    Active.Editor.PostCommand("SBND_ALL ");//要加空格，否则一直没有发送命令
                }
                else
                {
#if ACAD_ABOVE_2014
                    Active.Editor.Command("SBND_ALL");//异步，不用ui会等待选择
#else
                    ResultBuffer args = new ResultBuffer(
                        new TypedValue((int)LispDataType.Text, "_.SBND_ALL"),
                        new TypedValue((int)LispDataType.Text, " "));
                    Active.Editor.AcedCmd(args);
#endif
                }
                Active.Database.ObjectAppended -= Database_ObjectAppended;

                var roomBoundaries = GetBoundary(CollectObjs);

                Dispose();

                return roomBoundaries;
            }

        }




        private void Database_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is Hatch || e.DBObject is Curve)
            {
                CollectObjs.Add(e.DBObject);
            }
        }

        private DBObjectCollection GetBoundary(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                if (e is Polyline)
                {
                    results.Add(e.Clone() as Polyline);
                }
                else if (e is Hatch hatch)
                {
                    var curves = hatch.Boundaries();
                    curves.ForEach(c => results.Add(c));
                }
                else
                {
                    // not support
                }
            });
            return results;
        }
        public void Dispose()
        {
            Erase(CollectObjs);
            Erase(ModelSpaceEnties);
            CollectObjs.MDispose();
            ModelSpaceEnties.MDispose();
        }

        private static void Erase(DBObjectCollection objs)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                objs.OfType<Entity>().ForEach(e =>
                {
                    var entity = acadDb.Element<Entity>(e.Id, true);
                    if (!entity.IsErased)
                    {
                        entity.Erase();
                    }
                });
            }
        }

        private DBObjectCollection AddToSpace(DBObjectCollection boundaryObjs)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                //var resultDB = new DBObjectCollection();
                var entity = boundaryObjs.OfType<Entity>();
                foreach (Entity obj in entity)
                {
                    var e = obj.Clone() as Entity;
                    ModelSpaceEnties.Add(e);
                }

                foreach (Entity e in ModelSpaceEnties)
                {
                    acadDb.ModelSpace.Add(e);
                    //e.Layer = "0";
                    //e.ColorIndex = (int)ColorIndex.BYLAYER;
                    //e.LineWeight = LineWeight.ByLayer;
                    //e.Linetype = "ByLayer";
                }

                return ModelSpaceEnties;
            }
        }
    }
}
