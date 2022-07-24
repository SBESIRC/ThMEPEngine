using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService.ExportWorkers
{
    class EvacuationFrontExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(BaseSmokeProofViewModel baseModel, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, ExcelRangeCopyOperator copyoperator)
        {
            EvacuationFrontViewModel refugeFontRoomModel = baseModel as EvacuationFrontViewModel;
            setsheet.Cells["D2"].Value = "";
            setsheet.Cells["D3"].Value = "";
            setsheet.Cells["D4"].Value = refugeFontRoomModel.OpenDorrAirSupply.ToString();
            setsheet.Cells["D5"].Value = refugeFontRoomModel.OverAk.ToString();
            setsheet.Cells["D6"].Value = "1";
            int rowNo = 7;
            for (int i = 1; i <= refugeFontRoomModel.ListTabControl.Sum(f => f.FloorInfoItems.Count); i++)
            {
                setsheet.CopyRangeToNext(7, 1, 9, 4, 3 * i);
            }
            foreach (var floor in refugeFontRoomModel.ListTabControl)
            {
                for (int i = 0; i < floor.FloorInfoItems.Count; i++)
                {
                    setsheet.Cells["A" + rowNo].Value = floor.FloorName;
                    setsheet.Cells["B" + rowNo].Value = "前室疏散门" + (i + 1);
                    setsheet.Cells["D" + rowNo].Value = floor.FloorInfoItems[i].DoorHeight.ToString();
                    setsheet.Cells["D" + (rowNo + 1)].Value = floor.FloorInfoItems[i].DoorWidth.ToString();
                    setsheet.Cells["D" + (rowNo + 2)].Value = floor.FloorInfoItems[i].DoorNum.ToString();
                    rowNo += 3;
                }
            }
            copyoperator.CopyRangeToOtherSheet(setsheet, 1, 1, rowNo - 1, 4, targetsheet);
        }
    }
}
