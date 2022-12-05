using System;
using System.Linq;
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.Command;

namespace ThPlatform3D.Command
{
    public class ThDrawLineCmd : ThMEPBaseCommand, IDisposable
    {
        private DBObjectCollection _collectObjs;
        private DBObjectCollection _lines;
        public DBObjectCollection Lines => _lines;
        public ThDrawLineCmd()
        {
            ActionName = "绘制线";
            CommandName = "";
            _lines = new DBObjectCollection();
            _collectObjs =new DBObjectCollection();
        }
        public void Dispose()
        {
            Erase();
        }

        public override void SubExecute()
        {
            Active.Database.ObjectAppended += Database_ObjectAppended;
            //Active.Editor.Command("._Line");
            Active.Editor.PostCommand("Line ");
            Active.Database.ObjectAppended -= Database_ObjectAppended;
        }

        private void Erase()
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                _collectObjs.OfType<Entity>().ForEach(e =>
                {
                    DbHelper.EnsureLayerOn(e.Layer);
                    var entity = acadDb.Element<Entity>(e.ObjectId,true);
                    entity.Erase();
                });
            }
        }

        private void Database_ObjectAppended(object sender, ObjectEventArgs e)
        {
            if (e.DBObject is Line)
            {
                _collectObjs.Add(e.DBObject);
                _lines.Add(e.DBObject.Clone() as Line);
            }
        }
    }
}
