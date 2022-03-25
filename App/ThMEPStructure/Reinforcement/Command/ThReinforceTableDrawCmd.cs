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
                // A0(1189,841) A1(841,594) A2(594,420) A3(420,297) A4(210,297)
                var extents = new Extents2d(0,0,84100,59400);
                var tableBuilder = new ThReinforceTableBuilder(
                    extents, "0.000~3.000","1:25",800);
                var datas = new List<ThEdgeComponent>();
                datas = ThReinforceTestData.StandardTestDatas;
                //datas.Add(ThReinforceTestData.RectangleEdgeComponent);
                //datas.Add(ThReinforceTestData.RectangleCalEdgeComponent);
                //datas.Add(ThReinforceTestData.LTypeEdgeComponent);
                //datas.Add(ThReinforceTestData.LTypeCalEdgeComponent);
                //datas.Add(ThReinforceTestData.TTypeEdgeComponent);
                //datas.Add(ThReinforceTestData.TTypeCalEdgeComponent);

                //datas.Add(ThReinforceTestData.RectangleEdgeComponent);
                //datas.Add(ThReinforceTestData.RectangleCalEdgeComponent);
                //datas.Add(ThReinforceTestData.LTypeEdgeComponent);
                //datas.Add(ThReinforceTestData.LTypeCalEdgeComponent);
                //datas.Add(ThReinforceTestData.TTypeEdgeComponent);
                //datas.Add(ThReinforceTestData.TTypeCalEdgeComponent);

                //datas.Add(ThReinforceTestData.RectangleEdgeComponent);
                //datas.Add(ThReinforceTestData.RectangleCalEdgeComponent);
                //datas.Add(ThReinforceTestData.LTypeEdgeComponent);
                //datas.Add(ThReinforceTestData.LTypeCalEdgeComponent);
                //datas.Add(ThReinforceTestData.TTypeEdgeComponent);
                //datas.Add(ThReinforceTestData.TTypeCalEdgeComponent);

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
