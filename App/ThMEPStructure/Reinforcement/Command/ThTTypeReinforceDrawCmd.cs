using System;
using System.Linq;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Draw;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Command
{
    internal class ThTTypeReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThTTypeReinforceDrawCmd()
        {
            ActionName = "绘制标准T型";
            CommandName = "XXXXXX";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active()) 
            {
                var tEdgeComponent = ThReinforceTestData.TTypeEdgeComponent;
                var objs = tEdgeComponent.Draw("1.0-2.0", 800, 4);
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
    internal class ThTTypeCalReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThTTypeCalReinforceDrawCmd()
        {
            ActionName = "绘制标准计算书T型";
            CommandName = "XXXXXX";
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var tEdgeComponent = ThReinforceTestData.TTypeCalEdgeComponent;
                var objs = tEdgeComponent.Draw("1.0-2.0", 800, 4);
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
}
