using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.ElectricalLoadCalculation;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Model;

namespace ThMEPElectrical.Command
{
    class ThElectricalLoadCalculationCommand : ThMEPBaseCommand, IDisposable
    {
        public ThElectricalLoadCalculationCommand()
        {
            this.CommandName = "THYDFHSC";
            this.ActionName = "生成用电负荷计算结果";
        }

        public override void SubExecute()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var database = AcadDatabase.Active())
            {
                // 获取房间框线
                PromptSelectionOptions options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var dxfNames = new string[]
                {
                    RXClass.GetClass(typeof(Polyline)).DxfName,
                };
                var filter = ThSelectionFilterTool.Build(dxfNames, new string[] { ElectricalLoadCalculationConfig.Room_Layer_Name });
                var result = Active.Editor.GetSelection(options, filter);
                if (result.Status != PromptStatus.OK)
                {
                    return;
                }
                ObjectIdCollection dBObject = new ObjectIdCollection();
                foreach (ObjectId objid in result.Value.GetObjectIds())
                {
                    dBObject.Add(objid);
                }
                var roomEngine = new ThDB3RoomOutlineRecognitionEngine();
                roomEngine.RecognizeMS(database.Database, dBObject);
                var rooms = roomEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markEngine = new ThDB3RoomMarkRecognitionEngine();
                var marks = markEngine.Elements.Cast<ThIfcTextNote>().ToList();
                var roomBuilder = new ThRoomBuilderEngine();
                roomBuilder.Build(rooms, marks);

                if (rooms.Count==0)
                {
                    return;
                }

                //初始化图纸(导入图层/图块/图层三板斧等)
                ElectricalLoadCalculationService.initialization();

                var objs = rooms.Select(o => o.Boundary).ToCollection();
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(objs);
                rooms.ForEach(x => originTransformer.Transform(x.Boundary));

                //提取近点
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                var roomFunctionBlocks = getPrimitivesService.GetRoomFunctionBlocks();
                var loadCalculationtables = getPrimitivesService.GetLoadCalculationTables();
                var curves = getPrimitivesService.GetLoadCalculationCurves();

                LogicService logicService = new LogicService();
                List<Table> Deprecatedtables;
                var tables = logicService.InsertLoadCalculationTable(database.Database, rooms.Select(o => o.Boundary).ToList(), roomFunctionBlocks, loadCalculationtables, curves, out Deprecatedtables);

                //移回原点
                originTransformer.Reset(tables.ToCollection());
                originTransformer.Reset(roomFunctionBlocks.ToCollection());
                originTransformer.Reset(loadCalculationtables.ToCollection());
                ElectricalLoadCalculationService.InsertTable(tables);
                ElectricalLoadCalculationService.DeleteTable(Deprecatedtables);
            }
        }

        public void Dispose()
        {
            //
        }
    }
}
