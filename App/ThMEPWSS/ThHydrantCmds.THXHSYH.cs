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
using ThMEPEngineCore.Diagnostics;

using ThMEPWSS.Common;
using ThMEPWSS.HydrantLayout;
using ThMEPWSS.HydrantLayout.Command;
using ThMEPWSS.HydrantLayout.Service;
using ThMEPWSS.HydrantLayout.Data;
using ThMEPWSS.HydrantLayout.Model;
using ThMEPWSS.HydrantLayout.Engine;

namespace ThMEPWSS
{
    public partial class ThHydrantCmds
    {

        [CommandMethod("TIANHUACAD", "-THXHSYH", CommandFlags.Modal)]
        public void THHydrantLayoutNoUI()
        {

            var hintObject = new Dictionary<string, (string, string)>()
                        {{"0",("0","消火栓")},
                        {"1",("1","灭火器")},
                        {"2",("2","消火栓 & 灭火器")},
                        };
            var layoutObject = ThMEPWSSUtils.SettingSelection("\n优化对象", hintObject, "2");
            var radius = ThMEPWSSUtils.SettingInt("\n半径", 3000);


            var hintMode = new Dictionary<string, (string, string)>()
                        {{"0",("0","一字")},
                        {"1",("1","L字")},
                        {"2",("2","自由布置")},
                        };
            var layoutMode = ThMEPWSSUtils.SettingSelection("\n摆放方式", hintMode, "2");


            var avoidParking = ThMEPWSSUtils.SettingBoolean("\n车位是否阻挡开门", 1);
            var layoutInMid = ThMEPWSSUtils.SettingBoolean("\n一字布置在中间", 0);

            HydrantLayoutSetting.Instance.LayoutObject = Convert.ToInt32(layoutObject);
            HydrantLayoutSetting.Instance.SearchRadius = radius;
            HydrantLayoutSetting.Instance.LayoutMode = Convert.ToInt32(layoutMode);
            HydrantLayoutSetting.Instance.AvoidParking = avoidParking;
            HydrantLayoutSetting.Instance.LayoutInMiddle = layoutInMid;
            HydrantLayoutSetting.Instance.BlockNameDict = new Dictionary<string, List<string>>()
                                {
                                    { "集水井", new List<string>() { "A-Well-1" }},
                                    { "非机械车位", new List<string>() { "43543trer123" , "C充电车位", "ert54645645", "C18196EFF", "ret456546434543543", "机械车位", "car0", "停车位4", "A-Parking-1", "C514C01F1", "car", "C614A45C8", "4213", "C6356253C", "车位5100", "bkcw", "C0A575437", "停车位2" ,"独立停垂直式小型车位1" } }

                                };
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

                //var transformer = ThHydrantUtil.GetTransformer(selectPts);
                var transformer = new ThMEPOriginTransformer(new Point3d(0, 0, 0));

                var BlockNameDict = new Dictionary<string, List<string>>() {
                                        {"集水井", new List<string>() { "A-Well-1" }},
                                        {"非机械车位", new List<string>() { "car0", "停车位4", "A-Parking-1", "C514C01F1", "car", "C614A45C8", "4213", "C6356253C", "车位5100", "bkcw", "C0A575437" , "停车位2" } } };

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
                dataQuery.ProjectOntoXYPlane();
                dataQuery.Print();

                //dataQuery.Reset(transformer);
                //dataQuery.Print();
                //dataQuery.Clean();

            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [CommandMethod("TIANHUACAD", "ThLoadBlkTemplate", CommandFlags.Modal)]
        public void ThLoadBlkTemplate()
        {
            using (var doclock = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {

                var blkNameList = new List<string> { "室内消火栓平面" };
                var layerNameList = new List<string> { "" };

                InsertBlkService.LoadBlockLayerToDocument(acadDatabase.Database, blkNameList, layerNameList);
            }
        }
    }
}
