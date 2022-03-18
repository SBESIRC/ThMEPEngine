using System;
using System.Linq;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Draw;

namespace ThMEPStructure.Reinforcement.Command
{
    internal class ThRectTypeReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThRectTypeReinforceDrawCmd()
        {
            ActionName = "绘制标准一字型";
            CommandName = "XXXXXX";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active()) 
            {
                var recEdgeComponent = ThReinforceTestData.RectangleEdgeComponent;
                var objs = recEdgeComponent.Draw("1.0-2.0", 800, 4);
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
    internal class ThRectTypeCalReinforceDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThRectTypeCalReinforceDrawCmd()
        {
            ActionName = "绘制标准计算书一字型";
            CommandName = "XXXXXX";
        }
        public void Dispose()
        {
        }

        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var recEdgeComponent = ThReinforceTestData.RectangleCalEdgeComponent;
                var objs = recEdgeComponent.Draw("1.0-2.0", 800, 4);
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
}
