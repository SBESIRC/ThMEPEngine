using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService.ExportWorkers
{
    class FireFrontExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(BaseSmokeProofViewModel baseModel, string systemName, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, ExcelRangeCopyOperator copyoperator)
        {
            FireElevatorFrontRoomViewModel fireFrontModel = baseModel as FireElevatorFrontRoomViewModel;
            setsheet.Cells["D2"].Value = systemName;
            setsheet.Cells["D3"].Value = Math.Max(fireFrontModel.FinalValue, fireFrontModel.OpenDorrAirSupply + fireFrontModel.VentilationLeakage).ToString();
            setsheet.Cells["D4"].Value = (fireFrontModel.OpenDorrAirSupply + fireFrontModel.VentilationLeakage).ToString();
            setsheet.Cells["D5"].Value = fireFrontModel.OpenDorrAirSupply.ToString();
            setsheet.Cells["D7"].Value = fireFrontModel.VentilationLeakage.ToString();
            setsheet.Cells["D8"].Value = fireFrontModel.OverAk.ToString();
            setsheet.Cells["D9"].Value = "1";
            setsheet.Cells["D10"].Value = Math.Min(fireFrontModel.FloorNum, 3).ToString();
            setsheet.Cells["D11"].Value = (fireFrontModel.SectionLength * fireFrontModel.SectionWidth / 1000000).ToString();
            setsheet.Cells["D12"].Value = (fireFrontModel.FloorNum < 3 ? 0 : fireFrontModel.FloorNum - 3).ToString();
            setsheet.Cells["D13"].Value = fireFrontModel.FinalValue.ToString();
            setsheet.Cells["D14"].Value = fireFrontModel.FloorNum.ToString();
            setsheet.Cells["D15"].Value = GetLoadRange(fireFrontModel.FloorType.ToString());
            setsheet.Cells["D16"].Value = fireFrontModel.SectionLength.ToString();
            setsheet.Cells["D17"].Value = fireFrontModel.SectionWidth.ToString();

            int formRowIndex = 18;
            for (int i = 1; i <= fireFrontModel.ListTabControl.Sum(f => f.FloorInfoItems.Count); i++)
            {
                setsheet.CopyRangeToNext(19, 1, 21, 4, 3 * i);
            }
            foreach (var floor in fireFrontModel.ListTabControl)
            {
                for (int i = 0; i < floor.FloorInfoItems.Count; i++)
                {
                    setsheet.Cells["A" + formRowIndex].Value = floor.FloorName;
                    setsheet.Cells["B" + formRowIndex].Value = "前室疏散门" + (i + 1);
                    setsheet.Cells["D" + formRowIndex].Value = floor.FloorInfoItems[i].DoorHeight.ToString();
                    setsheet.Cells["D" + (formRowIndex + 1)].Value = floor.FloorInfoItems[i].DoorWidth.ToString();
                    setsheet.Cells["D" + (formRowIndex + 2)].Value = floor.FloorInfoItems[i].DoorNum.ToString();
                    formRowIndex += 3;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, formRowIndex - 1, 4, targetsheet);
        }
    }
}
