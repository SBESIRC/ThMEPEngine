using System;
using System.Linq;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Draw;
using Autodesk.AutoCAD.Geometry;
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
                double H, W;
                recEdgeComponent.InitAndCalTableSize("1.0-2.0", 800, 4,out H,out W);
                var objs = recEdgeComponent.Draw(H, W, new Point3d());
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
                double H, W;
                recEdgeComponent.InitAndCalTableSize("1.0-2.0", 800, 4, out H, out W);
                var objs = recEdgeComponent.Draw(H, W, new Point3d());
                objs.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
}
