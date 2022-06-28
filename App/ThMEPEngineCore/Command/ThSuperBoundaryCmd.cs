using System;
using System.Linq;
using NFox.Cad;
using AcHelper;
using Linq2Acad;
using ThCADCore.NTS;
using ThMEPEngineCore;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.CAD;
using System.Runtime.InteropServices;

namespace ThMEPWSS.Command
{
    public class ThSuperBoundaryCmd : ThMEPBaseCommand, IDisposable
    {
        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("accore.dll", EntryPoint = "acedCmd", CallingConvention = CallingConvention.Cdecl)]
        private static extern int acedCmd(IntPtr vlist);

        [DllImport("accore.dll", EntryPoint = "acedCommand")]
        private static extern int acedCommand(IntPtr vlist);

        ////CAD2008和2008以前版本中的EntryPoint名称和2009以及2009之后的版本不一样
        //[DllImport("accore.dll", CharSet = CharSet.Auto, EntryPoint = "?acedPostCommand@@YAHPB_W@Z", CallingConvention = CallingConvention.Cdecl)]
        //private static extern int acedPostCommand(string strExpr);
        //CAD2008之后的版本中
        [DllImport("accore.dll", CharSet = CharSet.Auto, EntryPoint = "?acedPostCommand@@YAHPEB_W@Z", CallingConvention = CallingConvention.Cdecl)]
        private static extern int acedPostCommand(string strExpr);

        private DBObjectCollection ModelSpaceEnties { get; set; }
        private DBObjectCollection CollectObjs { get; set; }
        public ThSuperBoundaryCmd(DBObjectCollection modelSpaceEnties)
        {
            ModelSpaceEnties = modelSpaceEnties;
            CollectObjs = new DBObjectCollection();
        }

        public void Dispose()
        {
            //
            Active.Database.ObjectAppended -= Database_ObjectAppended;
            Erase(CollectObjs);
            CollectObjs.MDispose();
        }

        public override void SubExecute()
        {
            // 首先清空现有的PickFirst选择集
            Active.Editor.SetImpliedSelection(new ObjectId[0]);
            // 接着将模型添加到PickFirst选择集
            Active.Editor.SetImpliedSelection(ModelSpaceEnties.OfType<Entity>().Select(o => o.ObjectId).ToArray());

            Active.Database.ObjectAppended += Database_ObjectAppended;
            acedPostCommand("SBND_PICK ");

            var roomBoundaries = GetRoomBoundaries(CollectObjs);
            PrintRooms(roomBoundaries);
        }

        private void Database_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is Hatch || e.DBObject is Curve)
            {
                CollectObjs.Add(e.DBObject);
            }
        }

        private DBObjectCollection GetRoomBoundaries(DBObjectCollection objs)
        {
            var results = new DBObjectCollection();
            objs.OfType<Entity>().ForEach(e =>
            {
                if(e is Polyline || e is Circle circle || e is Ellipse)
                {
                    results.Add(e.Clone() as Curve);
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

        private void PrintRooms(DBObjectCollection roomBoundaries)
        {
            using (var acadDb = AcadDatabase.Active())
            {
                // 将房间框线移动到原位置
                var layerId = acadDb.Database.CreateAIRoomOutlineLayer();
                roomBoundaries.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.LayerId = layerId;
                    e.ColorIndex = (int)ColorIndex.BYLAYER;
                    e.LineWeight = LineWeight.ByLayer;
                    e.Linetype = "ByLayer";
                });
            }
        }

        private void Erase(DBObjectCollection objs)
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
    }
}

