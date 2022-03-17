using System;
using System.Linq;
using System.Collections.Generic;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Command;
using ThMEPStructure.Reinforcement.Draw;
using ThMEPStructure.Reinforcement.Model;

namespace ThMEPStructure.Reinforcement.Command
{
    internal class ThReinforceTableDrawCmd : ThMEPBaseCommand, IDisposable
    {
        public ThReinforceTableDrawCmd()
        {
            ActionName = "生成柱表";
            CommandName = "XXXXXX";
        }
        public void Dispose()
        {
        }
        public override void SubExecute()
        {
            using (var acadDb = AcadDatabase.Active())
            {
                var tableBuilder = new ThReinforceTableBuilder(
                    "A1","0.000~3.000","1:25",800);
                var datas = new List<ThEdgeComponent>();
                datas.Add(ThReinforceTestData.RectangleEdgeComponent);
                datas.Add(ThReinforceTestData.RectangleCalEdgeComponent);
                datas.Add(ThReinforceTestData.LTypeEdgeComponent);
                datas.Add(ThReinforceTestData.LTypeCalEdgeComponent);
                datas.Add(ThReinforceTestData.TTypeEdgeComponent);
                datas.Add(ThReinforceTestData.TTypeCalEdgeComponent);

                datas.Add(ThReinforceTestData.RectangleEdgeComponent);
                datas.Add(ThReinforceTestData.RectangleCalEdgeComponent);
                datas.Add(ThReinforceTestData.LTypeEdgeComponent);
                datas.Add(ThReinforceTestData.LTypeCalEdgeComponent);
                datas.Add(ThReinforceTestData.TTypeEdgeComponent);
                datas.Add(ThReinforceTestData.TTypeCalEdgeComponent);

                datas.Add(ThReinforceTestData.RectangleEdgeComponent);
                datas.Add(ThReinforceTestData.RectangleCalEdgeComponent);
                datas.Add(ThReinforceTestData.LTypeEdgeComponent);
                datas.Add(ThReinforceTestData.LTypeCalEdgeComponent);
                datas.Add(ThReinforceTestData.TTypeEdgeComponent);
                datas.Add(ThReinforceTestData.TTypeCalEdgeComponent);

                var results = tableBuilder.Build(datas);
                results.OfType<Entity>().ForEach(e =>
                {
                    acadDb.ModelSpace.Add(e);
                    e.SetDatabaseDefaults();
                });
            }
        }
    }
}
