using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService.ExportWorkers
{
    class EvacuationWalkExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(BaseSmokeProofViewModel baseModel, string systemName, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, ExcelRangeCopyOperator copyoperator)
        {
            EvacuationWalkViewModel refugeCorridorModel = baseModel as EvacuationWalkViewModel;
            setsheet.Cells["D2"].Value = systemName;
            setsheet.Cells["D3"].Value = refugeCorridorModel.WindVolume.ToString();
            setsheet.Cells["D4"].Value = refugeCorridorModel.AreaNet.ToString();
            setsheet.Cells["D5"].Value = refugeCorridorModel.AirVolSpec.ToString();
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, 6, 4, targetsheet);
        }
    }
}
