using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPHVAC.ViewModel.ThSmokeProofSystemViewModels;

namespace ThMEPHVAC.SmokeProofSystem.ExportExcelService.ExportWorkers
{
    public class SeparateOrSharedWindExportWorker : BaseExportWorker
    {
        public override void ExportToExcel(BaseSmokeProofViewModel baseModel, string systemName, ExcelWorksheet setsheet, ExcelWorksheet targetsheet, ExcelRangeCopyOperator copyoperator)
        {
            SeparateOrSharedWindViewModel frontRoomWind = baseModel as SeparateOrSharedWindViewModel;
            setsheet.Cells["D2"].Value = systemName;
            setsheet.Cells["D3"].Value = Math.Max(frontRoomWind.FinalValue, (frontRoomWind.OpenDorrAirSupply + frontRoomWind.VentilationLeakage)).ToString();
            setsheet.Cells["D4"].Value = (frontRoomWind.OpenDorrAirSupply + frontRoomWind.VentilationLeakage).ToString();
            setsheet.Cells["D5"].Value = frontRoomWind.OpenDorrAirSupply.ToString();
            setsheet.Cells["D7"].Value = frontRoomWind.VentilationLeakage.ToString();
            setsheet.Cells["D8"].Value = frontRoomWind.OverAk.ToString();
            setsheet.Cells["D9"].Value = "0.7";
            setsheet.Cells["D10"].Value = Math.Min(frontRoomWind.FloorNum, 3).ToString();
            setsheet.Cells["D11"].Value = (frontRoomWind.SectionLength * frontRoomWind.SectionWidth / 1000000).ToString();
            setsheet.Cells["D12"].Value = (frontRoomWind.FloorNum < 3 ? 0 : frontRoomWind.FloorNum - 3).ToString();
            setsheet.Cells["D13"].Value = frontRoomWind.FinalValue.ToString();
            setsheet.Cells["D14"].Value = frontRoomWind.FloorNum.ToString();
            setsheet.Cells["D15"].Value = GetLoadRange(frontRoomWind.FloorType.ToString());
            setsheet.Cells["D16"].Value = frontRoomWind.SectionLength.ToString();
            setsheet.Cells["D17"].Value = frontRoomWind.SectionWidth.ToString();
            int rowNo = 18;
            for (int i = 1; i <= frontRoomWind.FrontRoomTabControl.Sum(f => f.FloorInfoItems.Count); i++)
            {
                setsheet.CopyRangeToNext(19, 1, 21, 4, 3 * i);
            }
            foreach (var floor in frontRoomWind.FrontRoomTabControl)
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
