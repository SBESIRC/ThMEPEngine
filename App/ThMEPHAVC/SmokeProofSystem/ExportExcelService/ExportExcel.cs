using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using Microsoft.Win32;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.IO;
using ThMEPEngineCore.IO.JSON;
using ThMEPHVAC.SmokeProofSystem.Model;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels.ThSmokeProofMappingModel;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService
{
    public class ExportExcel
    {
        public void Export()
        {
            var blockLst = SelectBlock();
            if (blockLst.Count <= 0)
            {
                return;
            }
            var models = GetExportModel(blockLst);

            var i = 4;
            ExcelRangeCopyOperator copyOperatorForVolumeModel = new ExcelRangeCopyOperator();
            using (var Targetpackage = ThExportExcelUtils.CreateModelCalculateExcelPackage())
            using (var volumeSourcepackage = ThExportExcelUtils.CreateSmokeProofExcelPackage())
            {
                models.ForEach(p =>
                {
                    if (!p.IsNull())
                    {
                        ExcelExportEngine.Instance.Model = p;

                        if (p.baseSmokeProofViewModel != null)
                        {
                            ExcelExportEngine.Instance.RangeCopyOperator = copyOperatorForVolumeModel;
                            ExcelExportEngine.Instance.Sourcebook = volumeSourcepackage.Workbook;
                            ExcelExportEngine.Instance.Targetsheet = Targetpackage.Workbook.Worksheets["防烟计算"];
                            ExcelExportEngine.Instance.Run();
                        }
                    }

                    i++;
                });

                SaveFileDialog _SaveFileDialog = new SaveFileDialog();
                _SaveFileDialog.Filter = "Xlsx Files(*.xlsx)|*.xlsx";
                _SaveFileDialog.RestoreDirectory = true;
                _SaveFileDialog.InitialDirectory = Active.DocumentDirectory;
                _SaveFileDialog.FileName = "正压送风计算书 - " + DateTime.Now.ToString("yyyy.MM.dd HH.mm");
                var DialogResult = _SaveFileDialog.ShowDialog();
                if (DialogResult.Value)
                {
                    var _FilePath = _SaveFileDialog.FileName.ToString();
                    Targetpackage.SaveAs(new FileInfo(_FilePath));
                }
            }
        }

        /// <summary>
        /// 获取数据模型
        /// </summary>
        /// <param name="blocks"></param>
        /// <returns></returns>
        private List<VolumeExportModel> GetExportModel(List<BlockReference> blocks)
        {
            var exportModels = new List<VolumeExportModel>();
            foreach (var block in blocks)
            {
                var flexData = FlexDataStoreExtensions.FlexDataStore(block.Id);
                var mainVal = flexData.GetValue(FlexDataKeyType.MianVm.ToString());
                var scenarioVal = flexData.GetValue(FlexDataKeyType.UserControlVm.ToString());
                var model = JsonHelper.DeserializeJsonToObject<SmokeCalculateMappingModel>(mainVal);

                VolumeExportModel volumeExportModel = new VolumeExportModel();
                volumeExportModel.ScenarioTitle = model.ScenarioTitle;
                switch (model.ScenarioTitle)
                {
                    case "消防电梯前室":
                        var fireElevatorFrontRoomViewModel = JsonHelper.DeserializeJsonToObject<FireElevatorFrontRoomViewModel>(scenarioVal);
                        volumeExportModel.baseSmokeProofViewModel = fireElevatorFrontRoomViewModel;
                        break;
                    case "独立或合用前室（楼梯间自然）":
                        var separateOrSharedNaturalViewModel = JsonHelper.DeserializeJsonToObject<SeparateOrSharedNaturalViewModel>(scenarioVal);
                        volumeExportModel.baseSmokeProofViewModel = separateOrSharedNaturalViewModel;
                        break;
                    case "独立或合用前室（楼梯间送风）":
                        var separateOrSharedWindViewModel = JsonHelper.DeserializeJsonToObject<SeparateOrSharedWindViewModel>(scenarioVal);
                        volumeExportModel.baseSmokeProofViewModel = separateOrSharedWindViewModel;
                        break;
                    case "楼梯间（前室不送风）":
                        var staircaseNoWindViewModel = JsonHelper.DeserializeJsonToObject<StaircaseNoWindViewModel>(scenarioVal);
                        volumeExportModel.baseSmokeProofViewModel = staircaseNoWindViewModel;
                        break;
                    case "楼梯间（前室送风）":
                        var staircaseWindViewModel = JsonHelper.DeserializeJsonToObject<StaircaseWindViewModel>(scenarioVal);
                        volumeExportModel.baseSmokeProofViewModel = staircaseWindViewModel;
                        break;
                    case "封闭避难层（间）、避难走道":
                        var evacuationWalkViewModel = JsonHelper.DeserializeJsonToObject<EvacuationWalkViewModel>(scenarioVal);
                        volumeExportModel.baseSmokeProofViewModel = evacuationWalkViewModel;
                        break;
                    case "避难走道前室":
                        var evacuationFrontViewModel = JsonHelper.DeserializeJsonToObject<EvacuationFrontViewModel>(scenarioVal);
                        volumeExportModel.baseSmokeProofViewModel = evacuationFrontViewModel;
                        break;
                    default:
                        break;
                }
                exportModels.Add(volumeExportModel);
            }

            return exportModels;
        }

        /// <summary>
        /// 选择需要导出excel的块
        /// </summary>
        /// <returns></returns>
        private List<BlockReference> SelectBlock()
        {
            var blocks = new List<BlockReference>();
            // 获取框线
            PromptSelectionOptions options = new PromptSelectionOptions()
            {
                AllowDuplicates = false,
                MessageForAdding = "请选择图块",
                RejectObjectsOnLockedLayers = true,
            };
            var dxfNames = new string[]
            {
                RXClass.GetClass(typeof(BlockReference)).DxfName,
            };
            var filterlist = OpFilter.Bulid(o =>
                o.Dxf((int)DxfCode.LayerName) == ThMEPHAVCCommon.SMOKE_PROOF_LAYER_NAME &
                o.Dxf((int)DxfCode.BlockName) == ThMEPHAVCCommon.SMOKE_PROOF_BLOCK_NAME &
                o.Dxf((int)DxfCode.Start) == string.Join(",", dxfNames));
            var result = Active.Editor.GetSelection(options, filterlist);
            if (result.Status == PromptStatus.OK)
            {
                using (AcadDatabase acdb = AcadDatabase.Active())
                {
                    foreach (ObjectId obj in result.Value.GetObjectIds())
                    {
                        blocks.Add(acdb.Element<BlockReference>(obj));
                    }
                }
            }

            return blocks;
        }
    }
}
