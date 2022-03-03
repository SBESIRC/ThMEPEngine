using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using AcHelper;
using Linq2Acad;

using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.IO;

using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;
using ThMEPWSS.HydrantLayout.Command;
using ThMEPWSS.HydrantLayout.Model;

namespace ThMEPWSS
{
    public partial class ThHydrantCmds
    {
        [CommandMethod("TIANHUACAD", "THHydrantLayoutNoUI", CommandFlags.Modal)]
        public void THHydrantLayout()
        {
            using (var cmd = new ThHydrantLayoutCmd())
            {
                cmd.Execute();
            }
        }

        [CommandMethod("TIANHUACAD", "THHydrantLayoutNoUI", CommandFlags.Modal)]
        public void THHydrantLayoutNoUI()
        {

            var hintObject = new Dictionary<string, (string, string)>()
                        {{"0",("0","消火栓")},
                        {"1",("1","灭火器")},
                        {"2",("2","消火栓 & 灭火器")},
                        };
            var layoutObject = ThHydrantUtil.SettingSelection("\n优化对象", hintObject, "2");
            var radius = ThHydrantUtil.SettingInt("\n半径", 3000);


            var hintMode = new Dictionary<string, (string, string)>()
                        {{"0",("0","一字")},
                        {"1",("1","L字")},
                        {"2",("2","自由布置")},
                        };
            var layoutMode = ThHydrantUtil.SettingSelection("\n摆放方式", hintMode, "2");

            HydrantLayoutSetting.Instance.LayoutObject = Convert.ToInt32(layoutObject);
            HydrantLayoutSetting.Instance.SearchRadius = radius;
            HydrantLayoutSetting.Instance.LayoutMode = Convert.ToInt32(layoutMode);

            using (var cmd = new ThHydrantLayoutCmd())
            {
                cmd.Execute();
            }
        }


        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "THHydrantData", CommandFlags.Modal)]
        public void THHydrantData()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                //画框，提数据，转数据
                var selectPts = ThSelectFrameUtil.GetFrame();
                if (selectPts.Count == 0)
                {
                    return;
                }

                var transformer = ThHydrantUtil.GetTransformer(selectPts);

                var BlockNameDict = new Dictionary<string, List<string>>() {
                                        {"集水井", new List<string>() { "A-Well-1" }},
                                        {"非机械车位", new List<string>() { "car0" } } };

                var dataFactory = new ThHydrantLayoutDataFactory()
                {
                    Transformer = transformer,
                    BlockNameDict = BlockNameDict,
                };
                dataFactory.GetElements(acadDatabase.Database, selectPts);

                var dataQuery = new ThHydrantLayoutDataQueryService()
                {
                    VerticalPipe = dataFactory.VerticalPipe,
                    Hydrant = dataFactory.Hydrant,
                    InputExtractors = dataFactory.Extractors,
                    Car = dataFactory.Car,
                    Well = dataFactory.Well,
                };

                dataQuery.ProcessArchitechData();
                dataQuery.ProcessHydrant();
                dataQuery.Transform(transformer);
                dataQuery.Print();
                dataQuery.Reset(transformer);
                dataQuery.Print();
                // dataQuery.Clean();


            }
        }
    }
}
