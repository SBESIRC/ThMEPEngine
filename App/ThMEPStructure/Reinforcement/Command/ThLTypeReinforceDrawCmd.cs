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
    internal class ThLTypeReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThLTypeReinforceDrawCmd()
        {
            ActionName = "绘制标准L型";
            CommandName = "XXXXXX";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active()) 
            {
                var lEdgeComponent = ThReinforceTestData.LTypeEdgeComponent;
                var objs = lEdgeComponent.Draw();
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
    internal class ThLTypeCalReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThLTypeCalReinforceDrawCmd()
        {
            ActionName = "绘制标准计算书L型";
            CommandName = "XXXXXX";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var lEdgeComponent = ThReinforceTestData.LTypeCalEdgeComponent;
                var objs = lEdgeComponent.Draw();
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
}
