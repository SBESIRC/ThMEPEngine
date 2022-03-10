using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Algorithm;
using System.Collections.Generic;
using ThMEPHVAC.LoadCalculation.Model;
using ThMEPHVAC.LoadCalculation.Service;
using ThMEPEngineCore.IO.ExcelService;

namespace ThMEPHVAC.LoadCalculation.Command
{
    public class ThExportExcelCmd : ThMEPBaseCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public ThExportExcelCmd()
        {
            this.CommandName = "THDCFHJSB";
            this.ActionName = "导出负荷计算Excel";
        }

        public override void SubExecute()
        {
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
                var filter = ThSelectionFilterTool.Build(dxfNames, new string[] { LoadCalculationParameterFromConfig.Room_Layer_Name });
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

                if (rooms.Count==0)
                {
                    return;
                }

                var objs = rooms.Select(o => o.Boundary).ToCollection();
                ThMEPOriginTransformer originTransformer = new ThMEPOriginTransformer(objs);
                rooms.ForEach(x => originTransformer.Transform(x.Boundary));

                //提取近点
                GetPrimitivesService getPrimitivesService = new GetPrimitivesService(originTransformer);
                var roomFunctionBlocks = getPrimitivesService.GetRoomFunctionBlocks();

                LogicService logicService = new LogicService();
                var dataset = logicService.StatisticalData(rooms.Select(o => o.Boundary).ToList(), roomFunctionBlocks);

                //移回原点
                originTransformer.Reset(roomFunctionBlocks.ToCollection());

                try
                {
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = "房间信息"; // Default file name
                    dlg.DefaultExt = ".xlsx"; // Default file extension
                    dlg.Filter = "Xlsx (.xlsx)|*.xlsx"; // Filter files by extension
                                                        // Show save file dialog box
                    Nullable<bool> dlgresult = dlg.ShowDialog();

                    // Process save file dialog box results
                    if (dlgresult == true)
                    {
                        // Save document
                        var filePathUrl = dlg.FileName;
                        var excelSrevice = new ReadExcelService();
                        excelSrevice.ConvertDataSetToExcel(dataset, filePathUrl);
                        Active.Editor.WriteLine($"\n已保存至{filePathUrl}。");
                    }
                    else
                    {
                        return;
                    }
                }
                catch
                {
                    return;
                }
            }
        }
    }
}
